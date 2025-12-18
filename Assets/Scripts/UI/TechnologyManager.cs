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
        public int amount;             // Количество ресурса
    }

    [System.Serializable]
    public class Technology
    {
        [Header("Основные настройки")]
        public string id;                      // Уникальный ID
        public string displayName;             // Отображаемое имя
        public string description;             // Описание для UI
        public TechType techType = TechType.MultiLevel; // Тип технологии
        public int maxLevel = 10;              // Максимальный уровень
        public int currentLevel = 0;           // Текущий уровень

        [Header("Требования к разблокировке")]
        public List<string> requiredTechIds = new List<string>(); // ID необходимых технологий
        public List<int> requiredTechLevels = new List<int>(); // Уровни необходимых технологий

        [Header("Прогресс исследования")]
        public bool isResearching = false;
        public bool isUnlocked = false;

        [Header("Прогрессивная стоимость")]
        public bool hasProgressiveCost = false;
        public int baseCostWarmleaf = 0;
        public int baseCostThunderite = 0;
        public int baseCostMirallite = 0;
        public float costMultiplierPerLevel = 1.0f;
        public CostProgressionType costProgressionType = CostProgressionType.Linear;

        [Header("Бонусы")]
        public float clickWarmleafBonus = 0;     // +X теплолиста за клик
        public float clickMiralliteBonus = 0;    // +X мираллита за клик
        public float clickThunderiteBonus = 0;   // +X грозалита за клик
        public float workerWarmleafBonus = 0;    // +X теплолиста за цикл рабочего
        public float workerThunderiteBonus = 0;  // +X грозалита за цикл рабочего
        public float workerMiralliteBonus = 0;   // +X мираллита за цикл рабочего
        public int carryCapacityBonus = 0;       // +X к вместимости рюкзака
        public float colonistSpeedBonus = 0;     // +X% скорости колонистов
        public int colonistCapacityBonus = 0;    // +X вместимость жилища
        public float researchSpeedBonus = 0;     // +X% скорости исследования
        public int miralliteDurabilityBonus = 0; // +X прочности мираллима
        public float miralliteRegenBonus = 0;    // +X восстановления мираллима
        public int researchSlotsBonus = 0;       // +X рабочих мест в лаборатории
        public float anomalyDurationBonus = 0;   // +X времени аномалий мираллима
        public float colonistWeightBonus = 0;    // +X веса колониста в обогатителе
        public int enricherCapacityBonus = 0;    // +X вместимости обогатителя
        public float researchSaveChanceBonus = 0; // +X% шанса не потратить ресурсы
        public float foodConsumptionReduction = 0; // -X потребления пищи

        [Header("Эффекты разблокировки")]
        public bool unlocksThunderite = false;   // Разблокирует грозалит
        public bool unlocksEnricher = false;     // Разблокирует обогатитель
        public bool unlocksThunderiteStation = false; // Разблокирует станцию добычи грозалита

        public enum TechType
        {
            MultiLevel,  // Многоуровневая
            Unlock,      // Разблокировка (одноразовая)
        }

        public enum CostProgressionType
        {
            Linear,      // Линейный рост: baseCost + (costPerLevel * level)
            Exponential, // Экспоненциальный: baseCost * (multiplier ^ level)
            Percentage   // Процентный рост: baseCost + (baseCost * percentage * level)
        }

        public string GetCostString(int level = -1)
        {
            if (level == -1) level = currentLevel + 1;
            if (level > maxLevel) return "Макс. уровень";

            int warmleafCost = GetCostForLevel("warmleaf", level);
            int thunderiteCost = GetCostForLevel("thunderite", level);
            int miralliteCost = GetCostForLevel("mirallite", level);

            string costStr = "";
            if (warmleafCost > 0) costStr += $"{warmleafCost} Т ";
            if (thunderiteCost > 0) costStr += $"{thunderiteCost} Г ";
            if (miralliteCost > 0) costStr += $"{miralliteCost} М ";

            return costStr.Trim();
        }

        public int GetCostForLevel(string resourceType, int level)
        {
            if (level <= 0 || level > maxLevel) return 0;

            int baseCost = 0;
            switch (resourceType)
            {
                case "warmleaf": baseCost = baseCostWarmleaf; break;
                case "thunderite": baseCost = baseCostThunderite; break;
                case "mirallite": baseCost = baseCostMirallite; break;
            }

            if (!hasProgressiveCost || level == 1) return baseCost;

            switch (costProgressionType)
            {
                case CostProgressionType.Linear:
                    return Mathf.RoundToInt(baseCost + (baseCost * costMultiplierPerLevel * (level - 1)));

                case CostProgressionType.Exponential:
                    return Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplierPerLevel, level - 1));

                case CostProgressionType.Percentage:
                    float percentage = costMultiplierPerLevel / 100f;
                    return Mathf.RoundToInt(baseCost * Mathf.Pow(1 + percentage, level - 1));

                default:
                    return baseCost;
            }
        }

        public bool HasRequirementsMet()
        {
            if (requiredTechIds.Count == 0) return true;

            for (int i = 0; i < requiredTechIds.Count; i++)
            {
                Technology requiredTech = TechnologyManager.Instance.GetTechnology(requiredTechIds[i]);
                if (requiredTech == null || requiredTech.currentLevel < requiredTechLevels[i])
                    return false;
            }

            return true;
        }

        public string GetRequirementsString()
        {
            if (requiredTechIds.Count == 0) return "Нет требований";

            string reqStr = "Требуется:\n";
            for (int i = 0; i < requiredTechIds.Count; i++)
            {
                Technology tech = TechnologyManager.Instance.GetTechnology(requiredTechIds[i]);
                if (tech != null)
                {
                    reqStr += $"{tech.displayName} (ур. {requiredTechLevels[i]})\n";
                }
            }
            return reqStr.Trim();
        }

        public string GetEffectDescription()
        {
            string effectStr = description + "\n\n";

            if (clickWarmleafBonus > 0) effectStr += $"+{clickWarmleafBonus} Т за клик\n";
            if (clickMiralliteBonus > 0) effectStr += $"+{clickMiralliteBonus} М за клик\n";
            if (clickThunderiteBonus > 0) effectStr += $"+{clickThunderiteBonus} Г за клик\n";
            if (workerWarmleafBonus > 0) effectStr += $"+{workerWarmleafBonus} Т за рабочий цикл\n";
            if (workerThunderiteBonus > 0) effectStr += $"+{workerThunderiteBonus} Г за рабочий цикл\n";
            if (workerMiralliteBonus > 0) effectStr += $"+{workerMiralliteBonus} М за рабочий цикл\n";
            if (carryCapacityBonus > 0) effectStr += $"+{carryCapacityBonus} вместимость рюкзака\n";
            if (colonistSpeedBonus > 0) effectStr += $"+{colonistSpeedBonus}% скорости колонистов\n";
            if (colonistCapacityBonus > 0) effectStr += $"+{colonistCapacityBonus} вместимость жилища\n";
            if (researchSpeedBonus > 0) effectStr += $"+{researchSpeedBonus}% скорости исследования\n";
            if (miralliteDurabilityBonus > 0) effectStr += $"+{miralliteDurabilityBonus} прочности мираллима\n";
            if (miralliteRegenBonus > 0) effectStr += $"+{miralliteRegenBonus} восстановления мираллима\n";
            if (researchSlotsBonus > 0) effectStr += $"+{researchSlotsBonus} рабочих мест в лаборатории\n";
            if (anomalyDurationBonus > 0) effectStr += $"+{anomalyDurationBonus}с времени аномалий\n";
            if (colonistWeightBonus > 0) effectStr += $"+{colonistWeightBonus} веса колониста\n";
            if (enricherCapacityBonus > 0) effectStr += $"+{enricherCapacityBonus} вместимости обогатителя\n";
            if (researchSaveChanceBonus > 0) effectStr += $"+{researchSaveChanceBonus}% сохранения ресурсов\n";
            if (foodConsumptionReduction > 0) effectStr += $"-{foodConsumptionReduction} потребления пищи\n";

            if (unlocksThunderite) effectStr += "Разблокирует Грозалит\n";
            if (unlocksEnricher) effectStr += "Разблокирует Обогатитель\n";
            if (unlocksThunderiteStation) effectStr += "Разблокирует Станцию добычи Грозалита\n";

            return effectStr.Trim();
        }
    }

    [Header("Все технологии")]
    public List<Technology> allTechnologies = new List<Technology>();

    [Header("Текущее выбранное исследование")]
    public Technology selectedTechnology = null;

    [Header("Ссылки на UI")]
    public GameObject technologyPanel;
    public TextMeshProUGUI techNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI requirementsText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI effectText;
    public Slider progressSlider;
    public Button researchButton;
    public Button closeButton;
    public GameObject researchActiveIndicator;

    [Header("Уведомления")]
    [SerializeField] private float notificationDuration = 3f;

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
        // Создаем все технологии если список пуст
        if (allTechnologies.Count == 0)
        {
            CreateAllTechnologies();
        }

        SetupUI();
        UpdateTechnologyUI();
        LoadTechnologyProgress();
    }

    void CreateAllTechnologies()
    {
        allTechnologies.Clear();

        // 1. Капитанский топор
        var captainAxe = new Technology
        {
            id = "captain_axe",
            displayName = "Капитанский топор",
            description = "Улучшенные инструменты для сбора теплолиста",
            techType = Technology.TechType.MultiLevel,
            maxLevel = 20,
            isUnlocked = true,
            hasProgressiveCost = true,
            costProgressionType = Technology.CostProgressionType.Linear,
            baseCostWarmleaf = 50,
            costMultiplierPerLevel = 1.0f, // 50 Т + (50 Т × Уровень)
            clickWarmleafBonus = 1
        };
        allTechnologies.Add(captainAxe);

        // 2. Сбор образцов
        var sampleCollection = new Technology
        {
            id = "sample_collection",
            displayName = "Сбор образцов",
            description = "Эффективный сбор мираллита",
            techType = Technology.TechType.MultiLevel,
            maxLevel = 20,
            isUnlocked = true,
            hasProgressiveCost = true,
            costProgressionType = Technology.CostProgressionType.Linear,
            baseCostWarmleaf = 60,
            costMultiplierPerLevel = 1.0f, // 60 Т + (60 Т × Уровень)
            clickMiralliteBonus = 1
        };
        allTechnologies.Add(sampleCollection);

        // 3. Автоматизация лесоповала
        var autoLumber = new Technology
        {
            id = "auto_lumber",
            displayName = "Автоматизация лесоповала",
            description = "Механизированная добыча теплолиста",
            techType = Technology.TechType.MultiLevel,
            maxLevel = 10,
            requiredTechIds = new List<string> { "polymer_axes" },
            requiredTechLevels = new List<int> { 1 },
            hasProgressiveCost = true,
            costProgressionType = Technology.CostProgressionType.Linear,
            baseCostWarmleaf = 200,
            costMultiplierPerLevel = 0.75f, // 200 Т + (150 Т × Уровень)
            workerWarmleafBonus = 1
        };
        allTechnologies.Add(autoLumber);

        // 4. Прочные соединения (Полимерные топоры - базовая)
        var polymerAxes = new Technology
        {
            id = "polymer_axes",
            displayName = "Полимерные топоры",
            description = "Прочные инструменты для работы",
            techType = Technology.TechType.MultiLevel,
            maxLevel = 10,
            requiredTechIds = new List<string> { "sample_collection" },
            requiredTechLevels = new List<int> { 1 },
            hasProgressiveCost = true,
            costProgressionType = Technology.CostProgressionType.Linear,
            baseCostWarmleaf = 300,
            costMultiplierPerLevel = 0.333f, // 300 Т + (100 × Уровень)
            miralliteDurabilityBonus = 5
        };
        allTechnologies.Add(polymerAxes);

        // 5. Быстрые связи
        var fastConnections = new Technology
        {
            id = "fast_connections",
            displayName = "Быстрые связи",
            description = "Ускоренное восстановление ресурсов",
            techType = Technology.TechType.MultiLevel,
            maxLevel = 5,
            requiredTechIds = new List<string> { "sample_collection" },
            requiredTechLevels = new List<int> { 1 },
            hasProgressiveCost = true,
            costProgressionType = Technology.CostProgressionType.Exponential,
            baseCostWarmleaf = 500,
            costMultiplierPerLevel = 1.5f, // 500 Т × (1.5 ^ Уровень)
            miralliteRegenBonus = 0.5f
        };
        allTechnologies.Add(fastConnections);

        // 6. Георазведка
        var geoSurvey = new Technology
        {
            id = "geo_survey",
            displayName = "Георазведка",
            description = "Обнаружение новых ресурсов",
            techType = Technology.TechType.Unlock,
            maxLevel = 1,
            requiredTechIds = new List<string> { "auto_lumber" },
            requiredTechLevels = new List<int> { 3 },
            hasProgressiveCost = false,
            baseCostWarmleaf = 2500,
            unlocksThunderite = true,
            unlocksThunderiteStation = true
        };
        allTechnologies.Add(geoSurvey);

        // 7. Обогащение Мираллима
        var miralliteEnrichment = new Technology
        {
            id = "mirallite_enrichment",
            displayName = "Обогащение Мираллима",
            description = "Продвинутая обработка мираллита",
            techType = Technology.TechType.Unlock,
            maxLevel = 1,
            requiredTechIds = new List<string> { "polymer_axes", "fast_connections" },
            requiredTechLevels = new List<int> { 2, 2 },
            hasProgressiveCost = false,
            baseCostWarmleaf = 3000,
            baseCostThunderite = 1000,
            unlocksEnricher = true
        };
        allTechnologies.Add(miralliteEnrichment);

        // 8. Модульные терминалы
        var modularTerminals = new Technology
        {
            id = "modular_terminals",
            displayName = "Модульные терминалы",
            description = "Расширение исследовательских мощностей",
            techType = Technology.TechType.MultiLevel,
            maxLevel = 5,
            requiredTechIds = new List<string> { "geo_survey" },
            requiredTechLevels = new List<int> { 1 },
            hasProgressiveCost = true,
            costProgressionType = Technology.CostProgressionType.Linear,
            baseCostWarmleaf = 1000,
            baseCostThunderite = 500,
            costMultiplierPerLevel = 0.5f, // +500/+250 каждый уровень
            researchSlotsBonus = 1
        };
        allTechnologies.Add(modularTerminals);

        // 9. Пневматические кирки
        var pneumaticPicks = new Technology
        {
            id = "pneumatic_picks",
            displayName = "Пневматические кирки",
            description = "Эффективная добыча грозалита",
            techType = Technology.TechType.MultiLevel,
            maxLevel = 20,
            requiredTechIds = new List<string> { "geo_survey" },
            requiredTechLevels = new List<int> { 1 },
            hasProgressiveCost = true,
            costProgressionType = Technology.CostProgressionType.Linear,
            baseCostWarmleaf = 100,
            baseCostThunderite = 100,
            costMultiplierPerLevel = 0.5f, // +50 Т и Г за уровень
            clickThunderiteBonus = 1
        };
        allTechnologies.Add(pneumaticPicks);

        // 10. Тяжелое бурение
        var heavyDrilling = new Technology
        {
            id = "heavy_drilling",
            displayName = "Тяжелое бурение",
            description = "Промышленная добыча грозалита",
            techType = Technology.TechType.MultiLevel,
            maxLevel = 10,
            requiredTechIds = new List<string> { "geo_survey" },
            requiredTechLevels = new List<int> { 1 },
            hasProgressiveCost = true,
            costProgressionType = Technology.CostProgressionType.Linear,
            baseCostWarmleaf = 500,
            baseCostThunderite = 300,
            costMultiplierPerLevel = 0.333f, // +100 Г за уровень
            workerThunderiteBonus = 1
        };
        allTechnologies.Add(heavyDrilling);

        // 11. Увеличенные рюкзаки
        var bigBackpacks = new Technology
        {
            id = "big_backpacks",
            displayName = "Увеличенные рюкзаки",
            description = "Увеличение грузоподъемности",
            techType = Technology.TechType.MultiLevel,
            maxLevel = 10,
            requiredTechIds = new List<string> { "geo_survey" },
            requiredTechLevels = new List<int> { 1 },
            hasProgressiveCost = true,
            costProgressionType = Technology.CostProgressionType.Percentage,
            baseCostWarmleaf = 1000,
            baseCostThunderite = 1000,
            costMultiplierPerLevel = 20f, // +20% к цене за уровень
            carryCapacityBonus = 1
        };
        allTechnologies.Add(bigBackpacks);

        // 12. Суп из опилок (Снижение голода)
        var sawdustSoup = new Technology
        {
            id = "sawdust_soup",
            displayName = "Снижение голода",
            description = "Оптимизация потребления пищи",
            techType = Technology.TechType.MultiLevel,
            maxLevel = 3,
            requiredTechIds = new List<string> { "geo_survey", "mirallite_enrichment" },
            requiredTechLevels = new List<int> { 1, 1 },
            hasProgressiveCost = true,
            costProgressionType = Technology.CostProgressionType.Exponential,
            baseCostWarmleaf = 5000,
            baseCostThunderite = 2000,
            costMultiplierPerLevel = 2.0f, // Каждый уровень x2
            foodConsumptionReduction = 0.1f
        };
        allTechnologies.Add(sawdustSoup);

        // 13. Стабилизация Мираллима
        var miralliteStabilization = new Technology
        {
            id = "mirallite_stabilization",
            displayName = "Стабилизация Мираллима",
            description = "Продление эффектов аномалий",
            techType = Technology.TechType.MultiLevel,
            maxLevel = 5,
            requiredTechIds = new List<string> { "mirallite_enrichment" },
            requiredTechLevels = new List<int> { 1 },
            hasProgressiveCost = true,
            costProgressionType = Technology.CostProgressionType.Linear,
            baseCostWarmleaf = 2000,
            baseCostThunderite = 2000,
            costMultiplierPerLevel = 1.0f,
            anomalyDurationBonus = 1.0f
        };
        allTechnologies.Add(miralliteStabilization);

        // 14. Квалифицированные сотрудники
        var skilledWorkers = new Technology
        {
            id = "skilled_workers",
            displayName = "Квалифицированные сотрудники",
            description = "Повышение эффективности в обогатителе",
            techType = Technology.TechType.MultiLevel,
            maxLevel = 5,
            requiredTechIds = new List<string> { "mirallite_enrichment" },
            requiredTechLevels = new List<int> { 1 },
            hasProgressiveCost = true,
            costProgressionType = Technology.CostProgressionType.Exponential,
            baseCostWarmleaf = 2000,
            baseCostThunderite = 2000,
            costMultiplierPerLevel = 2.0f, // Каждый уровень x2
            colonistWeightBonus = 0.1f
        };
        allTechnologies.Add(skilledWorkers);

        // 15. Рабочие места в обогатителе
        var enricherWorkplaces = new Technology
        {
            id = "enricher_workplaces",
            displayName = "Рабочие места",
            description = "Расширение мощностей обогатителя",
            techType = Technology.TechType.MultiLevel,
            maxLevel = 10,
            requiredTechIds = new List<string> { "mirallite_enrichment" },
            requiredTechLevels = new List<int> { 1 },
            hasProgressiveCost = true,
            costProgressionType = Technology.CostProgressionType.Exponential,
            baseCostWarmleaf = 1500,
            baseCostThunderite = 1500,
            costMultiplierPerLevel = 2.0f, // Каждый уровень x2
            enricherCapacityBonus = 1
        };
        allTechnologies.Add(enricherWorkplaces);

        // 16. Стабилизатор исследований
        var researchStabilizer = new Technology
        {
            id = "research_stabilizer",
            displayName = "Стабилизатор исследований",
            description = "Снижение потерь при исследованиях",
            techType = Technology.TechType.MultiLevel,
            maxLevel = 10,
            requiredTechIds = new List<string> { "modular_terminals" },
            requiredTechLevels = new List<int> { 1 },
            hasProgressiveCost = false,
            baseCostWarmleaf = 5000,
            baseCostThunderite = 5000,
            researchSaveChanceBonus = 2.0f
        };
        allTechnologies.Add(researchStabilizer);

        // 17. Экзоскелет
        var exoskeleton = new Technology
        {
            id = "exoskeleton",
            displayName = "Экзоскелет",
            description = "Увеличение скорости передвижения",
            techType = Technology.TechType.MultiLevel,
            maxLevel = 10,
            requiredTechIds = new List<string> { "sawdust_soup" },
            requiredTechLevels = new List<int> { 1 },
            hasProgressiveCost = true,
            costProgressionType = Technology.CostProgressionType.Exponential,
            baseCostWarmleaf = 1000,
            baseCostThunderite = 1000,
            costMultiplierPerLevel = 1.3f, // × (1.3 ^ Уровень)
            colonistSpeedBonus = 5.0f
        };
        allTechnologies.Add(exoskeleton);

        // 18. Двухъярусные спальные места
        var bunkBeds = new Technology
        {
            id = "bunk_beds",
            displayName = "Двухъярусные спальные места",
            description = "Увеличение вместимости жилищ",
            techType = Technology.TechType.MultiLevel,
            maxLevel = 5,
            requiredTechIds = new List<string> { "sawdust_soup", "big_backpacks" },
            requiredTechLevels = new List<int> { 1, 1 },
            hasProgressiveCost = true,
            costProgressionType = Technology.CostProgressionType.Percentage,
            baseCostWarmleaf = 3000,
            baseCostThunderite = 1500,
            costMultiplierPerLevel = 50f, // рост х1.5 за уровень
            colonistCapacityBonus = 1
        };
        allTechnologies.Add(bunkBeds);
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

        if (!selectedTechnology.HasRequirementsMet())
        {
            ShowNotification("Не выполнены требования!");
            return;
        }

        if (!selectedTechnology.isUnlocked)
        {
            ShowNotification("Технология не разблокирована!");
            return;
        }

        // Рассчитываем стоимость следующего уровня
        int nextLevel = selectedTechnology.currentLevel + 1;
        int warmleafCost = selectedTechnology.GetCostForLevel("warmleaf", nextLevel);
        int thunderiteCost = selectedTechnology.GetCostForLevel("thunderite", nextLevel);
        int miralliteCost = selectedTechnology.GetCostForLevel("mirallite", nextLevel);

        // Проверяем ресурсы
        if (ResourceManager.Instance != null)
        {
            if (warmleafCost > 0 && ResourceManager.Instance.GetWarmleafAmount() < warmleafCost)
            {
                ShowNotification($"Недостаточно теплолиста! Нужно: {warmleafCost}");
                return;
            }

            if (thunderiteCost > 0 && ResourceManager.Instance.GetThunderiteAmount() < thunderiteCost)
            {
                ShowNotification($"Недостаточно грозалита! Нужно: {thunderiteCost}");
                return;
            }

            if (miralliteCost > 0 && ResourceManager.Instance.GetMiralliteAmount() < miralliteCost)
            {
                ShowNotification($"Недостаточно мираллита! Нужно: {miralliteCost}");
                return;
            }
        }

        // Списание ресурсов
        bool resourcesSpent = true;
        if (warmleafCost > 0) resourcesSpent &= ResourceManager.Instance.TrySpendResource("warmleaf", warmleafCost);
        if (thunderiteCost > 0) resourcesSpent &= ResourceManager.Instance.TrySpendResource("thunderite", thunderiteCost);
        if (miralliteCost > 0) resourcesSpent &= ResourceManager.Instance.TrySpendResource("mirallite", miralliteCost);

        if (!resourcesSpent)
        {
            ShowNotification("Ошибка при списании ресурсов!");
            return;
        }

        // Повышаем уровень
        LevelUpTechnology(selectedTechnology);

        ShowNotification($"{selectedTechnology.displayName} повышен до уровня {selectedTechnology.currentLevel}!");
    }

    void LevelUpTechnology(Technology tech)
    {
        tech.currentLevel++;

        // Применяем бонусы
        ApplyTechnologyBonuses(tech);

        // Если технология разблокирующая - помечаем как завершенную
        if (tech.techType == Technology.TechType.Unlock && tech.currentLevel >= 1)
        {
            tech.isUnlocked = false; // Завершена
        }

        // Сохраняем прогресс
        SaveTechnologyProgress();

        // Обновляем UI
        UpdateTechnologyUI();

        // Проверяем разблокировку новых технологий
        CheckTechnologyUnlocks();

        // Если разблокировали ресурсы/здания - уведомляем систему
        if (tech.unlocksThunderite)
        {
            Debug.Log("Грозалит разблокирован!");
            // Уведомляем другие системы о разблокировке
        }

        if (tech.unlocksEnricher)
        {
            Debug.Log("Обогатитель разблокирован!");
            // Уведомляем BuildingManager
            if (BuildingManager.Instance != null)
            {
                // Разблокируем постройку обогатителя
            }
        }

        if (tech.unlocksThunderiteStation)
        {
            Debug.Log("Станция добычи грозалита разблокирована!");
            // Уведомляем BuildingManager
        }
    }

    void ApplyTechnologyBonuses(Technology tech)
    {
        Debug.Log($"Технология '{tech.displayName}' повышена до уровня {tech.currentLevel}");

        // Применяем бонусы ко всем соответствующим системам
        // (Эта логика будет интегрирована в соответствующие скрипты)

        if (tech.carryCapacityBonus > 0)
        {
            ApplyCarryCapacityBonus(tech.carryCapacityBonus);
        }

        // Здесь можно добавить применение других бонусов
    }

    private void ApplyCarryCapacityBonus(int bonus)
    {
        // Находим всех исследователей и увеличиваем им вместимость
        ResearchStationWorker[] researchers = FindObjectsByType<ResearchStationWorker>(FindObjectsSortMode.None);
        foreach (var researcher in researchers) // Была ошибка в имени переменной
        {
            researcher.IncreaseCarryCapacity(bonus);
        }
        Debug.Log($"Применен бонус вместимости +{bonus} к {researchers.Length} исследователям");
    }

    void CheckTechnologyUnlocks()
    {
        foreach (var tech in allTechnologies)
        {
            if (!tech.isUnlocked && tech.HasRequirementsMet())
            {
                tech.isUnlocked = true;
                Debug.Log($"Технология '{tech.displayName}' разблокирована!");
                ShowNotification($"Разблокирована: {tech.displayName}");
            }
        }
    }

    void Update()
    {
        // Обновляем UI каждые 0.1 секунды
        if (Time.frameCount % 6 == 0 && selectedTechnology != null)
        {
            UpdateTechnologyUI();
        }
    }

    void UpdateTechnologyUI()
    {
        if (selectedTechnology == null) return;

        // Обновляем тексты
        if (techNameText != null)
        {
            techNameText.text = selectedTechnology.displayName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = selectedTechnology.description;
        }

        if (levelText != null)
        {
            levelText.text = $"Уровень: {selectedTechnology.currentLevel}/{selectedTechnology.maxLevel}";
        }

        if (costText != null)
        {
            int nextLevel = selectedTechnology.currentLevel + 1;
            if (nextLevel <= selectedTechnology.maxLevel)
            {
                costText.text = $"Стоимость: {selectedTechnology.GetCostString(nextLevel)}";
            }
            else
            {
                costText.text = "Максимальный уровень";
            }
        }

        if (requirementsText != null)
        {
            requirementsText.text = selectedTechnology.GetRequirementsString();
        }

        if (effectText != null)
        {
            effectText.text = selectedTechnology.GetEffectDescription();
        }

        if (statusText != null)
        {
            if (selectedTechnology.currentLevel >= selectedTechnology.maxLevel)
            {
                statusText.text = "Макс. уровень";
                statusText.color = Color.yellow;
            }
            else if (!selectedTechnology.HasRequirementsMet())
            {
                statusText.text = "Требования не выполнены";
                statusText.color = Color.red;
            }
            else if (!selectedTechnology.isUnlocked)
            {
                statusText.text = "Заблокировано";
                statusText.color = Color.gray;
            }
            else
            {
                statusText.text = "Доступно";
                statusText.color = Color.green;
            }
        }

        // Обновляем кнопку
        if (researchButton != null)
        {
            bool canResearch = selectedTechnology.isUnlocked &&
                              selectedTechnology.currentLevel < selectedTechnology.maxLevel &&
                              selectedTechnology.HasRequirementsMet();

            researchButton.interactable = canResearch;

            TextMeshProUGUI buttonText = researchButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                if (canResearch)
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

    void SaveTechnologyProgress()
    {
        foreach (var tech in allTechnologies)
        {
            PlayerPrefs.SetInt($"tech_{tech.id}_level", tech.currentLevel);
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
            tech.isUnlocked = PlayerPrefs.GetInt($"tech_{tech.id}_unlocked",
                tech.requiredTechIds.Count == 0 ? 1 : 0) == 1;
        }

        // Проверяем разблокировки после загрузки
        CheckTechnologyUnlocks();
        Debug.Log("Прогресс технологий загружен");
    }

    #region ПУБЛИЧНЫЕ МЕТОДЫ


    public bool IsAnyTechnologyResearching()
    {
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel < tech.maxLevel && tech.isUnlocked && tech.HasRequirementsMet())
            {
                // Если есть доступные технологии для исследования
                return true;
            }
        }
        return false;
    }

    public Technology GetResearchingTechnology()
    {
        // Возвращаем первую доступную технологию для исследования
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel < tech.maxLevel && tech.isUnlocked && tech.HasRequirementsMet())
            {
                return tech;
            }
        }
        return null;
    }

    public void RegisterResearcher()
    {
        // В новой системе исследований это не нужно, но оставим для совместимости
        Debug.Log("Исследователь зарегистрирован");
    }

    public void UnregisterResearcher()
    {
        // В новой системе исследований это не нужно, но оставим для совместимости
        Debug.Log("Исследователь удален");
    }

    public Technology GetTechnology(string techId)
    {
        return allTechnologies.Find(t => t.id == techId);
    }

    public int GetClickWarmleafBonus()
    {
        int totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0)
            {
                totalBonus += Mathf.RoundToInt(tech.clickWarmleafBonus * tech.currentLevel);
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
                totalBonus += Mathf.RoundToInt(tech.clickMiralliteBonus * tech.currentLevel);
            }
        }
        return totalBonus;
    }

    public int GetClickThunderiteBonus()
    {
        int totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0)
            {
                totalBonus += Mathf.RoundToInt(tech.clickThunderiteBonus * tech.currentLevel);
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
                totalBonus += Mathf.RoundToInt(tech.workerWarmleafBonus * tech.currentLevel);
            }
        }
        return totalBonus;
    }

    public int GetWorkerThunderiteBonus()
    {
        int totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0)
            {
                totalBonus += Mathf.RoundToInt(tech.workerThunderiteBonus * tech.currentLevel);
            }
        }
        return totalBonus;
    }

    public int GetWorkerMiralliteBonus()
    {
        int totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0)
            {
                totalBonus += Mathf.RoundToInt(tech.workerMiralliteBonus * tech.currentLevel);
            }
        }
        return totalBonus;
    }

    public int GetCarryCapacityBonus()
    {
        int totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0)
            {
                totalBonus += tech.carryCapacityBonus * tech.currentLevel;
            }
        }
        return totalBonus;
    }

    public float GetColonistSpeedBonus()
    {
        float totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0)
            {
                totalBonus += tech.colonistSpeedBonus * tech.currentLevel;
            }
        }
        return totalBonus;
    }

    public int GetColonistCapacityBonus()
    {
        int totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0)
            {
                totalBonus += tech.colonistCapacityBonus * tech.currentLevel;
            }
        }
        return totalBonus;
    }

    public int GetResearchSlotsBonus()
    {
        int totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0)
            {
                totalBonus += tech.researchSlotsBonus * tech.currentLevel;
            }
        }
        return totalBonus;
    }

    public float GetAnomalyDurationBonus()
    {
        float totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0)
            {
                totalBonus += tech.anomalyDurationBonus * tech.currentLevel;
            }
        }
        return totalBonus;
    }

    public float GetColonistWeightBonus()
    {
        float totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0)
            {
                totalBonus += tech.colonistWeightBonus * tech.currentLevel;
            }
        }
        return totalBonus;
    }

    public int GetEnricherCapacityBonus()
    {
        int totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0)
            {
                totalBonus += tech.enricherCapacityBonus * tech.currentLevel;
            }
        }
        return totalBonus;
    }

    public float GetResearchSaveChanceBonus()
    {
        float totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0)
            {
                totalBonus += tech.researchSaveChanceBonus * tech.currentLevel;
            }
        }
        return totalBonus;
    }

    public float GetFoodConsumptionReduction()
    {
        float totalReduction = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0)
            {
                totalReduction += tech.foodConsumptionReduction * tech.currentLevel;
            }
        }
        return totalReduction;
    }

    public int GetMiralliteDurabilityBonus()
    {
        int totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0)
            {
                totalBonus += tech.miralliteDurabilityBonus * tech.currentLevel;
            }
        }
        return totalBonus;
    }

    public float GetMiralliteRegenBonus()
    {
        float totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0)
            {
                totalBonus += tech.miralliteRegenBonus * tech.currentLevel;
            }
        }
        return totalBonus;
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
            string unlocked = tech.isUnlocked ? "[ДОСТУПНО]" : "[ЗАБЛОКИРОВАНО]";
            string requirements = tech.HasRequirementsMet() ? "" : " [ТРЕБОВАНИЯ НЕ ВЫПОЛНЕНЫ]";
            Debug.Log($"{tech.displayName}: Ур. {tech.currentLevel}/{tech.maxLevel} {unlocked}{requirements}");
            Debug.Log($"  След. уровень: {tech.GetCostString(tech.currentLevel + 1)}");
        }
    }

    public void ResetAllTechnologies()
    {
        foreach (var tech in allTechnologies)
        {
            tech.currentLevel = 0;
            tech.isUnlocked = tech.requiredTechIds.Count == 0;
        }
        PlayerPrefs.DeleteAll();
        Debug.Log("Все технологии сброшены");
        UpdateTechnologyUI();
    }

    #endregion
}