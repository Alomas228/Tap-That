using UnityEngine;

public class WarmleafStationWorker : ColonistWorker
{
    [Header("Настройки станции теплолиста")]
    [SerializeField] private int warmleafPerCycle = 1;
    [SerializeField] private float interactionTime = 3f;

    [Header("Технологические улучшения")]
    [SerializeField] private float productionMultiplier = 1f;
    [SerializeField] private float speedMultiplier = 1f;

    protected override float GetInteractionTime()
    {
        return interactionTime / speedMultiplier; // Ускорение от технологий
    }

    protected override void CollectResourcesFromBuilding()
    {
        // Колонист получает теплолист
        int amount = Mathf.RoundToInt(warmleafPerCycle * productionMultiplier);
        AddCarriedResource("warmleaf", amount);

        Debug.Log($"{name} добыл {amount} теплолиста");

        // Проверяем улучшения (можно добавить систему технологий)
        UpdateTechnologyBonuses();
    }

    private void UpdateTechnologyBonuses()
    {
        // Здесь будет логика проверки технологий
        // Например:
        // productionMultiplier = TechnologyManager.Instance.GetWarmleafProductionMultiplier();
        // speedMultiplier = TechnologyManager.Instance.GetWorkerSpeedMultiplier();
    }

    protected override bool HasResourcesToDeliver()
    {
        // Всегда сдаем теплолист, если есть
        return carriedWarmleaf > 0;
    }

    // Дополнительные методы для UI
    public int GetWarmleafPerCycle() => warmleafPerCycle;
    public float GetProductionMultiplier() => productionMultiplier;
    public float GetSpeedMultiplier() => speedMultiplier;
}