using UnityEngine;

[CreateAssetMenu(fileName = "NewBuilding", menuName = "Building/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Основные настройки")]
    public string buildingName;
    public string description;
    public GameObject prefab;
    public Sprite icon;

    [Header("Размер и стоимость")]
    public Vector2Int gridSize = new(1, 1);
    public int baseCost = 100; // Базовая стоимость
    public CostType costType = CostType.Warmleaf; // Тип ресурса для оплаты

    [Header("Особые правила")]
    public bool hasProgressiveCost = false; // Использовать формулу для стоимости
    public bool isUnique = false; // Только один на карте
    public int maxCount = 0; // 0 = неограничено

    [Header("Визуальные настройки")]
    public Color previewColor = new(0, 1, 0, 0.5f);

    public enum CostType
    {
        Warmleaf,
        Thunderite,
        Mirallite
    }
}