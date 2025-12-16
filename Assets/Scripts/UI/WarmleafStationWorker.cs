using UnityEngine;

public class WarmleafStationWorker : ColonistWorker
{
    [Header("Настройки станции теплолиста")]
    [SerializeField] private int warmleafPerCycle = 1;
    [SerializeField] private float interactionTime = 3f;

    protected override float GetInteractionTime()
    {
        return interactionTime;
    }

    protected override void CollectResourcesFromBuilding()
    {
        // Колонист получает теплолист
        AddCarriedResource("warmleaf", warmleafPerCycle);

        Debug.Log($"Колонист добыл {warmleafPerCycle} теплолиста");
    }
}