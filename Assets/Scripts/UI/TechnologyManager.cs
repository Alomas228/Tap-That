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
        public string resourceId;
        public int amount;
    }

    [System.Serializable]
    public class Technology
    {
        [Header("Основные настройки")]
        public string id;
        public string displayName;
        public string description;
        public TechType techType = TechType.MultiLevel;
        public int maxLevel = 10;
        public int currentLevel = 0;

        [Header("Текущий прогресс исследования")]
        public int currentResourceAmount = 0;
        public int requiredResourceAmount = 0;
        public bool isResearchInProgress = false;

        [Header("Требования к разблокировке")]
        public List<string> requiredTechIds = new List<string>();
        public List<int> requiredTechLevels = new List<int>();

        [Header("Прогрессивная стоимость")]
        public bool hasProgressiveCost = false;
        public int baseCostWarmleaf = 0;
        public int baseCostThunderite = 0;
        public int baseCostMirallite = 0;
        public float costMultiplierPerLevel = 1.0f;
        public CostProgressionType costProgressionType = CostProgressionType.Linear;

        [Header("Бонусы")]
        public float clickWarmleafBonus = 0;
        public float clickMiralliteBonus = 0;
        public float clickThunderiteBonus = 0;
        public float workerWarmleafBonus = 0;
        public float workerThunderiteBonus = 0;
        public float workerMiralliteBonus = 0;
        public int carryCapacityBonus = 0;
        public float colonistSpeedBonus = 0;
        public int colonistCapacityBonus = 0;
        public float researchSpeedBonus = 0;
        public int miralliteDurabilityBonus = 0;
        public float miralliteRegenBonus = 0;
        public int researchSlotsBonus = 0;
        public float anomalyDurationBonus = 0;
        public float colonistWeightBonus = 0;
        public int enricherCapacityBonus = 0;
        public float researchSaveChanceBonus = 0;
        public float foodConsumptionReduction = 0;

        [Header("Эффекты разблокировки")]
        public bool unlocksThunderite = false;
        public bool unlocksEnricher = false;
        public bool unlocksThunderiteStation = false;

        public enum TechType
        {
            MultiLevel,
            Unlock,
        }

        public enum CostProgressionType
        {
            Linear,
            Exponential,
            Percentage
        }

        public void InitializeForLevel(int level)
        {
            if (level <= 0 || level > maxLevel) return;

            requiredResourceAmount = CalculateTotalCostForLevel(level);
        }

        public int CalculateTotalCostForLevel(int level)
        {
            int warmleafCost = GetCostForLevel("warmleaf", level);
            int thunderiteCost = GetCostForLevel("thunderite", level);
            int miralliteCost = GetCostForLevel("mirallite", level);

            return warmleafCost + thunderiteCost + miralliteCost;
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
            if (requiredTechIds == null || requiredTechIds.Count == 0)
                return true;

            if (requiredTechLevels == null || requiredTechIds.Count != requiredTechLevels.Count)
            {
                Debug.LogError($"Технология {displayName}: несоответствие требований!");
                return false;
            }

            for (int i = 0; i < requiredTechIds.Count; i++)
            {
                Technology requiredTech = TechnologyManager.Instance.GetTechnology(requiredTechIds[i]);
                if (requiredTech == null)
                {
                    Debug.LogWarning($"Технология {displayName}: не найдена требуемая технология {requiredTechIds[i]}");
                    return false;
                }

                int requiredLevel = requiredTechLevels[i];
                if (requiredTech.currentLevel < requiredLevel)
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

        public float GetProgressPercentage()
        {
            if (requiredResourceAmount <= 0) return 0f;
            return (float)currentResourceAmount / requiredResourceAmount;
        }

        public bool IsReadyForResearch()
        {
            return currentLevel < maxLevel &&
                   isResearchInProgress &&
                   HasRequirementsMet() &&
                   !string.IsNullOrEmpty(GetPrimaryResourceType());
        }

        public bool CanStartResearch()
        {
            return currentLevel < maxLevel &&
                   HasRequirementsMet();
        }

        public string GetPrimaryResourceType()
        {
            int warmleafCost = GetCostForLevel("warmleaf", currentLevel + 1);
            int thunderiteCost = GetCostForLevel("thunderite", currentLevel + 1);
            int miralliteCost = GetCostForLevel("mirallite", currentLevel + 1);

            if (warmleafCost > 0) return "warmleaf";
            if (thunderiteCost > 0) return "thunderite";
            if (miralliteCost > 0) return "mirallite";
            return "warmleaf";
        }

        public bool AcceptResource(string resourceType, int amount)
        {
            if (!IsReadyForResearch()) return false;
            if (GetPrimaryResourceType() != resourceType) return false;

            currentResourceAmount += amount;

            if (currentResourceAmount >= requiredResourceAmount)
            {
                CompleteResearch();
                return true;
            }

            return false;
        }

        public void StopResearch()
        {
            // Просто выключаем флаг исследования, прогресс сохраняется
            isResearchInProgress = false;
            Debug.Log($"Исследование {displayName} остановлено. Прогресс: {currentResourceAmount}/{requiredResourceAmount}");
        }

        public void StartResearch()
        {
            // Включаем исследование
            isResearchInProgress = true;

            // Если это первый раз, когда начинаем исследование этого уровня, инициализируем
            if (currentResourceAmount == 0 && requiredResourceAmount == 0)
            {
                InitializeForLevel(currentLevel + 1);
            }

            Debug.Log($"Исследование {displayName} начато/возобновлено. Прогресс: {currentResourceAmount}/{requiredResourceAmount}");
        }

        private void CompleteResearch()
        {
            currentLevel++;
            currentResourceAmount = 0;
            isResearchInProgress = false;

            if (currentLevel >= maxLevel)
            {
                Debug.Log($"{displayName} достиг максимального уровня!");
            }
            else
            {
                InitializeForLevel(currentLevel + 1);
            }

            TechnologyManager.Instance.SaveTechnologyProgress();
            TechnologyManager.Instance.ApplyTechnologyBonuses(this);

            Debug.Log($"{displayName} повышен до уровня {currentLevel}!");
        }
    }

    [Header("Все технологии")]
    public List<Technology> allTechnologies = new List<Technology>();

    [Header("Текущее выбранное исследование")]
    public Technology selectedTechnology = null;
    public Technology activeResearchingTechnology = null; // Активно исследуемая технология в данный момент

    [Header("Ссылки на UI")]
    public GameObject technologyPanel;
    public TextMeshProUGUI techNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI requirementsText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI effectText;
    public TextMeshProUGUI progressText;
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
        if (allTechnologies.Count == 0)
        {
            CreateAllTechnologies();
        }

        SetupUI();
        LoadTechnologyProgress();

        // Инициализируем все технологии
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel < tech.maxLevel)
            {
                // Если есть сохраненный прогресс, не переинициализируем
                if (tech.currentResourceAmount == 0 && tech.requiredResourceAmount == 0)
                {
                    tech.InitializeForLevel(tech.currentLevel + 1);
                }
            }
        }

        // Находим активное исследование (если есть)
        foreach (var tech in allTechnologies)
        {
            if (tech.isResearchInProgress)
            {
                activeResearchingTechnology = tech;
                Debug.Log($"Восстановлено активное исследование: {tech.displayName}");
                break;
            }
        }

        UpdateTechnologyUI();
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
            isResearchInProgress = false,
            hasProgressiveCost = true,
            costProgressionType = Technology.CostProgressionType.Linear,
            baseCostWarmleaf = 50,
            costMultiplierPerLevel = 1.0f,
            clickWarmleafBonus = 1
        };
        captainAxe.InitializeForLevel(1);
        allTechnologies.Add(captainAxe);

        // 2. Сбор образцов
        var sampleCollection = new Technology
        {
            id = "sample_collection",
            displayName = "Сбор образцов",
            description = "Эффективный сбор мираллита",
            techType = Technology.TechType.MultiLevel,
            maxLevel = 20,
            isResearchInProgress = false,
            hasProgressiveCost = true,
            costProgressionType = Technology.CostProgressionType.Linear,
            baseCostWarmleaf = 60,
            costMultiplierPerLevel = 1.0f,
            clickMiralliteBonus = 1
        };
        sampleCollection.InitializeForLevel(1);
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
            costMultiplierPerLevel = 0.75f,
            workerWarmleafBonus = 1
        };
        autoLumber.InitializeForLevel(1);
        allTechnologies.Add(autoLumber);

        // 4. Полимерные топоры
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
            costMultiplierPerLevel = 0.333f,
            miralliteDurabilityBonus = 5
        };
        polymerAxes.InitializeForLevel(1);
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
            costMultiplierPerLevel = 1.5f,
            miralliteRegenBonus = 0.5f
        };
        fastConnections.InitializeForLevel(1);
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
        geoSurvey.InitializeForLevel(1);
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
        miralliteEnrichment.InitializeForLevel(1);
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
            costMultiplierPerLevel = 0.5f,
            researchSlotsBonus = 1 // +1 слот за каждый уровень
        };
        modularTerminals.InitializeForLevel(1);
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
            costMultiplierPerLevel = 0.5f,
            clickThunderiteBonus = 1
        };
        pneumaticPicks.InitializeForLevel(1);
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
            costMultiplierPerLevel = 0.333f,
            workerThunderiteBonus = 1
        };
        heavyDrilling.InitializeForLevel(1);
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
            costMultiplierPerLevel = 20f,
            carryCapacityBonus = 1
        };
        bigBackpacks.InitializeForLevel(1);
        allTechnologies.Add(bigBackpacks);

        // 12. Снижение голода
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
            costMultiplierPerLevel = 2.0f,
            foodConsumptionReduction = 0.1f
        };
        sawdustSoup.InitializeForLevel(1);
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
        miralliteStabilization.InitializeForLevel(1);
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
            costMultiplierPerLevel = 2.0f,
            colonistWeightBonus = 0.1f
        };
        skilledWorkers.InitializeForLevel(1);
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
            costMultiplierPerLevel = 2.0f,
            enricherCapacityBonus = 1
        };
        enricherWorkplaces.InitializeForLevel(1);
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
        researchStabilizer.InitializeForLevel(1);
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
            costMultiplierPerLevel = 1.3f,
            colonistSpeedBonus = 5.0f
        };
        exoskeleton.InitializeForLevel(1);
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
            costMultiplierPerLevel = 50f,
            colonistCapacityBonus = 1
        };
        bunkBeds.InitializeForLevel(1);
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

    void Update()
    {
        if (Time.frameCount % 6 == 0 && selectedTechnology != null)
        {
            UpdateTechnologyUI();
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
            ShowNotification("Требования не выполнены!");
            return;
        }

        // Если эта технология уже исследуется - останавливаем ее
        if (selectedTechnology.isResearchInProgress)
        {
            selectedTechnology.StopResearch();
            activeResearchingTechnology = null;
            ShowNotification($"Исследование {selectedTechnology.displayName} остановлено");
        }
        else
        {
            // Если есть другая активная технология - останавливаем ее
            if (activeResearchingTechnology != null && activeResearchingTechnology != selectedTechnology)
            {
                activeResearchingTechnology.StopResearch();
                ShowNotification($"Исследование {activeResearchingTechnology.displayName} приостановлено");
            }

            // Начинаем/возобновляем исследование выбранной технологии
            selectedTechnology.StartResearch();
            activeResearchingTechnology = selectedTechnology;
            ShowNotification($"Исследование {selectedTechnology.displayName} начато");
        }

        SaveTechnologyProgress();
        UpdateTechnologyUI();

        Debug.Log($"Активное исследование: {activeResearchingTechnology?.displayName}");
    }

    public bool AddResearchProgress(string resourceType, int amount)
    {
        // Добавляем прогресс только в активную технологию
        if (activeResearchingTechnology != null &&
            activeResearchingTechnology.IsReadyForResearch() &&
            activeResearchingTechnology.GetPrimaryResourceType() == resourceType)
        {
            bool completed = activeResearchingTechnology.AcceptResource(resourceType, amount);
            if (completed)
            {
                ShowNotification($"Завершено исследование: {activeResearchingTechnology.displayName} (ур. {activeResearchingTechnology.currentLevel})");

                // После завершения исследования, активная технология сбрасывается
                activeResearchingTechnology = null;

                // Ищем следующую доступную технологию для авто-переключения
                Technology nextTech = FindNextAvailableTechnology();
                if (nextTech != null && nextTech.CanStartResearch())
                {
                    nextTech.StartResearch();
                    activeResearchingTechnology = nextTech;
                    ShowNotification($"Автоматически начато исследование: {nextTech.displayName}");
                }
            }
            UpdateTechnologyUI();
            return true;
        }
        return false;
    }

    Technology FindNextAvailableTechnology()
    {
        foreach (var tech in allTechnologies)
        {
            if (tech != activeResearchingTechnology &&
                tech.CanStartResearch() &&
                !tech.isResearchInProgress)
            {
                return tech;
            }
        }
        return null;
    }

    void UpdateTechnologyUI()
    {
        if (selectedTechnology == null) return;

        if (techNameText != null)
            techNameText.text = selectedTechnology.displayName;

        if (descriptionText != null)
            descriptionText.text = selectedTechnology.description;

        if (levelText != null)
            levelText.text = $"Уровень: {selectedTechnology.currentLevel}/{selectedTechnology.maxLevel}";

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
            requirementsText.text = selectedTechnology.GetRequirementsString();

        if (effectText != null)
            effectText.text = selectedTechnology.GetEffectDescription();

        if (progressText != null)
        {
            if (selectedTechnology.isResearchInProgress)
            {
                progressText.text = $"Прогресс: {selectedTechnology.currentResourceAmount}/{selectedTechnology.requiredResourceAmount}";
                progressText.color = Color.cyan;
            }
            else if (selectedTechnology.currentResourceAmount > 0)
            {
                progressText.text = $"Сохраненный прогресс: {selectedTechnology.currentResourceAmount}/{selectedTechnology.requiredResourceAmount}";
                progressText.color = Color.yellow;
            }
            else
            {
                progressText.text = "Не исследуется";
                progressText.color = Color.gray;
            }
        }

        if (progressSlider != null)
        {
            bool showSlider = selectedTechnology.isResearchInProgress || selectedTechnology.currentResourceAmount > 0;
            progressSlider.gameObject.SetActive(showSlider);
            if (showSlider)
            {
                progressSlider.value = selectedTechnology.GetProgressPercentage();
            }
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
            else if (selectedTechnology.isResearchInProgress)
            {
                statusText.text = "Активное исследование";
                statusText.color = Color.green;
            }
            else if (selectedTechnology.currentResourceAmount > 0)
            {
                statusText.text = "Есть сохраненный прогресс";
                statusText.color = Color.yellow;
            }
            else if (selectedTechnology.CanStartResearch())
            {
                statusText.text = "Готово к исследованию";
                statusText.color = Color.cyan;
            }
            else
            {
                statusText.text = "Недоступно";
                statusText.color = Color.gray;
            }
        }

        if (researchButton != null)
        {
            bool canToggleResearch = selectedTechnology.CanStartResearch();
            researchButton.interactable = canToggleResearch;

            TextMeshProUGUI buttonText = researchButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                if (selectedTechnology.isResearchInProgress)
                {
                    buttonText.text = "Стоп";
                    buttonText.color = Color.black;
                }
                else if (selectedTechnology.currentResourceAmount > 0)
                {
                    buttonText.text = "Изучить";
                    buttonText.color = Color.black;
                }
                else if (canToggleResearch)
                {
                    buttonText.text = "Исследовать";
                    buttonText.color = Color.black;
                }
                else
                {
                    buttonText.text = "Недоступно";
                    buttonText.color = Color.gray;
                }
            }
        }

        if (researchActiveIndicator != null)
        {
            researchActiveIndicator.SetActive(selectedTechnology.isResearchInProgress);
        }
    }

    public Technology GetTechnology(string techId)
    {
        return allTechnologies.Find(t => t.id == techId);
    }

    // Метод для совместимости с ResearchStationWorker.cs
    public Technology GetCurrentResearch()
    {
        return activeResearchingTechnology;
    }

    // Алиас для GetCurrentResearch()
    public Technology GetActiveResearch()
    {
        return activeResearchingTechnology;
    }

    public bool IsAnyTechnologyResearching()
    {
        return activeResearchingTechnology != null && activeResearchingTechnology.isResearchInProgress;
    }

    void SaveTechnologyProgress()
    {
        foreach (var tech in allTechnologies)
        {
            PlayerPrefs.SetInt($"tech_{tech.id}_level", tech.currentLevel);
            PlayerPrefs.SetInt($"tech_{tech.id}_progress", tech.currentResourceAmount);
            PlayerPrefs.SetInt($"tech_{tech.id}_researching", tech.isResearchInProgress ? 1 : 0);
        }
        PlayerPrefs.Save();
    }

    void LoadTechnologyProgress()
    {
        foreach (var tech in allTechnologies)
        {
            tech.currentLevel = PlayerPrefs.GetInt($"tech_{tech.id}_level", 0);
            tech.currentResourceAmount = PlayerPrefs.GetInt($"tech_{tech.id}_progress", 0);
            tech.isResearchInProgress = PlayerPrefs.GetInt($"tech_{tech.id}_researching", 0) == 1;

            // Инициализируем requiredResourceAmount если есть прогресс
            if (tech.currentResourceAmount > 0 && tech.currentLevel < tech.maxLevel)
            {
                tech.requiredResourceAmount = tech.CalculateTotalCostForLevel(tech.currentLevel + 1);
            }
            else if (tech.currentLevel < tech.maxLevel)
            {
                tech.InitializeForLevel(tech.currentLevel + 1);
            }
        }
    }

    public void ApplyTechnologyBonuses(Technology tech)
    {
        Debug.Log($"Применены бонусы от технологии: {tech.displayName} (ур. {tech.currentLevel})");
    }

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

    #region ПУБЛИЧНЫЕ МЕТОДЫ ДЛЯ БОНУСОВ

    public int GetClickWarmleafBonus()
    {
        int totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0) totalBonus += Mathf.RoundToInt(tech.clickWarmleafBonus * tech.currentLevel);
        }
        return totalBonus;
    }

    public int GetClickMiralliteBonus()
    {
        int totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0) totalBonus += Mathf.RoundToInt(tech.clickMiralliteBonus * tech.currentLevel);
        }
        return totalBonus;
    }

    public int GetClickThunderiteBonus()
    {
        int totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0) totalBonus += Mathf.RoundToInt(tech.clickThunderiteBonus * tech.currentLevel);
        }
        return totalBonus;
    }

    public int GetWorkerWarmleafBonus()
    {
        int totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0) totalBonus += Mathf.RoundToInt(tech.workerWarmleafBonus * tech.currentLevel);
        }
        return totalBonus;
    }

    public int GetWorkerThunderiteBonus()
    {
        int totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0) totalBonus += Mathf.RoundToInt(tech.workerThunderiteBonus * tech.currentLevel);
        }
        return totalBonus;
    }

    public int GetWorkerMiralliteBonus()
    {
        int totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0) totalBonus += Mathf.RoundToInt(tech.workerMiralliteBonus * tech.currentLevel);
        }
        return totalBonus;
    }

    public int GetCarryCapacityBonus()
    {
        int totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0) totalBonus += tech.carryCapacityBonus * tech.currentLevel;
        }
        return totalBonus;
    }

    public float GetColonistSpeedBonus()
    {
        float totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0) totalBonus += tech.colonistSpeedBonus * tech.currentLevel;
        }
        return totalBonus;
    }

    public int GetColonistCapacityBonus()
    {
        int totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0) totalBonus += tech.colonistCapacityBonus * tech.currentLevel;
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
                // Каждый уровень технологии дает +1 слот
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
            if (tech.currentLevel > 0) totalBonus += tech.anomalyDurationBonus * tech.currentLevel;
        }
        return totalBonus;
    }

    public float GetColonistWeightBonus()
    {
        float totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0) totalBonus += tech.colonistWeightBonus * tech.currentLevel;
        }
        return totalBonus;
    }

    public int GetEnricherCapacityBonus()
    {
        int totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0) totalBonus += tech.enricherCapacityBonus * tech.currentLevel;
        }
        return totalBonus;
    }

    public float GetResearchSaveChanceBonus()
    {
        float totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0) totalBonus += tech.researchSaveChanceBonus * tech.currentLevel;
        }
        return totalBonus;
    }

    public float GetFoodConsumptionReduction()
    {
        float totalReduction = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0) totalReduction += tech.foodConsumptionReduction * tech.currentLevel;
        }
        return totalReduction;
    }

    public int GetMiralliteDurabilityBonus()
    {
        int totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0) totalBonus += tech.miralliteDurabilityBonus * tech.currentLevel;
        }
        return totalBonus;
    }

    public float GetMiralliteRegenBonus()
    {
        float totalBonus = 0;
        foreach (var tech in allTechnologies)
        {
            if (tech.currentLevel > 0) totalBonus += tech.miralliteRegenBonus * tech.currentLevel;
        }
        return totalBonus;
    }

    #endregion
}