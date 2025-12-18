using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ResearchStationWorker : ColonistWorker
{
    [Header("Настройки исследования")]
    [SerializeField] private float interactionTime = 3f;
    [SerializeField] private float baseResearchSaveChance = 0f; // Базовый шанс сохранения ресурсов

    [Header("Визуальные эффекты")]
    [SerializeField] private GameObject resourceIndicator;
    [SerializeField] private ParticleSystem collectEffect;
    [SerializeField] private ParticleSystem researchEffect;

    private string currentResourceType = "";
    private bool hasResource = false;
    private int carryAmount = 1;
    private bool isRegistered = false;

    // Список требований для исследований (теперь храним локально)
    [System.Serializable]
    public class ResearchRequirement
    {
        public string resourceId;
        public int amount;
    }

    // Карта технологий и их требований
    private Dictionary<string, List<ResearchRequirement>> techRequirements = new Dictionary<string, List<ResearchRequirement>>();

    protected override float GetInteractionTime()
    {
        return interactionTime;
    }

    protected override void Start()
    {
        base.Start();

        // Инициализируем требования для технологий
        InitializeTechRequirements();

        // Увеличиваем вместимость за счет технологий
        if (TechnologyManager.Instance != null)
        {
            int capacityBonus = TechnologyManager.Instance.GetCarryCapacityBonus();
            carryAmount += capacityBonus;
            Debug.Log($"{name}: Начальная вместимость увеличена на {capacityBonus}, всего: {carryAmount}");
        }

        if (resourceIndicator != null)
            resourceIndicator.SetActive(false);

        // Запускаем работу
        StartCoroutine(InitializeAndWork());
    }

    private void InitializeTechRequirements()
    {
        techRequirements.Clear();

        // Теплолистовые технологии
        techRequirements["captain_axe"] = new List<ResearchRequirement>
        {
            new ResearchRequirement { resourceId = "warmleaf", amount = 50 }
        };

        // Мираллитовые технологии
        techRequirements["sample_collection"] = new List<ResearchRequirement>
        {
            new ResearchRequirement { resourceId = "mirallite", amount = 60 }
        };

        // Грозалитовые технологии
        techRequirements["pneumatic_picks"] = new List<ResearchRequirement>
        {
            new ResearchRequirement { resourceId = "thunderite", amount = 100 }
        };

        // Смешанные технологии
        techRequirements["big_backpacks"] = new List<ResearchRequirement>
        {
            new ResearchRequirement { resourceId = "warmleaf", amount = 1000 },
            new ResearchRequirement { resourceId = "thunderite", amount = 1000 }
        };
    }

    IEnumerator InitializeAndWork()
    {
        // Ждем инициализации систем
        yield return new WaitForSeconds(1f);

        // Регистрируемся как исследователь
        RegisterAsResearcher();

        // Запускаем основной цикл
        StartCoroutine(WorkCycle());
    }

    IEnumerator WorkCycle()
    {
        Debug.Log($"{name}: Начал работу исследователя");

        while (true)
        {
            // 1. ЖДЕМ АКТИВНОГО ИССЛЕДОВАНИЯ ====================
            while (!IsReadyToWork())
            {
                Debug.Log($"{name}: Ожидание условий для работы...");
                yield return new WaitForSeconds(2f);
            }

            // 2. ИДЕМ В ГЛАВНОЕ ЗДАНИЕ =====================================
            Debug.Log($"{name}: Иду в главное здание");

            currentState = ColonistState.MovingToMainBuilding;

            // Ждем достижения главного здания
            yield return StartCoroutine(MoveToLocation(mainBuilding, 30f));

            if (currentState != ColonistState.DeliveringResources)
            {
                Debug.LogWarning($"{name}: Не дошел до главного здания");
                yield return new WaitForSeconds(2f);
                continue;
            }

            // 3. БЕРЕМ РЕСУРСЫ =============================================
            Debug.Log($"{name}: Беру ресурсы");

            bool gotResource = false;
            int takeAmount = 0;

            // Получаем доступную технологию для исследования
            var currentResearch = GetAvailableTechnology();
            if (currentResearch != null)
            {
                // Получаем тип ресурса для исследования
                currentResourceType = GetResourceTypeForResearch(currentResearch);

                if (string.IsNullOrEmpty(currentResourceType))
                {
                    Debug.Log($"{name}: Не могу определить тип ресурса для исследования");
                    currentState = ColonistState.Idle;
                    yield return new WaitForSeconds(2f);
                    continue;
                }

                // Берем ресурсы
                if (ResourceManager.Instance != null)
                {
                    int available = ResourceManager.Instance.GetResourceAmount(currentResourceType);
                    takeAmount = Mathf.Min(carryAmount, available);

                    // Проверяем шанс сохранения ресурсов
                    float saveChance = GetResearchSaveChance();
                    bool savedResources = Random.Range(0f, 100f) < saveChance;

                    if (savedResources)
                    {
                        Debug.Log($"{name}: Удалось сохранить ресурсы благодаря технологии!");
                        takeAmount = 0; // Не тратим ресурсы
                        gotResource = true;
                    }
                    else if (takeAmount > 0 && ResourceManager.Instance.TrySpendResource(currentResourceType, takeAmount))
                    {
                        gotResource = true;
                        Debug.Log($"{name}: Взял {takeAmount} {currentResourceType}");
                    }

                    if (gotResource)
                    {
                        hasResource = true;

                        // Визуальный индикатор
                        if (resourceIndicator != null)
                        {
                            resourceIndicator.SetActive(true);
                            UpdateResourceIndicator();
                        }

                        if (collectEffect != null)
                            collectEffect.Play();
                    }
                }
            }

            if (!gotResource)
            {
                Debug.Log($"{name}: Не удалось взять ресурсы");
                currentState = ColonistState.Idle;
                yield return new WaitForSeconds(2f);
                continue;
            }

            // 4. ИДЕМ НА ИССЛЕДОВАТЕЛЬСКУЮ СТАНЦИЮ =========================
            Debug.Log($"{name}: Несу ресурсы на станцию");

            currentState = ColonistState.MovingToBuilding;

            // Ждем достижения станции
            yield return StartCoroutine(MoveToLocation(targetBuilding, 30f));

            if (currentState != ColonistState.Interacting)
            {
                Debug.LogWarning($"{name}: Не дошел до станции");
                currentState = ColonistState.Idle;
                yield return new WaitForSeconds(2f);
                continue;
            }

            // 5. ПРОВОДИМ ИССЛЕДОВАНИЕ =====================================
            Debug.Log($"{name}: Начинаю исследование");

            // Скрываем визуал
            if (visualObject != null)
                visualObject.SetActive(false);

            // Ждем время исследования
            yield return new WaitForSeconds(interactionTime);

            // Показываем визуал
            if (visualObject != null)
                visualObject.SetActive(true);

            // "Сдаем" ресурсы на исследование
            if (takeAmount > 0)
            {
                Debug.Log($"{name}: Исследовал {takeAmount} {currentResourceType}");

                if (researchEffect != null)
                    researchEffect.Play();
            }

            // Сбрасываем состояние
            if (resourceIndicator != null)
                resourceIndicator.SetActive(false);

            hasResource = false;
            currentState = ColonistState.Idle;

            // Пауза
            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator MoveToLocation(Transform target, float timeout)
    {
        if (target == null) yield break;

        float startTime = Time.time;

        while (currentState == ColonistState.MovingToMainBuilding ||
               currentState == ColonistState.MovingToBuilding)
        {
            // Проверяем таймаут
            if (Time.time - startTime > timeout)
            {
                Debug.LogWarning($"{name}: Таймаут движения");
                break;
            }

            // Проверяем достижение
            if (target != null)
            {
                Vector3 targetPos = GetOptimalApproachPoint(target);
                float distance = Vector3.Distance(transform.position, targetPos);

                if (distance <= 1.5f)
                {
                    // Достигли цели
                    if (target == mainBuilding)
                    {
                        currentState = ColonistState.DeliveringResources;
                    }
                    else if (target == targetBuilding)
                    {
                        currentState = ColonistState.Interacting;
                    }
                    break;
                }
            }

            yield return null;
        }
    }

    private bool IsReadyToWork()
    {
        // Проверяем назначенную станцию
        if (targetBuilding == null)
        {
            Debug.Log($"{name}: Нет назначенной станции");
            return false;
        }

        // Проверяем есть ли доступные технологии для исследования
        if (TechnologyManager.Instance == null || !TechnologyManager.Instance.IsAnyTechnologyResearching())
        {
            Debug.Log($"{name}: Нет доступных технологий для исследования");
            return false;
        }

        // Получаем доступную технологию
        var currentResearch = GetAvailableTechnology();
        if (currentResearch == null)
        {
            Debug.Log($"{name}: Нет доступной технологии");
            return false;
        }

        // Проверяем есть ли ресурсы (если не сработал шанс сохранения)
        float saveChance = GetResearchSaveChance();
        if (Random.Range(0f, 100f) >= saveChance) // Если не сохранили ресурсы
        {
            string resourceType = GetResourceTypeForResearch(currentResearch);
            if (ResourceManager.Instance != null)
            {
                int available = ResourceManager.Instance.GetResourceAmount(resourceType);
                if (available < 1)
                {
                    Debug.Log($"{name}: Нет ресурсов {resourceType} (есть: {available})");
                    return false;
                }
            }
        }

        return true;
    }

    private TechnologyManager.Technology GetAvailableTechnology()
    {
        if (TechnologyManager.Instance == null) return null;

        // Ищем первую доступную технологию
        foreach (var tech in TechnologyManager.Instance.allTechnologies)
        {
            if (tech.currentLevel < tech.maxLevel && tech.isUnlocked && tech.HasRequirementsMet())
            {
                return tech;
            }
        }
        return null;
    }

    private string GetResourceTypeForResearch(TechnologyManager.Technology tech)
    {
        // Определяем тип ресурса по ID технологии
        string techId = tech.id.ToLower();

        if (techId.Contains("warmleaf") || techId.Contains("axe") || techId.Contains("lumber") ||
            techId.Contains("auto_lumber") || techId.Contains("captain_axe"))
            return "warmleaf";
        else if (techId.Contains("thunderite") || techId.Contains("drilling") || techId.Contains("picks") ||
                 techId.Contains("pneumatic_picks") || techId.Contains("heavy_drilling"))
            return "thunderite";
        else if (techId.Contains("mirallite") || techId.Contains("enrichment") ||
                 techId.Contains("sample_collection") || techId.Contains("mirallite_enrichment"))
            return "mirallite";
        else
            return "warmleaf"; // По умолчанию
    }

    private void UpdateResourceIndicator()
    {
        if (resourceIndicator == null) return;

        SpriteRenderer renderer = resourceIndicator.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = currentResourceType switch
            {
                "warmleaf" => Color.green,
                "thunderite" => Color.blue,
                "mirallite" => Color.magenta,
                _ => Color.yellow
            };
        }
    }

    private void RegisterAsResearcher()
    {
        if (isRegistered) return;

        // В новой системе это не нужно, но оставим для совместимости
        isRegistered = true;
        Debug.Log($"{name}: Зарегистрирован как исследователь");
    }

    private void UnregisterAsResearcher()
    {
        if (!isRegistered) return;

        isRegistered = false;
        Debug.Log($"{name}: Удален из исследователей");
    }

    // МЕТОД ДЛЯ УВЕЛИЧЕНИЯ ВМЕСТИМОСТИ
    public void IncreaseCarryCapacity(int amount)
    {
        carryAmount += amount;
        Debug.Log($"{name}: Вместимость увеличена до {carryAmount}");
    }

    // МЕТОД ДЛЯ РАСЧЕТА ШАНСА СОХРАНЕНИЯ РЕСУРСОВ
    private float GetResearchSaveChance()
    {
        float chance = baseResearchSaveChance;

        if (TechnologyManager.Instance != null)
        {
            chance += TechnologyManager.Instance.GetResearchSaveChanceBonus();
        }

        return Mathf.Clamp(chance, 0f, 100f);
    }

    protected override void CollectResourcesFromBuilding()
    {
        // Не используем
    }

    protected override bool HasResourcesToDeliver()
    {
        return false;
    }

    void OnDestroy()
    {
        UnregisterAsResearcher();
    }

    // UI метод
    public string GetResearchStatus()
    {
        string status = $"Состояние: {currentState}\n";

        if (hasResource)
        {
            string resourceName = currentResourceType switch
            {
                "warmleaf" => "Теплолист",
                "thunderite" => "Грозалит",
                "mirallite" => "Мираллит",
                _ => currentResourceType
            };

            status += $"Несу: {resourceName}\n";
        }

        status += $"Вместимость: {carryAmount}\n";
        status += $"Шанс сохранения: {GetResearchSaveChance():F1}%\n";

        if (TechnologyManager.Instance != null)
        {
            var tech = GetAvailableTechnology();
            if (tech != null)
            {
                status += $"Исследование: {tech.displayName}\n";
                status += $"Уровень: {tech.currentLevel}/{tech.maxLevel}\n";
                status += $"Стоимость: {tech.GetCostString(tech.currentLevel + 1)}";
            }
            else
            {
                status += "Нет доступного исследования";
            }
        }

        return status;
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        if (hasResource)
        {
            Gizmos.color = currentResourceType switch
            {
                "warmleaf" => Color.green,
                "thunderite" => Color.blue,
                "mirallite" => Color.magenta,
                _ => Color.yellow
            };

            Gizmos.DrawWireSphere(transform.position, 0.3f);

            if (targetBuilding != null)
                Gizmos.DrawLine(transform.position, targetBuilding.position);
        }
        else if (currentState == ColonistState.MovingToMainBuilding)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.25f);

            if (mainBuilding != null)
                Gizmos.DrawLine(transform.position, mainBuilding.position);
        }
        else if (currentState == ColonistState.MovingToBuilding)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.25f);

            if (targetBuilding != null)
                Gizmos.DrawLine(transform.position, targetBuilding.position);
        }
    }
}