using UnityEngine;
using System.Collections;

public class ResearchStationWorker : ColonistWorker
{
    [Header("Настройки исследования")]
    [SerializeField] private float interactionTime = 3f;
    [SerializeField] private int carryCapacity = 5;

    [Header("Визуальные эффекты")]
    [SerializeField] private GameObject resourceIndicator;
    [SerializeField] private ParticleSystem collectEffect;

    private string currentResourceType = "";
    private int resourcesCarried = 0;
    private bool hasResources = false;
    private bool workCycleRunning = false;

    protected override float GetInteractionTime()
    {
        return interactionTime;
    }

    protected override void Start()
    {
        base.Start();

        if (resourceIndicator != null)
            resourceIndicator.SetActive(false);

        // Ждем немного и запускаем работу
        Invoke(nameof(StartWorkCycle), 1f);
    }

    void StartWorkCycle()
    {
        if (!workCycleRunning)
        {
            workCycleRunning = true;
            StartCoroutine(WorkCycle());
        }
    }

    IEnumerator WorkCycle()
    {
        Debug.Log($"{name}: Начал цикл работы исследователя");

        while (true)
        {
            // 1. ЖДЕМ ПОКА ЕСТЬ СТАНЦИЯ И ИССЛЕДОВАНИЕ ====================
            yield return StartCoroutine(WaitForResearchAndStation());

            // 2. ИДЕМ В ГЛАВНОЕ ЗДАНИЕ =====================================
            Debug.Log($"{name}: Иду в главное здание за ресурсами");

            bool reachedMainBuilding = false;
            currentState = ColonistState.MovingToMainBuilding;

            // Двигаемся к главному зданию
            float mainBuildingTimeout = 30f;
            float mainStartTime = Time.time;

            while (!reachedMainBuilding)
            {
                // Проверяем таймаут
                if (Time.time - mainStartTime > mainBuildingTimeout)
                {
                    Debug.LogWarning($"{name}: Таймаут при движении к главному зданию");
                    break;
                }

                // Проверяем достижение (увеличиваем радиус проверки)
                if (mainBuilding != null)
                {
                    Vector3 targetPos = GetOptimalApproachPoint(mainBuilding);
                    float distance = Vector3.Distance(transform.position, targetPos);

                    // УВЕЛИЧИВАЕМ РАДИУС ДОСТИЖЕНИЯ
                    if (distance <= 1.5f) // было interactionRange, делаем больше
                    {
                        Debug.Log($"{name}: ДОШЁЛ до главного здания! Дистанция: {distance:F2}");
                        reachedMainBuilding = true;
                        currentState = ColonistState.DeliveringResources;
                        break;
                    }
                }

                yield return null;
            }

            if (!reachedMainBuilding)
            {
                Debug.LogWarning($"{name}: Не дошел до главного здания");
                currentState = ColonistState.Idle;
                yield return new WaitForSeconds(2f);
                continue;
            }

            // 3. БЕРЕМ РЕСУРСЫ =============================================
            Debug.Log($"{name}: Беру ресурсы из главного здания");

            bool gotResources = false;

            // Определяем какой ресурс нужен
            var currentResearch = TechnologyManager.Instance.GetResearchingTechnology();
            if (currentResearch != null && currentResearch.requirements.Count > 0)
            {
                currentResourceType = currentResearch.requirements[0].resourceId;

                // Пробуем через ResourceManager
                if (ResourceManager.Instance != null)
                {
                    int available = ResourceManager.Instance.GetResourceAmount(currentResourceType);
                    int takeAmount = Mathf.Min(carryCapacity, available);

                    if (takeAmount > 0 && ResourceManager.Instance.TrySpendResource(currentResourceType, takeAmount))
                    {
                        resourcesCarried = takeAmount;
                        gotResources = true;
                        Debug.Log($"{name}: Взял {resourcesCarried} {currentResourceType} из ResourceManager");
                    }
                }

                // Если не получилось, пробуем через MainBuilding
                if (!gotResources && mainBuilding != null)
                {
                    MainBuilding mainComp = mainBuilding.GetComponent<MainBuilding>();
                    if (mainComp != null)
                    {
                        int available = mainComp.GetResourceAmount(currentResourceType);
                        int takeAmount = Mathf.Min(carryCapacity, available);

                        if (takeAmount > 0 && mainComp.TryTakeResource(currentResourceType, takeAmount))
                        {
                            resourcesCarried = takeAmount;
                            gotResources = true;
                            Debug.Log($"{name}: Взял {resourcesCarried} {currentResourceType} из MainBuilding");
                        }
                    }
                }
            }

            if (!gotResources)
            {
                Debug.Log($"{name}: Не удалось взять ресурсы");
                currentState = ColonistState.Idle;
                yield return new WaitForSeconds(2f);
                continue;
            }

            hasResources = true;

            // Визуальный индикатор
            if (resourceIndicator != null)
            {
                resourceIndicator.SetActive(true);
                // Настраиваем цвет в зависимости от ресурса
                var renderer = resourceIndicator.GetComponent<SpriteRenderer>();
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

            if (collectEffect != null)
                collectEffect.Play();

            // 4. ИДЕМ НА ИССЛЕДОВАТЕЛЬСКУЮ СТАНЦИЮ =========================
            Debug.Log($"{name}: Несу {resourcesCarried} {currentResourceType} на станцию");

            bool reachedStation = false;
            currentState = ColonistState.MovingToBuilding;

            float stationTimeout = 30f;
            float stationStartTime = Time.time;

            while (!reachedStation)
            {
                // Проверяем таймаут
                if (Time.time - stationStartTime > stationTimeout)
                {
                    Debug.LogWarning($"{name}: Таймаут при движении к станции");
                    break;
                }

                // Проверяем достижение
                if (targetBuilding != null)
                {
                    Vector3 targetPos = GetOptimalApproachPoint(targetBuilding);
                    float distance = Vector3.Distance(transform.position, targetPos);

                    // УВЕЛИЧИВАЕМ РАДИУС ДОСТИЖЕНИЯ
                    if (distance <= 1.5f)
                    {
                        Debug.Log($"{name}: ДОШЁЛ до исследовательской станции! Дистанция: {distance:F2}");
                        reachedStation = true;
                        currentState = ColonistState.Interacting;
                        break;
                    }
                }

                yield return null;
            }

            if (!reachedStation)
            {
                Debug.LogWarning($"{name}: Не дошел до станции");
                currentState = ColonistState.Idle;
                yield return new WaitForSeconds(2f);
                continue;
            }

            // 5. ПРОВОДИМ ИССЛЕДОВАНИЕ =====================================
            Debug.Log($"{name}: Начинаю исследование на станции");

            // Скрываем визуал
            if (visualObject != null)
                visualObject.SetActive(false);

            // Ждем время исследования
            yield return new WaitForSeconds(interactionTime);

            // Показываем визуал
            if (visualObject != null)
                visualObject.SetActive(true);

            // Сдаем ресурсы
            if (TechnologyManager.Instance != null)
            {
                bool delivered = TechnologyManager.Instance.DeliverResearchResource(currentResourceType, resourcesCarried);
                if (delivered)
                {
                    Debug.Log($"{name}: УСПЕШНО исследовал {resourcesCarried} {currentResourceType}");
                }
                else
                {
                    Debug.LogWarning($"{name}: Не удалось сдать ресурсы на исследование");
                }
            }

            // Сбрасываем состояние
            if (resourceIndicator != null)
                resourceIndicator.SetActive(false);

            resourcesCarried = 0;
            hasResources = false;
            currentState = ColonistState.Idle;

            // Пауза перед следующим циклом
            Debug.Log($"{name}: Цикл завершен, отдыхаю 1 секунду");
            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator WaitForResearchAndStation()
    {
        while (true)
        {
            // Проверяем назначенную станцию
            if (targetBuilding == null)
            {
                Debug.Log($"{name}: Ожидание назначения станции...");
                yield return new WaitForSeconds(2f);
                continue;
            }

            // Проверяем активное исследование
            if (TechnologyManager.Instance == null || !TechnologyManager.Instance.IsAnyTechnologyResearching())
            {
                Debug.Log($"{name}: Ожидание активного исследования...");
                yield return new WaitForSeconds(2f);
                continue;
            }

            var currentResearch = TechnologyManager.Instance.GetResearchingTechnology();
            if (currentResearch == null || currentResearch.requirements.Count == 0)
            {
                Debug.Log($"{name}: Исследование не имеет требований...");
                yield return new WaitForSeconds(2f);
                continue;
            }

            // Проверяем есть ли ресурсы для исследования
            currentResourceType = currentResearch.requirements[0].resourceId;
            if (ResourceManager.Instance != null)
            {
                int available = ResourceManager.Instance.GetResourceAmount(currentResourceType);
                if (available <= 0)
                {
                    Debug.Log($"{name}: Нет {currentResourceType} для исследования...");
                    yield return new WaitForSeconds(3f);
                    continue;
                }
            }

            // Все условия выполнены
            break;
        }
    }

    protected override void CollectResourcesFromBuilding()
    {
        // Не используем - своя логика выше
    }

    protected override bool HasResourcesToDeliver()
    {
        // Всегда false - исследователь не сдает ресурсы в главное здание
        return false;
    }

    // Метод для UI
    public string GetResearchStatus()
    {
        string status = $"Состояние: {currentState}\n";

        if (hasResources)
        {
            string resourceName = currentResourceType switch
            {
                "warmleaf" => "Теплолист",
                "thunderite" => "Грозалит",
                "mirallite" => "Мираллит",
                _ => currentResourceType
            };

            status += $"Несу: {resourcesCarried} {resourceName}\n";
        }

        if (TechnologyManager.Instance != null && TechnologyManager.Instance.GetResearchingTechnology() != null)
        {
            var tech = TechnologyManager.Instance.GetResearchingTechnology();
            status += $"Исследование: {tech.displayName}\n";
            status += $"Прогресс: {tech.currentProgress:F1}%";
        }

        return status;
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // Цвет в зависимости от того, несет ли ресурсы
        if (hasResources)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            if (targetBuilding != null)
                Gizmos.DrawLine(transform.position, targetBuilding.position);
        }
        else if (currentState == ColonistState.MovingToMainBuilding)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.4f);

            if (mainBuilding != null)
                Gizmos.DrawLine(transform.position, mainBuilding.position);
        }
    }
}