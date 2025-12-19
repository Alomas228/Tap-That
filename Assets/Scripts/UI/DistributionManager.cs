using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class DistributionManager : MonoBehaviour
{
    public static DistributionManager Instance { get; private set; }

    [Header("UI элементы распределения")]
    [SerializeField] private TextMeshProUGUI warmleafStationText;
    [SerializeField] private TextMeshProUGUI researchStationText;
    [SerializeField] private TextMeshProUGUI enricherText;
    [SerializeField] private TextMeshProUGUI occupiedText;

    [Header("Кнопки распределения")]
    [SerializeField] private Button processingPlusBtn;
    [SerializeField] private Button processingMinusBtn;
    [SerializeField] private Button researchPlusBtn;
    [SerializeField] private Button researchMinusBtn;
    [SerializeField] private Button enricherPlusBtn;
    [SerializeField] private Button enricherMinusBtn;

    [Header("Префабы")]
    [SerializeField] private GameObject colonistWorkerPrefab;
    [SerializeField] private Transform colonistWorkerContainer;

    [Header("Цвета для неактивных кнопок")]
    [SerializeField] private Color disabledButtonColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    [SerializeField] private Color disabledTextColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    [Header("Настройки ограничений")]
    [SerializeField] private int maxResearchWorkers = 5; // Максимум 5 исследователей
    [SerializeField] private int maxWorkersPerBuilding = 999; // Практически неограничено для других

    // Количество рабочих на каждом здании
    private int processingWorkers = 0;
    private int researchWorkers = 0;
    private int enricherWorkers = 0;

    // Менеджеры
    private ColonistManager colonistManager;
    private BuildingManager buildingManager;

    // Флаги наличия зданий
    private bool hasProcessingStation = false;
    private bool hasResearchStation = false;
    private bool hasEnricher = false;

    // Словари для хранения рабочих
    private Dictionary<BuildingType, List<ColonistWorker>> buildingWorkers = new();
    private Dictionary<BuildingType, List<Transform>> availableBuildings = new();

    // Данные зданий
    private BuildingData warmleafStationData;
    private BuildingData researchStationData;
    private BuildingData enricherData;
    private BuildingData houseData;

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
    }

    void Start()
    {
        colonistManager = ColonistManager.Instance;
        buildingManager = BuildingManager.Instance;

        if (colonistManager != null)
        {
            colonistManager.OnColonistChanged += OnColonistChanged;
        }

        // Получаем данные зданий
        if (buildingManager != null)
        {
            warmleafStationData = buildingManager.GetWarmleafStationData();
            researchStationData = buildingManager.GetResearchStationData();
            enricherData = buildingManager.GetEnricherData();
            houseData = buildingManager.GetHouseData();
        }

        CheckBuildingsExistence(true);
        UpdateAvailableBuildings();
        InitializeButtons();
        UpdateAllUI();

        Debug.Log("DistributionManager инициализирован");
    }

    void Update()
    {
        if (Time.frameCount % 120 == 0)
        {
            CheckBuildingsExistence(false);
            UpdateAvailableBuildings();
            UpdateButtonsVisualState();

            // Обновляем максимум исследователей с учетом технологий
            UpdateMaxResearchWorkers();
        }
    }

    void OnDestroy()
    {
        if (colonistManager != null)
        {
            colonistManager.OnColonistChanged -= OnColonistChanged;
        }
    }

    private void UpdateMaxResearchWorkers()
    {
        int baseMax = 5;
        int techBonus = 0;

        if (TechnologyManager.Instance != null)
        {
            techBonus = TechnologyManager.Instance.GetResearchSlotsBonus();
        }

        maxResearchWorkers = baseMax + techBonus;
    }

    #region Обработка изменений колонистов

    private void OnColonistChanged(int total, int available, int capacity, int queueLength, float patience)
    {
        UpdateOccupiedText();
    }

    #endregion

    #region Проверка зданий

    private void CheckBuildingsExistence(bool logChanges = false)
    {
        if (buildingManager == null) return;

        bool prevProcessing = hasProcessingStation;
        bool prevResearch = hasResearchStation;
        bool prevEnricher = hasEnricher;

        hasProcessingStation = warmleafStationData != null && buildingManager.GetBuildingCount(warmleafStationData) > 0;
        hasResearchStation = researchStationData != null && buildingManager.GetBuildingCount(researchStationData) > 0;
        hasEnricher = enricherData != null && buildingManager.GetBuildingCount(enricherData) > 0;

        if (logChanges && (prevProcessing != hasProcessingStation ||
                          prevResearch != hasResearchStation ||
                          prevEnricher != hasEnricher))
        {
            Debug.Log($"Наличие зданий: Обработка={hasProcessingStation}, " +
                     $"Исследование={hasResearchStation}, " +
                     $"Обогатитель={hasEnricher}");
        }
    }

    private void UpdateAvailableBuildings()
    {
        availableBuildings.Clear();
        availableBuildings[BuildingType.ProcessingStation] = new List<Transform>();
        availableBuildings[BuildingType.ResearchStation] = new List<Transform>();
        availableBuildings[BuildingType.Enricher] = new List<Transform>();

        if (buildingManager == null) return;

        Building[] allBuildings = FindObjectsByType<Building>(FindObjectsSortMode.None);

        foreach (var building in allBuildings)
        {
            if (building is MainBuilding) continue;

            BuildingData data = building.GetBuildingData();
            if (data == null || !building.IsBuilt()) continue;

            if (data == warmleafStationData)
            {
                availableBuildings[BuildingType.ProcessingStation].Add(building.transform);
            }
            else if (data == researchStationData)
            {
                availableBuildings[BuildingType.ResearchStation].Add(building.transform);
            }
            else if (data == enricherData)
            {
                availableBuildings[BuildingType.Enricher].Add(building.transform);
            }
        }
    }

    #endregion

    #region Настройка UI

    private void InitializeButtons()
    {
        SetupButton(processingPlusBtn, AddProcessingWorker);
        SetupButton(processingMinusBtn, RemoveProcessingWorker);
        SetupButton(researchPlusBtn, AddResearchWorker);
        SetupButton(researchMinusBtn, RemoveResearchWorker);
        SetupButton(enricherPlusBtn, AddEnricherWorker);
        SetupButton(enricherMinusBtn, RemoveEnricherWorker);
    }

    private void SetupButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }
    }

    private void UpdateButtonsVisualState()
    {
        UpdateButtonSet(processingPlusBtn, processingMinusBtn, hasProcessingStation);
        UpdateButtonSet(researchPlusBtn, researchMinusBtn, hasResearchStation);
        UpdateButtonSet(enricherPlusBtn, enricherMinusBtn, hasEnricher);

        // Для исследовательской станции проверяем лимит
        if (researchPlusBtn != null)
        {
            researchPlusBtn.interactable = hasResearchStation && researchWorkers < maxResearchWorkers;
        }
    }

    private void UpdateButtonSet(Button plusBtn, Button minusBtn, bool hasBuilding)
    {
        if (plusBtn != null)
        {
            plusBtn.interactable = hasBuilding;
            UpdateButtonVisuals(plusBtn, hasBuilding);
        }

        if (minusBtn != null)
        {
            minusBtn.interactable = hasBuilding;
            UpdateButtonVisuals(minusBtn, hasBuilding);
        }
    }

    private void UpdateButtonVisuals(Button button, bool isEnabled)
    {
        if (button == null) return;

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = isEnabled ? Color.white : disabledButtonColor;
        }
    }

    #endregion

    #region Обновление UI

    private void UpdateAllUI()
    {
        UpdateBuildingTexts();
        UpdateOccupiedText();
        UpdateButtonsVisualState();
    }

    private void UpdateBuildingTexts()
    {
        if (warmleafStationText != null)
        {
            warmleafStationText.text = hasProcessingStation ? processingWorkers.ToString() : "0";
            warmleafStationText.color = hasProcessingStation ? Color.white : disabledTextColor;
        }

        if (researchStationText != null)
        {
            researchStationText.text = hasResearchStation ? $"{researchWorkers}/{maxResearchWorkers}" : "0/5";
            researchStationText.color = hasResearchStation ? Color.white : disabledTextColor;
        }

        if (enricherText != null)
        {
            enricherText.text = hasEnricher ? enricherWorkers.ToString() : "0";
            enricherText.color = hasEnricher ? Color.white : disabledTextColor;
        }
    }

    private void UpdateOccupiedText()
    {
        if (occupiedText != null && colonistManager != null)
        {
            int assigned = GetTotalAssignedWorkers();
            int totalColonists = colonistManager.GetTotalColonists();
            occupiedText.text = $"{assigned}/{totalColonists}";
        }
    }

    #endregion

    #region Логика добавления/удаления рабочих

    public void AddProcessingWorker()
    {
        if (!hasProcessingStation)
        {
            ShowNotification("Станция обработки не построена!");
            return;
        }

        AddWorker(BuildingType.ProcessingStation);
    }

    public void RemoveProcessingWorker()
    {
        if (!hasProcessingStation) return;
        RemoveWorker(BuildingType.ProcessingStation);
    }

    public void AddResearchWorker()
    {
        if (!hasResearchStation)
        {
            ShowNotification("Исследовательская станция не построена!");
            return;
        }

        if (researchWorkers >= maxResearchWorkers)
        {
            ShowNotification($"Максимум {maxResearchWorkers} исследователей!");
            return;
        }

        AddWorker(BuildingType.ResearchStation);
    }

    public void RemoveResearchWorker()
    {
        if (!hasResearchStation) return;
        RemoveWorker(BuildingType.ResearchStation);
    }

    public void AddEnricherWorker()
    {
        if (!hasEnricher)
        {
            ShowNotification("Обогатитель не построен!");
            return;
        }

        AddWorker(BuildingType.Enricher);
    }

    public void RemoveEnricherWorker()
    {
        if (!hasEnricher) return;
        RemoveWorker(BuildingType.Enricher);
    }

    private void AddWorker(BuildingType buildingType)
    {
        if (!HasAvailableColonists())
        {
            ShowNotification("Нет свободных колонистов!");
            return;
        }

        if (colonistManager != null && colonistManager.AssignColonist(1))
        {
            switch (buildingType)
            {
                case BuildingType.ProcessingStation:
                    processingWorkers++;
                    break;
                case BuildingType.ResearchStation:
                    researchWorkers++;
                    break;
                case BuildingType.Enricher:
                    enricherWorkers++;
                    break;
            }

            CreateColonistWorker(buildingType);
            UpdateAllUI();
            Debug.Log($"Добавлен рабочий на {buildingType}. Всего: {GetWorkersOnBuilding(buildingType)}");
        }
    }

    private void RemoveWorker(BuildingType buildingType)
    {
        if (!HasWorkersOnBuilding(buildingType)) return;

        if (colonistManager != null && colonistManager.UnassignColonist(1))
        {
            switch (buildingType)
            {
                case BuildingType.ProcessingStation:
                    processingWorkers = Mathf.Max(0, processingWorkers - 1);
                    break;
                case BuildingType.ResearchStation:
                    researchWorkers = Mathf.Max(0, researchWorkers - 1);
                    break;
                case BuildingType.Enricher:
                    enricherWorkers = Mathf.Max(0, enricherWorkers - 1);
                    break;
            }

            RemoveColonistWorker(buildingType);
            UpdateAllUI();
            Debug.Log($"Удален рабочий с {buildingType}. Осталось: {GetWorkersOnBuilding(buildingType)}");
        }
    }

    #endregion

    #region Создание и удаление колонистов

    private void CreateColonistWorker(BuildingType buildingType)
    {
        if (colonistWorkerPrefab == null)
        {
            Debug.LogError("Префаб колониста не назначен!");
            return;
        }

        // Находим здание для работы
        Transform building = GetAvailableBuilding(buildingType);
        if (building == null)
        {
            Debug.LogError($"Нет доступных зданий типа {buildingType}!");
            ShowNotification($"Нет доступных зданий типа {buildingType}!");
            return;
        }

        Building buildingComponent = building.GetComponent<Building>();
        if (buildingComponent == null)
        {
            Debug.LogError($"Здание {building.name} не имеет компонента Building!");
            return;
        }

        BuildingData data = buildingComponent.GetBuildingData();
        Debug.Log($"Создаем колониста для здания: {building.name} ({data?.buildingName})");

        // Создаем колониста
        Vector3 spawnPos = GetMainBuildingPosition();
        GameObject colonistObj = Instantiate(colonistWorkerPrefab, spawnPos, Quaternion.identity, colonistWorkerContainer);
        colonistObj.name = $"ColonistWorker_{buildingType}_{System.Guid.NewGuid().ToString("N").Substring(0, 4)}";

        // Добавляем нужный компонент
        ColonistWorker worker = null;
        switch (buildingType)
        {
            case BuildingType.ProcessingStation:
                worker = colonistObj.AddComponent<WarmleafStationWorker>();
                break;
            case BuildingType.ResearchStation:
                worker = colonistObj.AddComponent<ResearchStationWorker>();
                break;
            case BuildingType.Enricher:
                worker = colonistObj.AddComponent<EnricherWorker>();
                break;
        }

        if (worker != null)
        {
            // Настраиваем визуал
            Transform visualTransform = colonistObj.transform.Find("Visual");
            if (visualTransform != null)
            {
                worker.visualObject = visualTransform.gameObject;
                worker.spriteRenderer = visualTransform.GetComponent<SpriteRenderer>();
            }

            // Назначаем на здание
            worker.AssignToBuilding(building);

            // Добавляем в словарь
            if (!buildingWorkers.ContainsKey(buildingType))
            {
                buildingWorkers[buildingType] = new List<ColonistWorker>();
            }
            buildingWorkers[buildingType].Add(worker);

            Debug.Log($"Создан {worker.GetType().Name} для здания {building.name}");
        }
    }

    private void RemoveColonistWorker(BuildingType buildingType)
    {
        if (!buildingWorkers.ContainsKey(buildingType) || buildingWorkers[buildingType].Count == 0)
            return;

        // Удаляем первого рабочего
        ColonistWorker workerToRemove = buildingWorkers[buildingType][0];
        buildingWorkers[buildingType].RemoveAt(0);

        if (workerToRemove != null)
        {
            Debug.Log($"Удаляем колониста: {workerToRemove.name}");
            Destroy(workerToRemove.gameObject);
        }
    }

    private Transform GetAvailableBuilding(BuildingType buildingType)
    {
        if (availableBuildings.ContainsKey(buildingType) && availableBuildings[buildingType].Count > 0)
        {
            // Распределяем рабочих по зданиям
            int buildingIndex = 0;
            if (buildingWorkers.ContainsKey(buildingType))
            {
                buildingIndex = buildingWorkers[buildingType].Count % availableBuildings[buildingType].Count;
            }

            return availableBuildings[buildingType][buildingIndex];
        }

        Debug.LogWarning($"Нет доступных зданий типа {buildingType}");
        return null;
    }

    private Vector3 GetMainBuildingPosition()
    {
        GameObject mainBuilding = GameObject.FindGameObjectWithTag("MainBuilding");
        if (mainBuilding != null)
        {
            Vector3 pos = mainBuilding.transform.position;
            pos.x += Random.Range(-2f, 2f);
            pos.y += Random.Range(-2f, 2f);
            return pos;
        }
        return Vector3.zero;
    }

    #endregion

    #region Вспомогательные методы

    private bool HasAvailableColonists()
    {
        return colonistManager != null && colonistManager.HasAvailableColonists();
    }

    private bool HasWorkersOnBuilding(BuildingType buildingType)
    {
        return GetWorkersOnBuilding(buildingType) > 0;
    }

    public int GetWorkersOnBuilding(BuildingType buildingType)
    {
        return buildingType switch
        {
            BuildingType.ProcessingStation => processingWorkers,
            BuildingType.ResearchStation => researchWorkers,
            BuildingType.Enricher => enricherWorkers,
            _ => 0
        };
    }

    public int GetTotalAssignedWorkers()
    {
        return processingWorkers + researchWorkers + enricherWorkers;
    }

    public int GetMaxResearchWorkers()
    {
        return maxResearchWorkers;
    }

    public int GetCurrentResearchWorkers()
    {
        return researchWorkers;
    }

    private void ShowNotification(string message)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowNotification(message, 1.5f);
        }
        else
        {
            Debug.Log($"Уведомление: {message}");
        }
    }

    public void ForceUpdateUI()
    {
        CheckBuildingsExistence(true);
        UpdateAvailableBuildings();
        UpdateAllUI();
    }

    #endregion

    #region Enum

    public enum BuildingType
    {
        ProcessingStation,
        ResearchStation,
        Enricher
    }

    #endregion
}