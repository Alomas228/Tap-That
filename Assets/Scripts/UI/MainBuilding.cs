using UnityEngine;

public class MainBuilding : Building
{
    [Header("Настройки главного здания")]
    [SerializeField] private MainBuildingData mainBuildingData;

    [Header("Хранение ресурсов")]
    [SerializeField] private int warmleafStorage = 0;
    [SerializeField] private int thunderiteStorage = 0;
    [SerializeField] private int miralliteStorage = 0;
    [SerializeField] private int maxStorageCapacity = 1000;

    [Header("Логистика")]
    [SerializeField] private int assignedTransportColonists = 0;
    [SerializeField] private int maxTransportColonists = 5;

    [Header("Настройки инициализации")]
    [SerializeField] private bool initializeOnStart = true;
    [SerializeField] private bool registerInGrid = true;

    private ResourceManager resourceManager;
    private bool isInitialized = false;

    void Start()
    {
        if (initializeOnStart && !isInitialized)
        {
            InitializeMainBuilding();
        }
    }

    public void InitializeMainBuilding()
    {
        if (isInitialized) return;

        if (mainBuildingData == null)
        {
            Debug.LogError("MainBuildingData не назначен! Назначьте ScriptableObject в инспекторе.");
            return;
        }

        // Инициализируем как обычное здание
        base.Initialize(mainBuildingData, 0);

        // Получаем менеджер ресурсов
        resourceManager = ResourceManager.Instance;

        // Загружаем начальные ресурсы
        maxStorageCapacity = mainBuildingData.ResourceStorageCapacity;
        maxTransportColonists = mainBuildingData.MaxColonistsForTransport;

        // Инициализируем начальные ресурсы
        warmleafStorage = mainBuildingData.InitialWarmleaf;
        thunderiteStorage = mainBuildingData.InitialThunderite;
        miralliteStorage = mainBuildingData.InitialMirallite;

        // Регистрируем в GridManager
        if (registerInGrid && GridManager.Instance != null)
        {
            Vector2Int gridPos = GridManager.Instance.WorldToGridPosition(transform.position);
            GridManager.Instance.RegisterBuilding(gameObject, gridPos);
        }

        // Синхронизируем с ResourceManager
        SyncWithResourceManager();

        isInitialized = true;

        Debug.Log($"Главное здание '{mainBuildingData.buildingName}' инициализировано!");
        Debug.Log($"Позиция: {transform.position}, Вместимость: {maxStorageCapacity}");
        Debug.Log($"Начальные ресурсы: Теплолист={warmleafStorage}, Грозалит={thunderiteStorage}, Мираллит={miralliteStorage}");
    }

    void Update()
    {
        if (isActive && isBuilt)
        {
            // Можно добавить логику обновления
        }
    }

    #region ХРАНЕНИЕ РЕСУРСОВ

    public bool AddResource(string resourceType, int amount)
    {
        if (amount <= 0) return false;

        switch (resourceType.ToLower())
        {
            case "warmleaf":
                if (warmleafStorage + amount > maxStorageCapacity) return false;
                warmleafStorage += amount;
                break;

            case "thunderite":
                if (thunderiteStorage + amount > maxStorageCapacity) return false;
                thunderiteStorage += amount;
                break;

            case "mirallite":
                if (miralliteStorage + amount > maxStorageCapacity) return false;
                miralliteStorage += amount;
                break;

            default:
                Debug.LogWarning($"Неизвестный тип ресурса: {resourceType}");
                return false;
        }

        SyncWithResourceManager();
        return true;
    }

    public bool TakeResource(string resourceType, int amount)
    {
        if (amount <= 0) return false;

        switch (resourceType.ToLower())
        {
            case "warmleaf":
                if (warmleafStorage < amount) return false;
                warmleafStorage -= amount;
                break;

            case "thunderite":
                if (thunderiteStorage < amount) return false;
                thunderiteStorage -= amount;
                break;

            case "mirallite":
                if (miralliteStorage < amount) return false;
                miralliteStorage -= amount;
                break;

            default:
                Debug.LogWarning($"Неизвестный тип ресурса: {resourceType}");
                return false;
        }

        SyncWithResourceManager();
        return true;
    }

    private void SyncWithResourceManager()
    {
        if (resourceManager == null) return;

        Debug.Log($"Ресурсы в главном здании: Теплолист={warmleafStorage}, Грозалит={thunderiteStorage}, Мираллит={miralliteStorage}");
    }

    #endregion

    #region ЛОГИСТИКА

    public bool AssignTransportColonist()
    {
        if (assignedTransportColonists >= maxTransportColonists)
        {
            Debug.Log("Достигнут лимит колонистов для транспортировки");
            return false;
        }

        assignedTransportColonists++;
        Debug.Log($"Колонист назначен на транспортировку. Всего: {assignedTransportColonists}/{maxTransportColonists}");
        return true;
    }

    public bool UnassignTransportColonist()
    {
        if (assignedTransportColonists <= 0) return false;

        assignedTransportColonists--;
        Debug.Log($"Колонист снят с транспортировки. Всего: {assignedTransportColonists}/{maxTransportColonists}");
        return true;
    }

    #endregion

    #region ПУБЛИЧНЫЕ МЕТОДЫ

    public int GetResourceAmount(string resourceType)
    {
        return resourceType.ToLower() switch
        {
            "warmleaf" => warmleafStorage,
            "thunderite" => thunderiteStorage,
            "mirallite" => miralliteStorage,
            _ => 0
        };
    }

    public int GetTotalResourceAmount()
    {
        return warmleafStorage + thunderiteStorage + miralliteStorage;
    }

    public float GetStorageUsagePercentage()
    {
        return maxStorageCapacity > 0 ? (float)GetTotalResourceAmount() / maxStorageCapacity : 0f;
    }

    public string GetStorageInfo()
    {
        return $"Хранилище: {GetTotalResourceAmount()}/{maxStorageCapacity} " +
               $"(Теплолист: {warmleafStorage}, Грозалит: {thunderiteStorage}, Мираллит={miralliteStorage})";
    }

    public (int warmleaf, int thunderite, int mirallite, int total, int capacity) GetStorageDetails()
    {
        return (warmleafStorage, thunderiteStorage, miralliteStorage, GetTotalResourceAmount(), maxStorageCapacity);
    }

    public int GetAssignedTransportColonists() => assignedTransportColonists;
    public int GetMaxTransportColonists() => maxTransportColonists;
    public bool CanAssignMoreTransportColonists() => assignedTransportColonists < maxTransportColonists;

    public bool IsInitialized() => isInitialized;
    public MainBuildingData GetMainBuildingData() => mainBuildingData;

    #endregion

    void OnDrawGizmosSelected()
    {
        // Визуализация радиуса действия
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawWireSphere(transform.position, 5f);
    }
}