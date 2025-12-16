using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BuildingMenuUI : MonoBehaviour
{
    [Header("Тексты цен (4 штуки)")]
    [SerializeField] private TextMeshProUGUI[] costTexts; // Element 0-3

    [Header("Кнопки (4 штуки)")]
    [SerializeField] private Button[] buildingButtons; // Element 0-3

    [Header("Цвета")]
    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color unaffordableColor = new(1, 0.5f, 0.5f);

    private BuildingManager buildingManager;
    private ResourceManager resourceManager;

    void Start()
    {
        buildingManager = BuildingManager.Instance;
        resourceManager = ResourceManager.Instance;

        UpdateAllPrices();
    }

    void Update()
    {
        // Обновляем каждые 15 кадров (оптимизация)
        if (Time.frameCount % 15 == 0)
        {
            UpdateAllPrices();
        }
    }

    public void UpdateAllPrices()
    {
        if (buildingManager == null || resourceManager == null) return;

        // Обновляем цены для 4 зданий
        for (int i = 0; i < 4; i++)
        {
            UpdatePrice(i);
        }
    }

    private void UpdatePrice(int index)
    {
        BuildingData buildingData = GetBuildingDataByIndex(index);
        if (buildingData == null) return;

        // 1. Получаем цену (без дебаг-лога)
        int cost = buildingManager.CalculateBuildingCost(buildingData);

        // 2. Формируем текст цены
        string priceText = FormatPriceText(buildingData, cost, index);

        // 3. Обновляем Text
        if (costTexts != null && index < costTexts.Length && costTexts[index] != null)
        {
            costTexts[index].text = priceText;

            // 4. Меняем цвет
            bool canAfford = CanAffordBuilding(buildingData, cost);
            bool canBuild = CanBuildBuilding(buildingData);
            costTexts[index].color = (canAfford && canBuild) ? affordableColor : unaffordableColor;
        }

        // 5. Обновляем кнопку
        if (buildingButtons != null && index < buildingButtons.Length && buildingButtons[index] != null)
        {
            bool canAfford = CanAffordBuilding(buildingData, cost);
            bool canBuild = CanBuildBuilding(buildingData);
            buildingButtons[index].interactable = canAfford && canBuild;
        }
    }

    private BuildingData GetBuildingDataByIndex(int index)
    {
        if (buildingManager == null) return null;

        return index switch
        {
            0 => buildingManager.GetHouseData(),
            1 => buildingManager.GetWarmleafStationData(),
            2 => buildingManager.GetResearchStationData(),
            3 => buildingManager.GetEnricherData(),
            _ => null
        };
    }

    private string FormatPriceText(BuildingData buildingData, int cost, int index)
    {
        string resourceName = GetResourceName(buildingData.costType);

        // БАЗОВЫЙ ФОРМАТ: "20 Теплолиста"
        string priceText = $"{cost} {resourceName}";

        // ДОПОЛНИТЕЛЬНАЯ ИНФОРМАЦИЯ (опционально):

        // Для жилища - показываем текущую стоимость без #1
        if (index == 0 && buildingData.hasProgressiveCost)
        {
            // Только число и ресурс: "24 Теплолиста"
            // Никаких (#1), (#2) и т.д.
        }

        // Для уникальных зданий - показываем [МАКС] если построено
        if (buildingData.isUnique)
        {
            int builtCount = buildingManager.GetBuildingCount(buildingData);
            if (builtCount >= 1)
            {
                priceText += " [МАКС]";
            }
        }

        return priceText;
    }

    private bool CanAffordBuilding(BuildingData buildingData, int cost)
    {
        if (resourceManager == null) return false;

        string resourceId = buildingData.costType.ToString().ToLower();
        int currentAmount = resourceManager.GetResourceAmount(resourceId);

        return currentAmount >= cost;
    }

    private bool CanBuildBuilding(BuildingData buildingData)
    {
        if (buildingData.isUnique)
        {
            return buildingManager.GetBuildingCount(buildingData) < 1;
        }

        if (buildingData.maxCount > 0)
        {
            return buildingManager.GetBuildingCount(buildingData) < buildingData.maxCount;
        }

        return true;
    }

    private string GetResourceName(BuildingData.CostType costType)
    {
        return costType switch
        {
            BuildingData.CostType.Warmleaf => "Теплолиста",
            BuildingData.CostType.Thunderite => "Грозалита",
            BuildingData.CostType.Mirallite => "Мираллита",
            _ => ""
        };
    }
}