using UnityEngine;
using System.Collections;

public class ResearchStationWorker : ColonistWorker
{
    [Header("Настройки исследования")]
    [SerializeField] private float interactionTime = 3f;

    [Header("Визуальные эффекты")]
    [SerializeField] private GameObject resourceIndicator;
    [SerializeField] private ParticleSystem collectEffect;
    [SerializeField] private ParticleSystem researchEffect;

    private string currentResourceType = "";
    private bool hasResource = false;
    private bool workCycleRunning = false;
    private int carryAmount = 1;
    private bool isRegistered = false;

    protected override float GetInteractionTime()
    {
        return interactionTime;
    }

    protected override void Start()
    {
        base.Start();

        if (resourceIndicator != null)
            resourceIndicator.SetActive(false);

        // Запускаем работу
        StartCoroutine(InitializeAndWork());
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
            // 1. ЖДЕМ АКТИВНОГО ИССЛЕДОВАНИЯ И РЕСУРСОВ ====================
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

            // Получаем текущее исследование
            var currentResearch = TechnologyManager.Instance.GetResearchingTechnology();
            if (currentResearch != null && currentResearch.requirements.Count > 0)
            {
                currentResourceType = currentResearch.requirements[0].resourceId;

                // Берем ресурсы
                if (ResourceManager.Instance != null)
                {
                    int available = ResourceManager.Instance.GetResourceAmount(currentResourceType);
                    takeAmount = Mathf.Min(carryAmount, available);

                    if (takeAmount > 0 && ResourceManager.Instance.TrySpendResource(currentResourceType, takeAmount))
                    {
                        gotResource = true;
                        Debug.Log($"{name}: Взял {takeAmount} {currentResourceType}");

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

            // Сдаем ресурсы на исследование
            if (TechnologyManager.Instance != null)
            {
                bool delivered = TechnologyManager.Instance.DeliverResearchResource(currentResourceType, takeAmount);
                if (delivered)
                {
                    Debug.Log($"{name}: Исследовал {takeAmount} {currentResourceType}");

                    if (researchEffect != null)
                        researchEffect.Play();
                }
                else
                {
                    Debug.LogWarning($"{name}: Не удалось сдать ресурсы");
                }
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

        // Проверяем активное исследование
        if (TechnologyManager.Instance == null || !TechnologyManager.Instance.IsAnyTechnologyResearching())
        {
            Debug.Log($"{name}: Нет активного исследования");
            return false;
        }

        var currentResearch = TechnologyManager.Instance.GetResearchingTechnology();
        if (currentResearch == null || currentResearch.requirements.Count == 0)
        {
            Debug.Log($"{name}: Исследование без требований");
            return false;
        }

        // Проверяем есть ли ресурсы
        string resourceType = currentResearch.requirements[0].resourceId;
        if (ResourceManager.Instance != null)
        {
            int available = ResourceManager.Instance.GetResourceAmount(resourceType);
            if (available < 1)
            {
                Debug.Log($"{name}: Нет ресурсов {resourceType} (есть: {available})");
                return false;
            }
        }

        return true;
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
        if (isRegistered || TechnologyManager.Instance == null) return;

        TechnologyManager.Instance.RegisterResearcher();
        isRegistered = true;
        Debug.Log($"{name}: Зарегистрирован как исследователь");
    }

    private void UnregisterAsResearcher()
    {
        if (!isRegistered || TechnologyManager.Instance == null) return;

        TechnologyManager.Instance.UnregisterResearcher();
        isRegistered = false;
        Debug.Log($"{name}: Удален из исследователей");
    }

    // МЕТОД ДЛЯ УВЕЛИЧЕНИЯ ВМЕСТИМОСТИ
    public void IncreaseCarryCapacity(int amount)
    {
        carryAmount += amount;
        Debug.Log($"{name}: Вместимость увеличена до {carryAmount}");
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

        if (TechnologyManager.Instance != null)
        {
            var tech = TechnologyManager.Instance.GetResearchingTechnology();
            if (tech != null)
            {
                status += $"Исследование: {tech.displayName}\n";
                status += $"Уровень: {tech.currentLevel}/{tech.maxLevel}\n";
                status += $"Ресурсы: {tech.collectedResources}/{tech.resourcesPerLevel}\n";
                status += $"Прогресс: {tech.currentProgress:F1}%";
            }
            else
            {
                status += "Нет активного исследования";
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