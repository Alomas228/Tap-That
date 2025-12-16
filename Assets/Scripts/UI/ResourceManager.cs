using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    // ТОЛЬКО общее количество ресурсов у игрока
    private readonly Dictionary<string, int> playerResources = new();

    // Настройки типов ресурсов (только константы)
    [System.Serializable]
    public class ResourceTypeSettings
    {
        public string resourceId;
        public string displayName;
        public Sprite icon;
        public int defaultClickReward = 1;
        public bool hasDurability = false;
        public int maxDurability = 100;
        public float durabilityRegenRate = 0.5f;
    }

    [Header("Настройки типов ресурсов")]
    [SerializeField] private ResourceTypeSettings warmleafSettings;
    [SerializeField] private ResourceTypeSettings thunderiteSettings;
    [SerializeField] private ResourceTypeSettings miralliteSettings;

    [Header("События")]
    public UnityEvent<string, int> OnResourceChanged;
    public UnityEvent<string> OnResourceDepleted; // Для совместимости

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Инициализация словаря
        playerResources.Clear();
        playerResources["warmleaf"] = 0;
        playerResources["thunderite"] = 0;
        playerResources["mirallite"] = 0;

        // Настройки по умолчанию
        warmleafSettings.resourceId = "warmleaf";
        warmleafSettings.displayName = "Теплолист";
        warmleafSettings.hasDurability = false;

        thunderiteSettings.resourceId = "thunderite";
        thunderiteSettings.displayName = "Грозалит";
        thunderiteSettings.hasDurability = false;

        miralliteSettings.resourceId = "mirallite";
        miralliteSettings.displayName = "Мираллит";
        miralliteSettings.hasDurability = true;
        miralliteSettings.maxDurability = 100;
        miralliteSettings.durabilityRegenRate = 0.5f;

        Debug.Log("ResourceManager инициализирован");
    }

    #region Новые методы (основные)

    public void AddResource(string resourceId, int amount)
    {
        if (!playerResources.ContainsKey(resourceId))
            playerResources[resourceId] = 0;

        playerResources[resourceId] += amount;
        OnResourceChanged?.Invoke(resourceId, playerResources[resourceId]);

        Debug.Log($"Добавлено {GetDisplayName(resourceId)}: +{amount}, Всего: {playerResources[resourceId]}");
    }

    public int GetResourceAmount(string resourceId)
    {
        return playerResources.ContainsKey(resourceId) ? playerResources[resourceId] : 0;
    }

    public string GetDisplayName(string resourceId)
    {
        if (resourceId == "warmleaf") return warmleafSettings.displayName;
        if (resourceId == "thunderite") return thunderiteSettings.displayName;
        if (resourceId == "mirallite") return miralliteSettings.displayName;
        return resourceId;
    }

    public ResourceTypeSettings GetSettings(string resourceId)
    {
        if (resourceId == "warmleaf") return warmleafSettings;
        if (resourceId == "thunderite") return thunderiteSettings;
        if (resourceId == "mirallite") return miralliteSettings;
        return null;
    }

    #endregion

    #region Методы для совместимости (старые названия)

    public int GetWarmleafAmount() => GetResourceAmount("warmleaf");
    public int GetThunderiteAmount() => GetResourceAmount("thunderite");
    public int GetMiralliteAmount() => GetResourceAmount("mirallite");

    public ResourceTypeSettings GetMiralliteData() => GetSettings("mirallite");

    public float GetMiralliteDurabilityPercentage()
    {
        Debug.LogWarning("GetMiralliteDurabilityPercentage не актуален - прочность у каждого источника своя");
        return 0f;
    }

    public void CollectWarmleaf(int amount = 1)
    {
        AddResource("warmleaf", amount);
        Debug.Log($"Собран теплолист: +{amount}");
    }

    public void CollectThunderite(int amount = 1)
    {
        AddResource("thunderite", amount);
        Debug.Log($"Собран грозалит: +{amount}");
    }

    public bool TryCollectMirallite(int amount = 1)
    {
        Debug.LogWarning("TryCollectMirallite не актуален - у каждого источника своя прочность");
        AddResource("mirallite", amount);
        return true;
    }

    #endregion

    #region Сохранение и загрузка

    public void SaveResources()
    {
        PlayerPrefs.SetInt("WarmleafAmount", GetWarmleafAmount());
        PlayerPrefs.SetInt("ThunderiteAmount", GetThunderiteAmount());
        PlayerPrefs.SetInt("MiralliteAmount", GetMiralliteAmount());
        PlayerPrefs.Save();

        Debug.Log("Ресурсы сохранены");
    }

    public void LoadResources()
    {
        if (PlayerPrefs.HasKey("WarmleafAmount"))
        {
            AddResource("warmleaf", PlayerPrefs.GetInt("WarmleafAmount", 0) - GetWarmleafAmount());
            AddResource("thunderite", PlayerPrefs.GetInt("ThunderiteAmount", 0) - GetThunderiteAmount());
            AddResource("mirallite", PlayerPrefs.GetInt("MiralliteAmount", 0) - GetMiralliteAmount());

            Debug.Log("Ресурсы загружены");
        }
    }

    public void ResetResources()
    {
        playerResources["warmleaf"] = 0;
        playerResources["thunderite"] = 0;
        playerResources["mirallite"] = 0;

        OnResourceChanged?.Invoke("warmleaf", 0);
        OnResourceChanged?.Invoke("thunderite", 0);
        OnResourceChanged?.Invoke("mirallite", 0);

        Debug.Log("Ресурсы сброшены");
    }

    public bool TrySpendResource(string resourceId, int amount)
    {
        if (!playerResources.ContainsKey(resourceId))
            return false;

        if (playerResources[resourceId] < amount)
            return false;

        playerResources[resourceId] -= amount;
        OnResourceChanged?.Invoke(resourceId, playerResources[resourceId]);

        Debug.Log($"Потрачено {GetDisplayName(resourceId)}: -{amount}, Осталось: {playerResources[resourceId]}");
        return true;
    }

    #endregion

    #region Утилиты

    public void LogAllResources()
    {
        Debug.Log("=== ТЕКУЩИЕ РЕСУРСЫ ===");
        Debug.Log($"Теплолист: {GetWarmleafAmount()}");
        Debug.Log($"Грозалит: {GetThunderiteAmount()}");
        Debug.Log($"Мираллит: {GetMiralliteAmount()}");
    }

    #endregion
}