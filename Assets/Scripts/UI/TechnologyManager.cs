using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TechnologyManager : MonoBehaviour
{
    public static TechnologyManager Instance { get; private set; }

    [System.Serializable]
    public class ResearchRequirement
    {
        public string resourceId;      // "warmleaf", "thunderite", "mirallite"
        public int amountPerLevel;     // Количество ресурса за уровень
        public float consumptionRate;  // Скорость потребления в секунду
    }

    [System.Serializable]
    public class Technology
    {
        [Header("Основные настройки")]
        public string id;                      // Уникальный ID
        public string displayName;             // Отображаемое имя
        public string description;             // Описание для UI
        public int maxLevel = 20;              // Максимальный уровень
        public int currentLevel = 0;           // Текущий уровень
        public float currentProgress = 0f;     // Текущий прогресс (0-100)
        public float requiredProgress = 100f;  // Необходимый прогресс для уровня
        public bool isResearching = false;     // Исследуется ли сейчас
        public bool isUnlocked = false;        // Разблокирована ли технология

        [Header("Требования")]
        public List<ResearchRequirement> requirements = new List<ResearchRequirement>();

        [Header("Скорость исследования")]
        public float colonistSpeedMultiplier = 0.1f; // +10% скорости за каждого исследователя

        [Header("Бонусы")]
        public int clickWarmleafBonus = 0;     // +X теплолиста за клик
        public int clickMiralliteBonus = 0;    // +X мираллита за клик
        public int workerWarmleafBonus = 0;    // +X теплолиста за цикл рабочего
        public int workerThunderiteBonus = 0;  // +X грозалита за цикл рабочего
        public int workerMiralliteBonus = 0;   // +X мираллита за цикл рабочего
    }

    [Header("Все технологии")]
    public List<Technology> allTechnologies = new List<Technology>();

    [Header("Текущее выбранное исследование")]
    public Technology selectedTechnology = null;

    [Header("Ссылки на UI")]
    public GameObject technologyPanel;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI requirementsText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI colonistsText;
    public Slider progressSlider;
    public Button researchButton;
    public Button closeButton;
    public GameObject researchActiveIndicator;

    [Header("Настройки исследования")]
    [SerializeField] private float baseResearchSpeed = 1f; // Базовый прогресс в секунду

    // Текущие потребляемые ресурсы
    private Dictionary<string, float> consumedResources = new Dictionary<string, float>();

    [Header("Уведомления")]
    [SerializeField] private float notificationDuration = 3f;

    // Количество активных исследователей
    private int activeResearchers = 0;

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
        // Создаем технологии если список пуст
        if (allTechnologies.Count == 0)
        {
            CreateBasicTechnologies();
        }

        SetupUI();
        UpdateTechnologyUI();
        LoadTechnologyProgress();
    }

    void Update()
    {
        // Если есть активное исследование и исследователи
        if (selectedTechnology != null &&
            selectedTechnology.isResearching &&
            selectedTechnology.currentLevel < selectedTechnology.maxLevel &&
            activeResearchers > 0)
        {
            UpdateResearchProgress();
        }

        // Обновляем UI
        if (Time.frameCount % 6 == 0)
        {
            UpdateTechnologyUI();
        }
    }

    void CreateBasicTechnologies()
    {
        // 1. Капитанский топор (требует теплолист)
        var captainAxe = new Technology
        {
            id = "captain_axe",
            displayName = "Капитанский топор",
            description = "+1 теплолиста за каждый клик игрока",
            maxLevel = 20,
            clickWarmleafBonus = 1,
            isUnlocked = true,
            colonistSpeedMultiplier = 0.1f
        };
        captainAxe.requirements.Add(new ResearchRequirement
        {
            resourceId = "warmleaf",
            amountPerLevel = 10,
            consumptionRate = 0.5f
        });
        allTechnologies.Add(captainAxe);

        // 2. Сбор образцов (требует мираллит)
        var sampleCollection = new Technology
        {
            id = "sample_collection",
            displayName = "Сбор образцов",
            description = "+1 мираллита за каждый клик игрока",
            maxLevel = 20,
            clickMiralliteBonus = 1,
            isUnlocked = true,
            colonistSpeedMultiplier = 0.15f
        };
        sampleCollection.requirements.Add(new ResearchRequirement
        {
            resourceId = "mirallite",
            amountPerLevel = 15,
            consumptionRate = 0.3f
        });
        allTechnologies.Add(sampleCollection);

        // 3. Автоматизация лесоповала (требует теплолист)
        var autoLumber = new Technology
        {
            id = "auto_lumber",
            displayName = "Автоматизация лесоповала",
            description = "+1 теплолиста за рабочий цикл",
            maxLevel = 10,
            workerWarmleafBonus = 1,
            isUnlocked = true,
            colonistSpeedMultiplier = 0.12f
        };
        autoLumber.requirements.Add(new ResearchRequirement
        {
            resourceId = "warmleaf",
            amountPerLevel = 20,
            consumptionRate = 0.4f
        });
        allTechnologies.Add(autoLumber);
    }

    void SetupUI()
    {
        if (researchButton != null)
        {
            researchButton.onClick.AddListener(OnResearchButtonClick);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() =>
            {
                if (technologyPanel != null)
                {
                    technologyPanel.SetActive(false);
                }
            });
        }

        if (technologyPanel != null)
        {
            technologyPanel.SetActive(false);
        }

        if (researchActiveIndicator != null)
        {
            researchActiveIndicator.SetActive(false);
        }

        // Инициализируем словарь потребляемых ресурсов
        consumedResources["warmleaf"] = 0f;
        consumedResources["thunderite"] = 0f;
        consumedResources["mirallite"] = 0f;
    }

    public void SelectTechnology(string techId)
    {
        Technology tech = allTechnologies.Find(t => t.id == techId);

        if (tech != null)
        {
            selectedTechnology = tech;
            UpdateTechnologyUI();

            if (technologyPanel != null && !technologyPanel.activeSelf)
            {
                technologyPanel.SetActive(true);
            }
        }
        else
        {
            Debug.LogWarning($"Технология с ID '{techId}' не найдена!");
        }
    }

    private void UpdateResearchProgress()
    {
        if (selectedTechnology == null || !selectedTechnology.isResearching) return;
        if (activeResearchers <= 0) return;

        // Рассчитываем скорость исследования с учетом колонистов
        float researchSpeed = baseResearchSpeed * (1 + (activeResearchers * selectedTechnology.colonistSpeedMultiplier));

        // Создаем копию ключей для безопасного перебора
        List<string> resourceKeys = new List<string>(consumedResources.Keys);

        foreach (var resourceId in resourceKeys)
        {
            // Находим требование для этого ресурса
            ResearchRequirement requirement = null;
            foreach (var req in selectedTechnology.requirements)
            {
                if (req.resourceId == resourceId)
                {
                    requirement = req;
                    break;
                }
            }

            if (requirement == null) continue;

            // Накапливаем потребление
            consumedResources[resourceId] += requirement.consumptionRate * Time.deltaTime * activeResearchers;

            // Если накопилось достаточно для списания
            if (consumedResources[resourceId] >= 1f)
            {
                int amountToConsume = Mathf.FloorToInt(consumedResources[resourceId]);

                if (ResourceManager.Instance != null &&
                    ResourceManager.Instance.TrySpendResource(resourceId, amountToConsume))
                {
                    consumedResources[resourceId] -= amountToConsume;

                    // Добавляем прогресс пропорционально потраченному ресурсу
                    float progressPerResource = researchSpeed / requirement.amountPerLevel;
                    selectedTechnology.currentProgress += progressPerResource * amountToConsume;

                    // Если прогресс достиг 100%
                    if (selectedTechnology.currentProgress >= selectedTechnology.requiredProgress)
                    {
                        LevelUpTechnology(selectedTechnology);
                        return; // Выходим после повышения уровня
                    }
                }
                else
                {
                    // Не удалось списать ресурс
                    StopResearch();
                    ShowNotification("Исследование приостановлено: недостаточно ресурсов");
                    return;
                }
            }
        }
    }

    void OnResearchButtonClick()
    {
        if (selectedTechnology == null)
        {
            ShowNotification("Не выбрана технология для исследования!");
            return;
        }

        if (selectedTechnology.currentLevel >= selectedTechnology.maxLevel)
        {
            ShowNotification("Достигнут максимальный уровень!");
            return;
        }

        if (!selectedTechnology.isUnlocked)
        {
            ShowNotification("Технология не разблокирована!");
            return;
        }

        // Переключаем состояние
        if (selectedTechnology.isResearching)
        {
            StopResearch();
            ShowNotification($"Исследование приостановлено: {selectedTechnology.displayName}");
        }
        else
        {
            StartResearch();
        }
    }

    private void StartResearch()
    {
        if (selectedTechnology == null) return;

        // Проверяем есть ли ресурсы для начала
        if (!HasEnoughResourcesForStart())
        {
            ShowNotification("Недостаточно ресурсов для начала исследования!");
            return;
        }

        selectedTechnology.isResearching = true;

        // Сбрасываем накопленные ресурсы (безопасно)
        List<string> keys = new List<string>(consumedResources.Keys);
        foreach (var key in keys)
        {
            consumedResources[key] = 0f;
        }

        if (researchActiveIndicator != null)
        {
            researchActiveIndicator.SetActive(true);
        }

        ShowNotification($"Начато исследование: {selectedTechnology.displayName}");
        Debug.Log($"Исследование начато: {selectedTechnology.displayName}");
    }

    private bool HasEnoughResourcesForStart()
    {
        if (selectedTechnology == null || ResourceManager.Instance == null) return false;

        foreach (var requirement in selectedTechnology.requirements)
        {
            if (ResourceManager.Instance.GetResourceAmount(requirement.resourceId) < 1)
            {
                return false;
            }
        }
        return true;
    }

    private void StopResearch()
    {
        if (selectedTechnology == null) return;

        selectedTechnology.isResearching = false;

        if (researchActiveIndicator != null)
        {
            researchActiveIndicator.SetActive(false);
        }

        Debug.Log($"Исследование остановлено: {selectedTechnology.displayName}");
    }

    void UpdateTechnologyUI()
    {
        if (selectedTechnology == null) return;

        // Обновляем тексты
        if (descriptionText != null)
        {
            descriptionText.text = selectedTechnology.description;
        }

        if (levelText != null)
        {
            levelText.text = $"Уровень: {selectedTechnology.currentLevel}/{selectedTechnology.maxLevel}";
        }

        if (progressText != null)
        {
            progressText.text = $"Прогресс: {selectedTechnology.currentProgress:F1}/{selectedTechnology.requiredProgress}";
        }

        if (requirementsText != null)
        {
            string requirementsStr = "Требуется:";
            foreach (var req in selectedTechnology.requirements)
            {
                string resourceName = ResourceManager.Instance?.GetDisplayName(req.resourceId) ?? req.resourceId;
                requirementsStr += $"\n• {resourceName}: {req.amountPerLevel} за уровень";
            }
            requirementsText.text = requirementsStr;
        }

        if (statusText != null)
        {
            if (selectedTechnology.isResearching)
            {
                statusText.text = $"Статус: Исследуется ({activeResearchers} исследователей)";
                statusText.color = Color.green;
            }
            else if (selectedTechnology.currentLevel >= selectedTechnology.maxLevel)
            {
                statusText.text = "Статус: Макс. уровень";
                statusText.color = Color.yellow;
            }
            else if (!selectedTechnology.isUnlocked)
            {
                statusText.text = "Статус: Заблокировано";
                statusText.color = Color.red;
            }
            else
            {
                statusText.text = "Статус: Доступно";
                statusText.color = Color.white;
            }
        }

        if (colonistsText != null)
        {
            colonistsText.text = $"Исследователей: {activeResearchers}\n" +
                                $"Скорость: +{selectedTechnology.colonistSpeedMultiplier * 100:F0}% за каждого";
        }

        // Обновляем прогресс бар
        if (progressSlider != null)
        {
            float progressPercentage = selectedTechnology.currentProgress / selectedTechnology.requiredProgress;
            progressSlider.value = Mathf.Clamp01(progressPercentage);
        }

        // Обновляем кнопку
        if (researchButton != null)
        {
            bool canResearch = selectedTechnology.isUnlocked &&
                              selectedTechnology.currentLevel < selectedTechnology.maxLevel;

            researchButton.interactable = canResearch;

            TextMeshProUGUI buttonText = researchButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                if (selectedTechnology.isResearching)
                {
                    buttonText.text = "Приостановить";
                    buttonText.color = Color.yellow;
                }
                else if (canResearch)
                {
                    buttonText.text = "Исследовать";
                    buttonText.color = Color.white;
                }
                else
                {
                    buttonText.text = "Недоступно";
                    buttonText.color = Color.gray;
                }
            }
        }
    }

    void LevelUpTechnology(Technology tech)
    {
        tech.currentLevel++;
        tech.currentProgress = 0f;
        tech.isResearching = false;

        // Применяем бонусы
        ApplyTechnologyBonuses(tech);

        // Показываем уведомление
        ShowNotification($"{tech.displayName} повышен до уровня {tech.currentLevel}!");

        // Сохраняем прогресс
        SaveTechnologyProgress();

        // Обновляем UI
        UpdateTechnologyUI();

        // Отключаем индикатор исследования
        if (researchActiveIndicator != null)
        {
            researchActiveIndicator.SetActive(false);
        }

        // Разблокируем связанные технологии
        UnlockRelatedTechnologies(tech);
    }

    void ApplyTechnologyBonuses(Technology tech)
    {
        Debug.Log($"Технология '{tech.displayName}' повышена до уровня {tech.currentLevel}");

        // Специальные разблокировки
        if (tech.id == "geo_research" && tech.currentLevel >= 1)
        {
            UnlockTechnology("pneumatic_picks");
            ShowNotification("Грозалит разблокирован! Можно исследовать Пневматические кирки.");
        }
    }

    void UnlockRelatedTechnologies(Technology tech)
    {
        switch (tech.id)
        {
            case "captain_axe":
                if (tech.currentLevel >= 5) UnlockTechnology("auto_lumber");
                break;
            case "sample_collection":
                if (tech.currentLevel >= 3) UnlockTechnology("strong_connections");
                break;
        }
    }

    void UnlockTechnology(string techId)
    {
        Technology tech = allTechnologies.Find(t => t.id == techId);
        if (tech != null && !tech.isUnlocked)
        {
            tech.isUnlocked = true;
            ShowNotification($"Разблокирована технология: {tech.displayName}");
        }
    }

    // МЕТОД ДЛЯ ИССЛЕДОВАТЕЛЕЙ: колонист принес ресурс на станцию
    public bool DeliverResearchResource(string resourceId, int amount)
    {
        if (selectedTechnology == null || !selectedTechnology.isResearching) return false;
        if (amount <= 0) return false;

        // Проверяем нужен ли этот ресурс для текущего исследования
        bool resourceNeeded = false;
        ResearchRequirement requirement = null;

        foreach (var req in selectedTechnology.requirements)
        {
            if (req.resourceId == resourceId)
            {
                resourceNeeded = true;
                requirement = req;
                break;
            }
        }

        if (!resourceNeeded || requirement == null) return false;

        // Добавляем прогресс
        float researchSpeed = baseResearchSpeed * (1 + (activeResearchers * selectedTechnology.colonistSpeedMultiplier));
        float progressPerResource = researchSpeed / requirement.amountPerLevel;
        selectedTechnology.currentProgress += progressPerResource * amount;

        // Если прогресс достиг 100%
        if (selectedTechnology.currentProgress >= selectedTechnology.requiredProgress)
        {
            LevelUpTechnology(selectedTechnology);
        }

        return true;
    }

    // Регистрация исследователей
    public void RegisterResearcher()
    {
        activeResearchers++;
        Debug.Log($"Зарегистрирован исследователь. Всего: {activeResearchers}");
        UpdateTechnologyUI();
    }

    public void UnregisterResearcher()
    {
        activeResearchers = Mathf.Max(0, activeResearchers - 1);
        Debug.Log($"Удален исследователь. Всего: {activeResearchers}");
        UpdateTechnologyUI();
    }

    void SaveTechnologyProgress()
    {
        foreach (var tech in allTechnologies)
        {
            PlayerPrefs.SetInt($"tech_{tech.id}_level", tech.currentLevel);
            PlayerPrefs.SetFloat($"tech_{tech.id}_progress", tech.currentProgress);
            PlayerPrefs.SetInt($"tech_{tech.id}_researching", tech.isResearching ? 1 : 0);
            PlayerPrefs.SetInt($"tech_{tech.id}_unlocked", tech.isUnlocked ? 1 : 0);
        }
        PlayerPrefs.Save();
        Debug.Log("Прогресс технологий сохранен");
    }

    void LoadTechnologyProgress()
    {
        foreach (var tech in allTechnologies)
        {
            tech.currentLevel = PlayerPrefs.GetInt($"tech_{tech.id}_level", 0);
            tech.currentProgress = PlayerPrefs.GetFloat($"tech_{tech.id}_progress", 0f);
            tech.isResearching = PlayerPrefs.GetInt($"tech_{tech.id}_researching", 0) == 1;
            tech.isUnlocked = PlayerPrefs.GetInt($"tech_{tech.id}_unlocked", tech.isUnlocked ? 1 : 0) == 1;
        }
        Debug.Log("Прогресс технологий загружен");
    }

    #region ПУБЛИЧНЫЕ МЕТОДЫ

    public int GetClickWarmleafBonus()
    {
        int totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0)
            {
                totalBonus += tech.clickWarmleafBonus * tech.currentLevel;
            }
        }
        return totalBonus;
    }

    public int GetClickMiralliteBonus()
    {
        int totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0)
            {
                totalBonus += tech.clickMiralliteBonus * tech.currentLevel;
            }
        }
        return totalBonus;
    }

    public int GetWorkerWarmleafBonus()
    {
        int totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0)
            {
                totalBonus += tech.workerWarmleafBonus * tech.currentLevel;
            }
        }
        return totalBonus;
    }

    public bool IsAnyTechnologyResearching()
    {
        foreach (var tech in allTechnologies)
        {
            if (tech.isResearching) return true;
        }
        return false;
    }

    public Technology GetResearchingTechnology()
    {
        foreach (var tech in allTechnologies)
        {
            if (tech.isResearching) return tech;
        }
        return null;
    }

    public int GetActiveResearchersCount() => activeResearchers;

    #endregion

    #region УТИЛИТЫ

    private void ShowNotification(string message)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowNotification(message, notificationDuration);
        }
        else
        {
            Debug.Log($"Уведомление: {message}");
        }
    }

    public void LogAllTechnologies()
    {
        Debug.Log("=== ВСЕ ТЕХНОЛОГИИ ===");
        foreach (var tech in allTechnologies)
        {
            string researching = tech.isResearching ? " [ИССЛЕДУЕТСЯ]" : "";
            string unlocked = tech.isUnlocked ? "" : " [ЗАБЛОКИРОВАНО]";
            Debug.Log($"{tech.displayName}: Ур. {tech.currentLevel}/{tech.maxLevel}, Прогресс: {tech.currentProgress:F1}%{researching}{unlocked}");
        }
    }

    public void ResetAllTechnologies()
    {
        foreach (var tech in allTechnologies)
        {
            tech.currentLevel = 0;
            tech.currentProgress = 0f;
            tech.isResearching = false;
            tech.isUnlocked = true; // Сброс на изначальные разблокировки
        }
        PlayerPrefs.DeleteAll();
        Debug.Log("Все технологии сброшены");
        UpdateTechnologyUI();
    }

    #endregion
}