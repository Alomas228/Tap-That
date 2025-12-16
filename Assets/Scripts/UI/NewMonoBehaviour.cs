using UnityEngine;

[CreateAssetMenu(fileName = "NewMainBuilding", menuName = "Building/Main Building Data")]
public class MainBuildingData : BuildingData
{
    [Header("Настройки главного здания")]
    [SerializeField] private int initialWarmleaf = 0;
    [SerializeField] private int initialThunderite = 0;
    [SerializeField] private int initialMirallite = 0;
    [SerializeField] private int resourceStorageCapacity = 1000;

    [Header("Логистика")]
    [SerializeField] private int maxColonistsForTransport = 5;
    [SerializeField] private float transportSpeedMultiplier = 1f;

    public int InitialWarmleaf => initialWarmleaf;
    public int InitialThunderite => initialThunderite;
    public int InitialMirallite => initialMirallite;
    public int ResourceStorageCapacity => resourceStorageCapacity;
    public int MaxColonistsForTransport => maxColonistsForTransport;
    public float TransportSpeedMultiplier => transportSpeedMultiplier;
}