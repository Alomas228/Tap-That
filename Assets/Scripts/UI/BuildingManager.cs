using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    [Header("Данные зданий")]
    [SerializeField] private BuildingData houseData;
    [SerializeField] private BuildingData warmleafStationData;
    [SerializeField] private BuildingData researchStationData;
    [SerializeField] private BuildingData enricherData;

    [Header("Главное здание")]
    [SerializeField] private MainBuildingData mainBuildingData;
    [SerializeField] private GameObject mainBuildingPrefab;

    private GameObject mainBuildingInstance;
    private GridManager gridManager;
    private ResourceManager resourceManager;
    private Mouse currentMouse;
    private Keyboard currentKeyboard;

    // Трекеры построенных зданий
    private Dictionary<string, int> buildingCount = new();
    private Dictionary<string, List<GameObject>> builtBuildings = new();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Инициализируем словари
        InitializeBuildingDictionaries();
    }

    void Start()
    {
        gridManager = GridManager.Instance;
        resourceManager = ResourceManager.Instance;
        currentMouse = Mouse.current;
        currentKeyboard = Keyboard.current;

        if (gridManager == null) Debug.LogError("GridManager не найден!");
        if (resourceManager == null) Debug.LogError("ResourceManager не найден!");

        
    }

    void Update()
    {
        if (gridManager != null && gridManager.IsBuildingMode)
        {
            HandleBuildingInput();
        }
    }

    private void InitializeBuildingDictionaries()
    {
        // Инициализируем для каждого типа зданий
        if (houseData != null)
        {
            buildingCount[houseData.buildingName] = 0;
            builtBuildings[houseData.buildingName] = new List<GameObject>();
        }

        if (warmleafStationData != null)
        {
            buildingCount[warmleafStationData.buildingName] = 0;
            builtBuildings[warmleafStationData.buildingName] = new List<GameObject>();
        }

        if (researchStationData != null)
        {
            buildingCount[researchStationData.buildingName] = 0;
            builtBuildings[researchStationData.buildingName] = new List<GameObject>();
        }

        if (enricherData != null)
        {
            buildingCount[enricherData.buildingName] = 0;
            builtBuildings[enricherData.buildingName] = new List<GameObject>();
        }
    }

    #region Публичные методы для UI

    public void StartBuildingHouse()
    {
        TryStartBuilding(houseData);
    }

    public void StartBuildingWarmleafStation() // Было StartBuildingProcessing
    {
        TryStartBuilding(warmleafStationData);
    }

    public void StartBuildingResearchStation() // Было StartBuildingResearch
    {
        TryStartBuilding(researchStationData);
    }

    public void StartBuildingEnricher()
    {
        TryStartBuilding(enricherData);
    }

    public void CancelBuilding()
    {
        if (gridManager != null)
        {
            gridManager.ExitBuildingMode();
        }
    }

    #endregion
    

    
    // Добавляем публичный метод для получения главного здания
    public GameObject GetMainBuilding() => mainBuildingInstance;
    public MainBuildingData GetMainBuildingData() => mainBuildingData;

    private void TryStartBuilding(BuildingData buildingData)
    {
        if (gridManager == null)
        {
            Debug.LogError("GridManager не найден!");
            return;
        }

        if (buildingData == null)
        {
            Debug.LogError($"BuildingData не назначен!");
            return;
        }

        if (buildingData.prefab == null)
        {
            Debug.LogError($"Префаб не назначен в {buildingData.name}!");
            return;
        }

        // Проверяем ограничения по количеству
        if (buildingData.isUnique && GetBuildingCount(buildingData) >= 1)
        {
            Debug.Log($"Можно построить только одно здание: {buildingData.buildingName}");
            ShowNotification($"Можно построить только одно: {buildingData.buildingName}");
            return;
        }

        if (buildingData.maxCount > 0 && GetBuildingCount(buildingData) >= buildingData.maxCount)
        {
            Debug.Log($"Достигнут лимит: {buildingData.buildingName} (макс: {buildingData.maxCount})");
            ShowNotification($"Лимит: {buildingData.buildingName} ({buildingData.maxCount} шт.)");
            return;
        }

        // Проверяем достаточно ли ресурсов
        int cost = CalculateBuildingCost(buildingData);
        if (!HasEnoughResources(buildingData, cost))
        {
            string resourceName = GetResourceName(buildingData.costType);
            Debug.Log($"Недостаточно {resourceName}: нужно {cost}, есть {GetResourceAmount(buildingData.costType)}");
            ShowNotification($"Недостаточно {resourceName}");
            return;
        }

        gridManager.EnterBuildingMode(buildingData);
        Debug.Log($"Начинаем строительство: {buildingData.buildingName} (Стоимость: {cost})");
    }

    private void HandleBuildingInput()
    {
        if (currentMouse == null || currentKeyboard == null) return;

        // ЛКМ - построить
        if (currentMouse.leftButton.wasPressedThisFrame)
        {
            Vector3 mousePos = GetMouseWorldPosition();
            if (TryPlaceBuilding(mousePos))
            {
                gridManager.ExitBuildingMode();
            }
        }

        // ПКМ или Escape - отмена
        if (currentMouse.rightButton.wasPressedThisFrame ||
            currentKeyboard.escapeKey.wasPressedThisFrame)
        {
            gridManager.ExitBuildingMode();
            Debug.Log("Строительство отменено");
        }
    }

    private bool TryPlaceBuilding(Vector3 worldPosition)
    {
        if (gridManager == null || gridManager.CurrentBuildingData == null) return false;

        BuildingData buildingData = gridManager.CurrentBuildingData;

        // Проверяем можно ли построить здесь
        if (!gridManager.CanPlaceBuilding(worldPosition)) // ИЗМЕНЕНИЕ: используем CanPlaceBuilding вместо TryPlaceBuilding
        {
            Debug.Log("Нельзя построить: клетки заняты или вне сетки");
            return false;
        }

        // Рассчитываем стоимость
        int cost = CalculateBuildingCost(buildingData);

        // Списываем ресурсы
        if (!SpendResources(buildingData.costType, cost))
        {
            Debug.LogError("Не удалось списать ресурсы!");
            return false;
        }

        // Создаем здание
        Vector2Int gridPos = gridManager.WorldToGridPosition(worldPosition);
        Vector3 buildPosition = gridManager.GetBuildingWorldPosition(gridPos);
        GameObject newBuilding = Instantiate(buildingData.prefab, buildPosition, Quaternion.identity);
        newBuilding.name = $"{buildingData.buildingName}_{GetBuildingCount(buildingData) + 1}";

        // Добавляем скрипт Building для отслеживания
        Building buildingScript = newBuilding.GetComponent<Building>();
        if (buildingScript == null)
        {
            buildingScript = newBuilding.AddComponent<Building>();
        }
        buildingScript.Initialize(buildingData, cost);

        // Регистрируем здание в GridManager (ИЗМЕНЕНИЕ: вызываем новый метод)
        if (gridManager != null)
        {
            gridManager.RegisterBuilding(newBuilding, gridPos);
        }

        // Регистрируем здание в BuildingManager
        RegisterBuilding(buildingData, newBuilding);

        Debug.Log($"Построено: {buildingData.buildingName} (Стоимость: {cost} {GetResourceName(buildingData.costType)})");
        ShowNotification($"{buildingData.buildingName} построен!");

        return true;
    }

    #region Система стоимости

    public int CalculateBuildingCost(BuildingData buildingData)
    {
        if (!buildingData.hasProgressiveCost)
        {
            return buildingData.baseCost;
        }

        // Формула для жилища: P(n) = Pbase * (1 + 0.2 * (n - 1))^1.7
        int builtCount = GetBuildingCount(buildingData);
        int n = builtCount + 1; // Номер нового здания

        float progressiveMultiplier = Mathf.Pow(1 + 0.2f * (n - 1), 1.7f);
        int cost = Mathf.RoundToInt(buildingData.baseCost * progressiveMultiplier);

        //Debug.Log($"Стоимость {buildingData.buildingName} #{n}: {cost} (база: {buildingData.baseCost}, множитель: {progressiveMultiplier:F2})");

        return cost;
    }

    
    public string GetBuildingCostString(BuildingData buildingData)
    {
        int cost = CalculateBuildingCost(buildingData);
        string resourceName = GetResourceName(buildingData.costType);

        if (buildingData.hasProgressiveCost)
        {
            int builtCount = GetBuildingCount(buildingData);
            return $"{cost} {resourceName}";
        }

        return $"{cost} {resourceName}";
    }

    #endregion

    #region Вспомогательные методы

    private bool HasEnoughResources(BuildingData buildingData, int cost)
    {
        int currentAmount = GetResourceAmount(buildingData.costType);
        return currentAmount >= cost;
    }

    private bool SpendResources(BuildingData.CostType costType, int amount)
    {
        if (resourceManager == null) return false;

        string resourceId = costType.ToString().ToLower();
        return resourceManager.TrySpendResource(resourceId, amount);
    }

    private int GetResourceAmount(BuildingData.CostType costType)
    {
        if (resourceManager == null) return 0;

        string resourceId = costType.ToString().ToLower();
        return resourceManager.GetResourceAmount(resourceId);
    }

    private string GetResourceName(BuildingData.CostType costType)
    {
        return costType switch
        {
            BuildingData.CostType.Warmleaf => "Теплолиста",
            BuildingData.CostType.Thunderite => "Грозалита",
            BuildingData.CostType.Mirallite => "Мираллита",
            _ => "Ресурса"
        };
    }

    private void RegisterBuilding(BuildingData buildingData, GameObject building)
    {
        if (!buildingCount.ContainsKey(buildingData.buildingName))
        {
            buildingCount[buildingData.buildingName] = 0;
            builtBuildings[buildingData.buildingName] = new List<GameObject>();
        }

        buildingCount[buildingData.buildingName]++;
        builtBuildings[buildingData.buildingName].Add(building);
    }

    public int GetBuildingCount(BuildingData buildingData)
    {
        if (buildingData == null) return 0;

        if (buildingCount.ContainsKey(buildingData.buildingName))
        {
            return buildingCount[buildingData.buildingName];
        }

        return 0;
    }

    private void ShowNotification(string message)
    {
        // Используй твой UIManager для показа уведомлений
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowNotification(message);
        }
        else
        {
            Debug.Log($"Уведомление: {message}");
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        if (currentMouse == null) return Vector3.zero;

        Vector2 mousePos = currentMouse.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(mousePos.x, mousePos.y, Mathf.Abs(Camera.main.transform.position.z))
        );

        return new Vector3(worldPos.x, worldPos.y, 0);
    }

    #endregion

    #region Публичные методы для UI

    public BuildingData GetHouseData() => houseData;
    public BuildingData GetWarmleafStationData() => warmleafStationData;
    public BuildingData GetResearchStationData() => researchStationData;
    public BuildingData GetEnricherData() => enricherData;

    #endregion
}