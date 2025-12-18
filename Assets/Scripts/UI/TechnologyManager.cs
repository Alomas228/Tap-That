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
    }

    [System.Serializable]
    public class Technology
    {
        [Header("Основные настройки")]
        public string id;                      // Уникальный ID
        public string displayName;             // Отображаемое имя
        public string description;             // Описание для UI
        public int maxLevel = 10;              // Максимальный уровень
        public int currentLevel = 0;           // Текущий уровень

        [Header("Прогресс исследования")]
        public int resourcesPerLevel = 20;     // Сколько ресурсов нужно на 1 уровень
        public int collectedResources = 0;     // Сколько уже собрано для текущего уровня
        public float currentProgress = 0f;     // Процент прогресса (0-100)
        public bool isResearching = false;
        public bool isUnlocked = false;

        [Header("Требования")]
        public List<ResearchRequirement> requirements = new List<ResearchRequirement>();

        [Header("Бонусы")]
        public int clickWarmleafBonus = 0;     // +X теплолиста за клик
        public int clickMiralliteBonus = 0;    // +X мираллита за клик
        public int clickThunderiteBonus = 0;   // +X грозалита за клик
        public int workerWarmleafBonus = 0;    // +X теплолиста за цикл рабочего
        public int workerThunderiteBonus = 0;  // +X грозалита за цикл рабочего
        public int workerMiralliteBonus = 0;   // +X мираллита за цикл рабочего
        public int carryCapacityBonus = 0;     // +X к вместимости рюкзака
        public int colonistSpeedBonus = 0;     // +X% скорости колонистов
        public int colonistCapacityBonus = 0;  // +X вместимость жилища
        public int researchSpeedBonus = 0;     // +X% скорости исследования
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
    public TextMeshProUGUI resourcesText;
    public TextMeshProUGUI requirementsText;
    public TextMeshProUGUI statusText;
    public Slider progressSlider;
    public Button researchButton;
    public Button closeButton;
    public GameObject researchActiveIndicator;

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
        // Создаем базовые технологии если список пуст
        if (allTechnologies.Count == 0)
        {
            CreateBasicTechnologies();
        }

        SetupUI();
        UpdateTechnologyUI();
        LoadTechnologyProgress();
    }

    void CreateBasicTechnologies()
    {
        // 1. Капитанский топор
        var captainAxe = new Technology
        {
            id = "captain_axe",
            displayName = "Капитанский топор",
            description = "+1 теплолиста за каждый клик игрока",
            maxLevel = 20,
            resourcesPerLevel = 20,
            clickWarmleafBonus = 1,
            isUnlocked = true
        };
        captainAxe.requirements.Add(new ResearchRequirement
        {
            resourceId = "warmleaf",
            amountPerLevel = 20
        });
        allTechnologies.Add(captainAxe);

        // 2. Сбор образцов
        var sampleCollection = new Technology
        {
            id = "sample_collection",
            displayName = "Сбор образцов",
            description = "+1 мираллита за каждый клик игрока",
            maxLevel = 20,
            resourcesPerLevel = 25,
            clickMiralliteBonus = 1,
            isUnlocked = true
        };
        sampleCollection.requirements.Add(new ResearchRequirement
        {
            resourceId = "mirallite",
            amountPerLevel = 25
        });
        allTechnologies.Add(sampleCollection);

        // 3. Увеличенные рюкзаки
        var bigBackpacks = new Technology
        {
            id = "big_backpacks",
            displayName = "Увеличенные рюкзаки",
            description = "+1 к вместимости рюкзака исследователей",
            maxLevel = 5,
            resourcesPerLevel = 30,
            carryCapacityBonus = 1,
            isUnlocked = true
        };
        bigBackpacks.requirements.Add(new ResearchRequirement
        {
            resourceId = "warmleaf",
            amountPerLevel = 30
        });
        allTechnologies.Add(bigBackpacks);

        // 4. Быстрые ноги
        var fastFeet = new Technology
        {
            id = "fast_feet",
            displayName = "Быстрые ноги",
            description = "+5% скорости колонистов",
            maxLevel = 10,
            resourcesPerLevel = 40,
            colonistSpeedBonus = 5,
            isUnlocked = true
        };
        fastFeet.requirements.Add(new ResearchRequirement
        {
            resourceId = "warmleaf",
            amountPerLevel = 40
        });
        allTechnologies.Add(fastFeet);
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
        selectedTechnology.collectedResources = 0;
        selectedTechnology.currentProgress = 0f;

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

    void Update()
    {
        // Обновляем UI каждые 0.1 секунды
        if (Time.frameCount % 6 == 0)
        {
            UpdateTechnologyUI();
        }
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
            progressText.text = $"Прогресс: {selectedTechnology.currentProgress:F1}%";
        }

        if (resourcesText != null)
        {
            resourcesText.text = $"Ресурсы: {selectedTechnology.collectedResources}/{selectedTechnology.resourcesPerLevel}";
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
                statusText.text = $"Статус: Исследуется";
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

        // Обновляем прогресс бар
        if (progressSlider != null)
        {
            float progressPercentage = selectedTechnology.currentProgress / 100f;
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

    // МЕТОД ДЛЯ ИССЛЕДОВАТЕЛЕЙ: колонист принес ресурс на станцию
    public bool DeliverResearchResource(string resourceId, int amount)
    {
        if (selectedTechnology == null || !selectedTechnology.isResearching) return false;
        if (amount <= 0) return false;

        // Проверяем нужен ли этот ресурс
        bool resourceNeeded = false;
        foreach (var req in selectedTechnology.requirements)
        {
            if (req.resourceId == resourceId)
            {
                resourceNeeded = true;
                break;
            }
        }

        if (!resourceNeeded) return false;

        // Добавляем ресурсы
        selectedTechnology.collectedResources += amount;

        // Пересчитываем прогресс: (собрано / нужно) * 100%
        selectedTechnology.currentProgress =
            (float)selectedTechnology.collectedResources / selectedTechnology.resourcesPerLevel * 100f;

        // Если прогресс > 100%, корректируем
        if (selectedTechnology.currentProgress > 100f)
        {
            selectedTechnology.currentProgress = 100f;
        }

        // Если собрали достаточно
        if (selectedTechnology.collectedResources >= selectedTechnology.resourcesPerLevel)
        {
            LevelUpTechnology(selectedTechnology);
        }

        return true;
    }

    void LevelUpTechnology(Technology tech)
    {
        tech.currentLevel++;
        tech.collectedResources = 0;
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
    }

    void ApplyTechnologyBonuses(Technology tech)
    {
        Debug.Log($"Технология '{tech.displayName}' повышена до уровня {tech.currentLevel}");

        // Применяем бонусы ко всем исследователям
        if (tech.carryCapacityBonus > 0)
        {
            ApplyCarryCapacityBonus(tech.carryCapacityBonus);
        }

        // Можно добавить другие бонусы здесь
    }

    // Метод для применения бонуса вместимости
    private void ApplyCarryCapacityBonus(int bonus)
    {
        // Находим всех исследователей и увеличиваем им вместимость
        ResearchStationWorker[] researchers = FindObjectsOfType<ResearchStationWorker>();
        foreach (var researcher in researchers)
        {
            researcher.IncreaseCarryCapacity(bonus);
        }
        Debug.Log($"Применен бонус вместимости +{bonus} к {researchers.Length} исследователям");
    }

    // Регистрация исследователей
    public void RegisterResearcher()
    {
        activeResearchers++;
        Debug.Log($"Зарегистрирован исследователь. Всего: {activeResearchers}");
    }

    public void UnregisterResearcher()
    {
        activeResearchers = Mathf.Max(0, activeResearchers - 1);
        Debug.Log($"Удален исследователь. Всего: {activeResearchers}");
    }

    void SaveTechnologyProgress()
    {
        foreach (var tech in allTechnologies)
        {
            PlayerPrefs.SetInt($"tech_{tech.id}_level", tech.currentLevel);
            PlayerPrefs.SetInt($"tech_{tech.id}_collected", tech.collectedResources);
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
            tech.collectedResources = PlayerPrefs.GetInt($"tech_{tech.id}_collected", 0);
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
            Debug.Log($"{tech.displayName}: Ур. {tech.currentLevel}/{tech.maxLevel}, " +
                     $"Ресурсы: {tech.collectedResources}/{tech.resourcesPerLevel}{researching}{unlocked}");
        }
    }

    public void ResetAllTechnologies()
    {
        foreach (var tech in allTechnologies)
        {
            tech.currentLevel = 0;
            tech.collectedResources = 0;
            tech.currentProgress = 0f;
            tech.isResearching = false;
            tech.isUnlocked = true;
        }
        PlayerPrefs.DeleteAll();
        Debug.Log("Все технологии сброшены");
        UpdateTechnologyUI();
    }

    #endregion
}