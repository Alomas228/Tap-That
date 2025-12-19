using UnityEngine;
using System.Collections;

public class ResearchStationWorker : ColonistWorker
{
    [Header("Настройки исследования")]
    [SerializeField] private float interactionTime = 3f;
    [SerializeField] private float baseResearchSaveChance = 0f;

    [Header("Визуальные эффекты")]
    [SerializeField] private GameObject resourceIndicator;
    [SerializeField] private ParticleSystem collectEffect;
    [SerializeField] private ParticleSystem researchEffect;

    private string currentResourceType = "";
    private bool hasResource = false;
    private int carryAmount = 1;
    private bool isRegistered = false;

    protected override float GetInteractionTime()
    {
        return interactionTime;
    }

    protected override void Start()
    {
        base.Start();

        if (TechnologyManager.Instance != null)
        {
            int capacityBonus = TechnologyManager.Instance.GetCarryCapacityBonus();
            carryAmount += capacityBonus;
            Debug.Log($"{name}: Начальная вместимость увеличена на {capacityBonus}, всего: {carryAmount}");
        }

        if (resourceIndicator != null)
            resourceIndicator.SetActive(false);

        StartCoroutine(InitializeAndWork());
    }

    IEnumerator InitializeAndWork()
    {
        yield return new WaitForSeconds(1f);
        RegisterAsResearcher();
        StartCoroutine(WorkCycle());
    }

    IEnumerator WorkCycle()
    {
        Debug.Log($"{name}: Начал работу исследователя");

        while (true)
        {
            // 1. ЖДЕМ АКТИВНОГО ИССЛЕДОВАНИЯ
            while (!IsReadyToWork())
            {
                yield return new WaitForSeconds(2f);
            }

            // 2. ИДЕМ В ГЛАВНОЕ ЗДАНИЕ
            currentState = ColonistState.MovingToMainBuilding;

            yield return StartCoroutine(MoveToLocation(mainBuilding, 30f));

            if (currentState != ColonistState.DeliveringResources)
            {
                yield return new WaitForSeconds(2f);
                continue;
            }

            // 3. БЕРЕМ РЕСУРСЫ
            bool gotResource = false;
            int takeAmount = 0;

            // ИСПРАВЛЕНО: Используем правильный метод
            var currentResearch = TechnologyManager.Instance != null ? TechnologyManager.Instance.GetCurrentResearch() : null;
            if (currentResearch != null)
            {
                currentResourceType = currentResearch.GetPrimaryResourceType();

                if (ResourceManager.Instance != null)
                {
                    int available = ResourceManager.Instance.GetResourceAmount(currentResourceType);
                    takeAmount = Mathf.Min(carryAmount, available);

                    if (takeAmount > 0)
                    {
                        // Проверяем шанс сохранения ресурсов
                        float saveChance = GetResearchSaveChance();
                        bool savedResources = Random.Range(0f, 100f) < saveChance;

                        if (savedResources)
                        {
                            Debug.Log($"{name}: Удалось сохранить ресурсы благодаря технологии!");
                            gotResource = true;
                            // Не тратим ресурсы, но все равно "берем" их
                        }
                        else if (ResourceManager.Instance.TrySpendResource(currentResourceType, takeAmount))
                        {
                            gotResource = true;
                            Debug.Log($"{name}: Взял {takeAmount} {currentResourceType}");
                        }

                        if (gotResource)
                        {
                            hasResource = true;
                            AddCarriedResource(currentResourceType, takeAmount);

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
            }

            if (!gotResource)
            {
                Debug.Log($"{name}: Не удалось взять ресурсы");
                currentState = ColonistState.Idle;
                yield return new WaitForSeconds(2f);
                continue;
            }

            // 4. ИДЕМ НА ИССЛЕДОВАТЕЛЬСКУЮ СТАНЦИЮ
            currentState = ColonistState.MovingToBuilding;

            yield return StartCoroutine(MoveToLocation(targetBuilding, 30f));

            if (currentState != ColonistState.Interacting)
            {
                currentState = ColonistState.Idle;
                yield return new WaitForSeconds(2f);
                continue;
            }

            // 5. ПРОВОДИМ ИССЛЕДОВАНИЕ
            if (visualObject != null)
                visualObject.SetActive(false);

            yield return new WaitForSeconds(interactionTime);

            if (visualObject != null)
                visualObject.SetActive(true);

            // СДАЕМ РЕСУРСЫ НА ИССЛЕДОВАНИЕ
            int deliveredAmount = GetCarriedResourceCount(currentResourceType);

            if (deliveredAmount > 0 && TechnologyManager.Instance != null)
            {
                bool researchProgressed = TechnologyManager.Instance.AddResearchProgress(currentResourceType, deliveredAmount);

                if (researchProgressed)
                {
                    Debug.Log($"{name}: Передал {deliveredAmount} {currentResourceType} на исследование");

                    // Обнуляем количество ресурса
                    switch (currentResourceType)
                    {
                        case "warmleaf":
                            carriedWarmleaf = 0;
                            break;
                        case "thunderite":
                            carriedThunderite = 0;
                            break;
                        case "mirallite":
                            carriedMirallite = 0;
                            break;
                    }

                    if (researchEffect != null)
                        researchEffect.Play();
                }
                else
                {
                    Debug.Log($"{name}: Добавил {deliveredAmount} {currentResourceType} к исследованию");
                }
            }

            // Сбрасываем состояние
            if (resourceIndicator != null)
                resourceIndicator.SetActive(false);

            hasResource = false;
            currentResourceType = "";
            currentState = ColonistState.Idle;

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
            if (Time.time - startTime > timeout)
            {
                Debug.LogWarning($"{name}: Таймаут движения");
                break;
            }

            if (target != null)
            {
                Vector3 targetPos = GetOptimalApproachPoint(target);
                float distance = Vector3.Distance(transform.position, targetPos);

                if (distance <= 1.5f)
                {
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
        if (targetBuilding == null)
        {
            return false;
        }

        if (TechnologyManager.Instance == null)
        {
            return false;
        }

        // Проверяем есть ли активные исследования
        if (!TechnologyManager.Instance.IsAnyTechnologyResearching())
        {
            return false;
        }

        // ИСПРАВЛЕНО: Используем правильный метод
        var currentResearch = TechnologyManager.Instance.GetCurrentResearch();
        if (currentResearch == null)
        {
            return false;
        }

        // Проверяем не завершено ли исследование
        if (!currentResearch.IsReadyForResearch())
        {
            return false;
        }

        // Проверяем есть ли ресурсы для исследования
        string resourceType = currentResearch.GetPrimaryResourceType();
        if (ResourceManager.Instance != null)
        {
            int available = ResourceManager.Instance.GetResourceAmount(resourceType);
            if (available < 1)
            {
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
        if (isRegistered) return;
        isRegistered = true;
    }

    private void UnregisterAsResearcher()
    {
        if (!isRegistered) return;
        isRegistered = false;
    }

    public void IncreaseCarryCapacity(int amount)
    {
        carryAmount += amount;
    }

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
        return hasResource;
    }

    protected override void DeliverResources()
    {
        // Переопределяем доставку - теперь это делается в WorkCycle
    }

    void OnDestroy()
    {
        UnregisterAsResearcher();
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