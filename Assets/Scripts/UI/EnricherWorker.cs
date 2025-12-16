using UnityEngine;
using System.Collections;

public class EnricherWorker : ColonistWorker
{
    [Header("Настройки обогатителя")]
    [SerializeField] private float interactionTime = 2f;
    [SerializeField] private int mirallitePerCycle = 1;

    [Header("Система аномалий")]
    [SerializeField] private float baseAnomalyChance = 10f;
    [SerializeField] private float colonistMultiplier = 0.5f;
    [SerializeField] private float anomalyDuration = 5f;

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
        return interactionTime / speedBonusMultiplier;
    }

    protected override void CollectResourcesFromBuilding()
    {
        int miralliteToAdd = mirallitePerCycle;

        // Проверяем активные аномалии
        if (isAnomalyActive)
        {
            switch (currentAnomaly)
            {
                case AnomalyType.EnergySurge:
                    miralliteToAdd *= 2;
                    Debug.Log($"{name}: Энергетический всплеск! Добыча x2");
                    break;

                case AnomalyType.RegenerativeStorm:
                    Debug.Log($"{name}: Регенеративный шторм активен");
                    break;

                case AnomalyType.PheromoneRelease:
                    Debug.Log($"{name}: Феромонный выброс, скорость +{((speedBonusMultiplier - 1) * 100):F0}%");
                    break;
            }
        }

        AddCarriedResource("mirallite", miralliteToAdd);
        Debug.Log($"{name} добыл {miralliteToAdd} мираллита" +
                 (isAnomalyActive ? $" (аномалия: {currentAnomaly})" : ""));

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
}