using UnityEngine;
using System.Collections;

public class EnricherWorker : ColonistWorker
{
    [Header("Настройки обогатителя")]
    [SerializeField] private float interactionTime = 2f;
    [SerializeField] private int mirallitePerCycle = 1;
    [SerializeField] private float colonistWeight = 1.0f; // Вес колониста для расчета эффективности

    [Header("Система аномалий")]
    [SerializeField] private float baseAnomalyChance = 10f;
    [SerializeField] private float colonistMultiplier = 0.5f;
    [SerializeField] private float anomalyDuration = 5f;
    [SerializeField] private int enricherCapacity = 1; // Вместимость обогатителя

    private bool isAnomalyActive = false;
    private AnomalyType currentAnomaly = AnomalyType.None;
    private float anomalyTimer = 0f;
    private float speedBonusMultiplier = 1f;

    public enum AnomalyType
    {
        None,
        EnergySurge,        // Добыча x2
        RegenerativeStorm,  // Восстановление x10
        PheromoneRelease    // Скорость +2%
    }

    protected override float GetInteractionTime()
    {
        // Учитываем бонусы скорости от технологий
        float speedMultiplier = 1f;
        if (TechnologyManager.Instance != null)
        {
            speedMultiplier += TechnologyManager.Instance.GetColonistSpeedBonus() / 100f;
        }

        return interactionTime / (speedBonusMultiplier * speedMultiplier);
    }

    protected override void Start()
    {
        base.Start();

        // Применяем начальные бонусы от технологий
        ApplyTechnologyBonuses();
    }

    protected override void CollectResourcesFromBuilding()
    {
        // Базовое количество мираллита с учетом веса колониста
        float baseProduction = mirallitePerCycle * colonistWeight;

        // Проверяем активные аномалии
        float anomalyMultiplier = 1f;
        if (isAnomalyActive)
        {
            switch (currentAnomaly)
            {
                case AnomalyType.EnergySurge:
                    anomalyMultiplier = 2f;
                    Debug.Log($"{name}: Энергетический всплеск! Добыча x2");
                    break;

                case AnomalyType.RegenerativeStorm:
                    // Восстанавливаем прочность источников мираллита
                    RestoreMiralliteSources();
                    break;

                case AnomalyType.PheromoneRelease:
                    Debug.Log($"{name}: Феромонный выброс, скорость +{((speedBonusMultiplier - 1) * 100):F0}%");
                    break;
            }
        }

        int miralliteToAdd = Mathf.RoundToInt(baseProduction * anomalyMultiplier);
        AddCarriedResource("mirallite", miralliteToAdd);
        Debug.Log($"{name} добыл {miralliteToAdd} мираллита" +
                 (isAnomalyActive ? $" (аномалия: {currentAnomaly})" : "") +
                 $" (вес: {colonistWeight:F2})");

        // Проверяем шанс аномалии
        CheckForAnomaly();
    }

    protected override void Update()
    {
        base.Update();

        if (isAnomalyActive)
        {
            anomalyTimer -= Time.deltaTime;
            if (anomalyTimer <= 0f)
            {
                EndAnomaly();
            }
        }
    }

    private void ApplyTechnologyBonuses()
    {
        if (TechnologyManager.Instance != null)
        {
            // Бонус к весу колониста
            colonistWeight += TechnologyManager.Instance.GetColonistWeightBonus();

            // Бонус к длительности аномалий
            anomalyDuration += TechnologyManager.Instance.GetAnomalyDurationBonus();

            // Бонус к вместимости обогатителя
            enricherCapacity += TechnologyManager.Instance.GetEnricherCapacityBonus();

            Debug.Log($"{name}: Бонусы применены - Вес: {colonistWeight:F2}, Аномалия: +{TechnologyManager.Instance.GetAnomalyDurationBonus()}с, Вместимость: {enricherCapacity}");
        }
    }

    private void RestoreMiralliteSources()
    {
        ResourceSource[] miralliteSources = FindObjectsByType<ResourceSource>(FindObjectsSortMode.None);
        int restoredCount = 0;

        foreach (var source in miralliteSources)
        {
            // Используем публичный метод GetResourceId()
            if (source.GetResourceId() == "mirallite")
            {
                // Восстанавливаем 10 единиц прочности
                // (это упрощенная реализация, можно доработать)
                restoredCount++;
            }
        }

        Debug.Log($"{name}: Регенеративный шторм восстановил {restoredCount} источников мираллита");
    }

    private void CheckForAnomaly()
    {
        if (isAnomalyActive) return;

        int colonistCount = ColonistManager.Instance != null ?
            ColonistManager.Instance.GetTotalColonists() : 0;

        float anomalyChance = baseAnomalyChance + (colonistCount * colonistMultiplier);

        if (Random.Range(0f, 100f) <= anomalyChance)
        {
            StartRandomAnomaly();
        }
    }

    private void StartRandomAnomaly()
    {
        AnomalyType[] possibleAnomalies = {
            AnomalyType.EnergySurge,
            AnomalyType.RegenerativeStorm,
            AnomalyType.PheromoneRelease
        };

        currentAnomaly = possibleAnomalies[Random.Range(0, possibleAnomalies.Length)];
        isAnomalyActive = true;
        anomalyTimer = anomalyDuration;

        Debug.Log($"АКТИВИРОВАНА АНОМАЛИЯ: {currentAnomaly} на {anomalyDuration} секунд!");

        ApplyAnomalyEffects(true);
    }

    private void ApplyAnomalyEffects(bool start)
    {
        switch (currentAnomaly)
        {
            case AnomalyType.PheromoneRelease:
                if (start)
                {
                    speedBonusMultiplier = 1.02f;
                }
                else
                {
                    speedBonusMultiplier = 1f;
                }
                break;
        }
    }

    private void EndAnomaly()
    {
        ApplyAnomalyEffects(false);

        isAnomalyActive = false;
        currentAnomaly = AnomalyType.None;
        speedBonusMultiplier = 1f;

        Debug.Log("Аномалия завершена");
    }

    protected override bool HasResourcesToDeliver()
    {
        return carriedMirallite > 0;
    }

    // Публичные методы для UI
    public bool IsAnomalyActive() => isAnomalyActive;
    public AnomalyType GetCurrentAnomaly() => currentAnomaly;
    public float GetAnomalyTimeLeft() => anomalyTimer;
    public float GetAnomalyChance()
    {
        int colonistCount = ColonistManager.Instance != null ?
            ColonistManager.Instance.GetTotalColonists() : 0;
        return baseAnomalyChance + (colonistCount * colonistMultiplier);
    }

    public float GetColonistWeight() => colonistWeight;
    public int GetEnricherCapacity() => enricherCapacity;

    public string GetEnricherInfo()
    {
        string info = $"Производство: {mirallitePerCycle}/цикл\n";
        info += $"Вес колониста: {colonistWeight:F2}\n";
        info += $"Вместимость: {enricherCapacity}\n";
        info += $"Шанс аномалии: {GetAnomalyChance():F1}%\n";

        if (isAnomalyActive)
        {
            info += $"Активная аномалия: {currentAnomaly}\n";
            info += $"Осталось: {anomalyTimer:F1}с";
        }

        return info;
    }
}