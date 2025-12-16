using UnityEngine;
using System.Collections;

public class EnricherWorker : ColonistWorker
{
    [Header("Настройки обогатителя")]
    [SerializeField] private float interactionTime = 2f;
    [SerializeField] private int mirallitePerCycle = 1;

    [Header("Система аномалий")]
    [SerializeField] private float baseAnomalyChance = 10f; // 10%
    [SerializeField] private float colonistMultiplier = 0.5f; // +0.5% за каждого колониста
    [SerializeField] private float anomalyDuration = 5f;

    private bool isAnomalyActive = false;
    private AnomalyType currentAnomaly = AnomalyType.None;
    private float anomalyTimer = 0f;

    public enum AnomalyType
    {
        None,
        EnergySurge,        // Энергетический всплеск - добыча x2
        RegenerativeStorm,  // Регенеративный шторм - восстановление x10
        PheromoneRelease    // Феромонный выброс - скорость +2%
    }

    protected override float GetInteractionTime()
    {
        return interactionTime;
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
                    miralliteToAdd *= 2; // x2 добыча
                    break;
                    // Другие аномалии будут влиять по-другому
            }
        }

        AddCarriedResource("mirallite", miralliteToAdd);
        Debug.Log($"Колонист добыл {miralliteToAdd} мираллита" +
                 (isAnomalyActive ? $" (аномалия: {currentAnomaly})" : ""));

        // Проверяем шанс аномалии после добычи
        CheckForAnomaly();
    }

    protected override void Update()  // Используем override вместо new
    {
        base.Update(); // Вызываем базовый Update

        // Обновляем таймер аномалии
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

        // Получаем количество колонистов
        int colonistCount = 0;
        if (ColonistManager.Instance != null)
        {
            colonistCount = ColonistManager.Instance.GetTotalColonists();
        }

        // Рассчитываем шанс аномалии
        float anomalyChance = baseAnomalyChance + (colonistCount * colonistMultiplier);

        // Проверяем срабатывание
        if (Random.Range(0f, 100f) <= anomalyChance)
        {
            StartRandomAnomaly();
        }
    }

    private void StartRandomAnomaly()
    {
        // Выбираем случайную аномалию
        AnomalyType[] possibleAnomalies = {
            AnomalyType.EnergySurge,
            AnomalyType.RegenerativeStorm,
            AnomalyType.PheromoneRelease
        };

        currentAnomaly = possibleAnomalies[Random.Range(0, possibleAnomalies.Length)];
        isAnomalyActive = true;
        anomalyTimer = anomalyDuration;

        Debug.Log($"АКТИВИРОВАНА АНОМАЛИЯ: {currentAnomaly} на {anomalyDuration} секунд!");

        // Применяем эффекты аномалии
        ApplyAnomalyEffects(true);
    }

    private void ApplyAnomalyEffects(bool start)
    {
        switch (currentAnomaly)
        {
            case AnomalyType.EnergySurge:
                // Эффект уже применяется в CollectResourcesFromBuilding
                break;

            case AnomalyType.RegenerativeStorm:
                // Ускоряем восстановление прочности источников мираллита
                // Реализуем позже
                break;

            case AnomalyType.PheromoneRelease:
                // Ускоряем всех колонистов
                // Реализуем позже
                break;
        }
    }

    private void EndAnomaly()
    {
        isAnomalyActive = false;
        currentAnomaly = AnomalyType.None;

        // Снимаем эффекты аномалии
        ApplyAnomalyEffects(false);

        Debug.Log("Аномалия завершена");
    }

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