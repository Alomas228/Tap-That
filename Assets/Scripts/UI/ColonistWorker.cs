using UnityEngine;
using System.Collections;

public class ColonistWorker : MonoBehaviour
{
    [Header("Базовые настройки")]
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float interactionRange = 0.5f;

    [Header("Ленивое обнаружение препятствий")]
    [SerializeField] protected float stuckCheckInterval = 0.5f;
    [SerializeField] protected float stuckTimeThreshold = 1.5f;
    [SerializeField] protected float moveThreshold = 0.2f;

    [Header("Алгоритм обхода")]
    [SerializeField] protected float avoidanceDistance = 2f;
    [SerializeField] protected float avoidanceDuration = 2f;

    [Header("Визуальные ссылки")]
    [SerializeField] public GameObject visualObject;
    [SerializeField] public SpriteRenderer spriteRenderer;

    [Header("Состояние")]
    protected ColonistState currentState = ColonistState.Idle;
    protected Transform targetBuilding;
    protected Transform mainBuilding;
    protected Vector3 targetPosition;

    // Инвентарь
    protected int carriedWarmleaf = 0;
    protected int carriedThunderite = 0;
    protected int carriedMirallite = 0;

    // Для ленивого обнаружения
    private Vector3 lastPosition;
    private float stuckTimer = 0f;
    private float checkTimer = 0f;
    private bool isAvoiding = false;
    private float avoidanceTimer = 0f;
    private Vector3 avoidanceTarget = Vector3.zero;
    private Vector3 originalTargetDirection = Vector3.zero;

    // Ссылки
    protected BuildingManager buildingManager;
    protected ResourceManager resourceManager;

    public enum ColonistState
    {
        Idle,
        MovingToBuilding,
        Interacting,
        MovingToMainBuilding,
        DeliveringResources,
        ReturningToWork
    }

    void Start()
    {
        buildingManager = BuildingManager.Instance;
        resourceManager = ResourceManager.Instance;

        lastPosition = transform.position;
        FindMainBuilding();
        StartCoroutine(WorkRoutine());
    }

    void OnEnable()
    {
        ResetStuckState();
    }

    protected virtual void Update()
    {
        if (currentState == ColonistState.MovingToBuilding ||
            currentState == ColonistState.MovingToMainBuilding ||
            currentState == ColonistState.ReturningToWork)
        {
            UpdateMovement();
            UpdateStuckDetection();
        }

        UpdateVisuals();
    }



    #region ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ДЛЯ ТОЧЕК

    // Получить середину нижнего края BoxCollider2D здания
    private Vector3 GetBuildingBottomCenter(Transform building)
    {
        BoxCollider2D collider = building.GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            // Получаем границы коллайдера в мировых координатах
            Bounds bounds = collider.bounds;

            // Середина нижнего края коллайдера
            Vector3 bottomCenter = new Vector3(
                bounds.center.x,    // Центр по X
                bounds.min.y,       // Самая нижняя точка по Y
                0
            );

            return bottomCenter;
        }
        else
        {
            // Если нет коллайдера, используем позицию здания минус 0.5 по Y
            Debug.LogWarning($"У здания {building.name} нет BoxCollider2D");
            return building.position + new Vector3(0, -0.5f, 0);
        }
    }

    #endregion

    #region ПОЛНЫЙ ЦИКЛ РАБОТЫ

    protected virtual IEnumerator WorkRoutine()
    {
        while (targetBuilding == null)
        {
            yield return new WaitForSeconds(1f);
        }

        Debug.Log($"{name} начал работу на {targetBuilding.name}");

        while (true)
        {
            // 1. Идем к нижней точке здания
            currentState = ColonistState.MovingToBuilding;
            SetTargetPosition(GetBuildingBottomCenter(targetBuilding));
            yield return StartCoroutine(MoveToTarget());

            // 2. Входим в здание и работаем
            yield return StartCoroutine(InteractWithBuilding());

            // 3. Если есть ресурсы - несем в главное здание
            if (HasResourcesToDeliver())
            {
                yield return StartCoroutine(DeliverResourcesToMainBuilding());

                // 4. Возвращаемся к работе
                currentState = ColonistState.ReturningToWork;
                SetTargetPosition(GetBuildingBottomCenter(targetBuilding));
                yield return StartCoroutine(MoveToTarget());
            }

            // Пауза между циклами
            yield return new WaitForSeconds(0.5f);
        }
    }

    #endregion

    #region ДВИЖЕНИЕ

    protected virtual IEnumerator MoveToTarget()
    {
        // Запоминаем первоначальное направление
        if (currentState == ColonistState.MovingToBuilding ||
            currentState == ColonistState.ReturningToWork)
        {
            originalTargetDirection = (targetPosition - transform.position).normalized;
        }

        while (Vector3.Distance(transform.position, targetPosition) > interactionRange)
        {
            yield return null;
        }

        // Достигли цели
        switch (currentState)
        {
            case ColonistState.MovingToBuilding:
                currentState = ColonistState.Interacting;
                break;
            case ColonistState.MovingToMainBuilding:
                currentState = ColonistState.DeliveringResources;
                break;
            case ColonistState.ReturningToWork:
                currentState = ColonistState.Interacting;
                break;
        }

        ResetStuckState();
    }

    protected virtual void UpdateMovement()
    {
        if (currentState == ColonistState.MovingToBuilding ||
            currentState == ColonistState.MovingToMainBuilding ||
            currentState == ColonistState.ReturningToWork)
        {
            Vector3 direction;

            // Если близко к целевому зданию - идем прямо к нему
            if (currentState == ColonistState.MovingToBuilding ||
                currentState == ColonistState.ReturningToWork)
            {
                float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

                if (distanceToTarget < avoidanceDistance)
                {
                    // Близко к целевому зданию - всегда идем прямо к нему
                    direction = (targetPosition - transform.position).normalized;
                }
                else if (isAvoiding)
                {
                    // В режиме обхода
                    direction = (avoidanceTarget - transform.position).normalized;

                    // Если достигли точки обхода, пробуем вернуться
                    if (Vector3.Distance(transform.position, avoidanceTarget) < 0.3f ||
                        avoidanceTimer < avoidanceDuration * 0.3f)
                    {
                        Vector3 toTarget = (targetPosition - transform.position).normalized;
                        direction = toTarget;
                    }
                }
                else
                {
                    // Обычное движение к цели
                    direction = (targetPosition - transform.position).normalized;
                }
            }
            else if (isAvoiding)
            {
                // Для главного здания - режим обхода
                direction = (avoidanceTarget - transform.position).normalized;

                if (Vector3.Distance(transform.position, avoidanceTarget) < 0.3f ||
                    avoidanceTimer < avoidanceDuration * 0.3f)
                {
                    Vector3 toTarget = (targetPosition - transform.position).normalized;
                    direction = toTarget;
                }
            }
            else
            {
                // Обычное движение
                direction = (targetPosition - transform.position).normalized;
            }

            // Двигаемся в выбранном направлении
            transform.position += direction * moveSpeed * Time.deltaTime;

            // Поворачиваем спрайт
            if (spriteRenderer != null && direction.x != 0)
            {
                spriteRenderer.flipX = direction.x < 0;
            }
        }
    }

    #endregion

    #region ВЗАИМОДЕЙСТВИЕ С ЗДАНИЯМИ

    protected virtual IEnumerator InteractWithBuilding()
    {
        // Скрываем колониста при входе в здание
        if (visualObject != null)
        {
            visualObject.SetActive(false);
        }

        // Ждем время взаимодействия
        float interactionTime = GetInteractionTime();
        Debug.Log($"{name} работает в здании {interactionTime} секунд");
        yield return new WaitForSeconds(interactionTime);

        // Собираем ресурсы (определяется в наследниках)
        CollectResourcesFromBuilding();

        // Показываем колониста при выходе
        if (visualObject != null)
        {
            visualObject.SetActive(true);
        }

        currentState = ColonistState.Idle;
        ResetStuckState();
    }

    protected virtual IEnumerator DeliverResourcesToMainBuilding()
    {
        if (mainBuilding == null)
        {
            Debug.LogError($"{name}: главное здание не найдено!");
            yield break;
        }

        Debug.Log($"{name} несет ресурсы в главное здание");

        // Идем к нижней точке главного здания
        currentState = ColonistState.MovingToMainBuilding;
        SetTargetPosition(GetBuildingBottomCenter(mainBuilding));
        yield return StartCoroutine(MoveToTarget());

        // Сдаем ресурсы
        DeliverResources();

        currentState = ColonistState.Idle;
        ResetStuckState();
    }

    #endregion

    #region ЛЕНИВОЕ ОБНАРУЖЕНИЕ ПРЕПЯТСТВИЙ

    private void UpdateStuckDetection()
    {
        // Если близко к целевому зданию - отключаем обход
        if (currentState == ColonistState.MovingToBuilding ||
            currentState == ColonistState.ReturningToWork)
        {
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
            if (distanceToTarget < avoidanceDistance * 0.5f)
            {
                if (isAvoiding) StopAvoidance();
                stuckTimer = 0f;
                return;
            }
        }

        checkTimer += Time.deltaTime;
        if (checkTimer >= stuckCheckInterval)
        {
            checkTimer = 0f;

            float distanceMoved = Vector3.Distance(transform.position, lastPosition);

            if (distanceMoved < moveThreshold)
            {
                stuckTimer += stuckCheckInterval;

                if (!isAvoiding && stuckTimer >= stuckTimeThreshold)
                {
                    StartAvoidance();
                }
            }
            else
            {
                stuckTimer = Mathf.Max(0f, stuckTimer - stuckCheckInterval * 0.5f);
            }

            lastPosition = transform.position;
        }

        if (isAvoiding)
        {
            avoidanceTimer -= Time.deltaTime;
            if (avoidanceTimer <= 0f) StopAvoidance();
        }
    }

    private void StartAvoidance()
    {
        if (currentState == ColonistState.MovingToBuilding ||
            currentState == ColonistState.ReturningToWork)
        {
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
            if (distanceToTarget < avoidanceDistance) return;
        }

        isAvoiding = true;
        avoidanceTimer = avoidanceDuration;

        if (currentState == ColonistState.MovingToBuilding ||
            currentState == ColonistState.ReturningToWork)
        {
            Vector3 perpendicular = Random.value > 0.5f
                ? Quaternion.Euler(0, 0, 90) * originalTargetDirection
                : Quaternion.Euler(0, 0, -90) * originalTargetDirection;

            avoidanceTarget = transform.position + perpendicular * avoidanceDistance;
        }
        else
        {
            Vector3 toTarget = (targetPosition - transform.position).normalized;
            Vector3 perpendicular = Random.value > 0.5f
                ? Quaternion.Euler(0, 0, 90) * toTarget
                : Quaternion.Euler(0, 0, -90) * toTarget;

            avoidanceTarget = transform.position + perpendicular * avoidanceDistance;
        }
    }



    private void StopAvoidance()
    {
        isAvoiding = false;
        avoidanceTimer = 0f;
        avoidanceTarget = Vector3.zero;
        stuckTimer = 0f;
    }

    private void ResetStuckState()
    {
        stuckTimer = 0f;
        checkTimer = 0f;
        isAvoiding = false;
        avoidanceTimer = 0f;
        avoidanceTarget = Vector3.zero;
        originalTargetDirection = Vector3.zero;
        lastPosition = transform.position;
    }

    #endregion

    #region ВИЗУАЛЫ

    protected virtual void UpdateVisuals()
    {
        if (spriteRenderer == null) return;

        if (isAvoiding)
        {
            // Синий при обходе (не к целевому зданию)
            float alpha = Mathf.PingPong(Time.time * 3f, 0.2f) + 0.8f;
            spriteRenderer.color = new Color(0.7f, 0.7f, 1f, alpha);
        }
        else if (stuckTimer > stuckTimeThreshold * 0.5f &&
                (currentState == ColonistState.MovingToBuilding ||
                 currentState == ColonistState.ReturningToWork))
        {
            // Оранжевый при застревании на пути к целевому зданию
            float t = Mathf.Clamp01(stuckTimer / stuckTimeThreshold);
            spriteRenderer.color = Color.Lerp(Color.white, new Color(1f, 0.5f, 0f), t);
        }
        else if (currentState == ColonistState.MovingToBuilding ||
                currentState == ColonistState.ReturningToWork)
        {
            // Зеленый при движении к целевому зданию
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
            float intensity = Mathf.Clamp01(1f - (distanceToTarget / 10f));
            spriteRenderer.color = Color.Lerp(Color.white, new Color(0.5f, 1f, 0.5f), intensity);
        }
        else
        {
            spriteRenderer.color = Color.white;
        }
    }

    #endregion

    #region ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ

    protected virtual void SetTargetPosition(Vector3 position)
    {
        targetPosition = position;
        ResetStuckState();

        if (currentState == ColonistState.MovingToBuilding ||
            currentState == ColonistState.ReturningToWork)
        {
            originalTargetDirection = (targetPosition - transform.position).normalized;
        }
    }

    protected virtual void FindMainBuilding()
    {
        GameObject mainBuildingObj = GameObject.FindGameObjectWithTag("MainBuilding");
        if (mainBuildingObj != null)
        {
            mainBuilding = mainBuildingObj.transform;
            Debug.Log($"{name} нашел главное здание");
        }
        else
        {
            Debug.LogError($"{name}: главное здание не найдено (тег MainBuilding)");
        }
    }

    protected virtual float GetInteractionTime()
    {
        return 3f; // Базовая длительность
    }

    protected virtual void CollectResourcesFromBuilding()
    {
        // Переопределяется в наследниках
    }

    protected virtual bool HasResourcesToDeliver()
    {
        return carriedWarmleaf > 0 || carriedThunderite > 0 || carriedMirallite > 0;
    }

    protected virtual void DeliverResources()
    {
        if (resourceManager == null || !HasResourcesToDeliver()) return;

        if (carriedWarmleaf > 0)
        {
            resourceManager.AddResource("warmleaf", carriedWarmleaf);
            Debug.Log($"{name} сдал {carriedWarmleaf} теплолиста");
            carriedWarmleaf = 0;
        }

        if (carriedThunderite > 0)
        {
            resourceManager.AddResource("thunderite", carriedThunderite);
            Debug.Log($"{name} сдал {carriedThunderite} грозалита");
            carriedThunderite = 0;
        }

        if (carriedMirallite > 0)
        {
            resourceManager.AddResource("mirallite", carriedMirallite);
            Debug.Log($"{name} сдал {carriedMirallite} мираллита");
            carriedMirallite = 0;
        }
    }

    #endregion

    #region ПУБЛИЧНЫЕ МЕТОДЫ

    public void AssignToBuilding(Transform building)
    {
        if (building == null) return;
        targetBuilding = building;
        ResetStuckState();

        if (building != null)
        {
            originalTargetDirection = (GetBuildingBottomCenter(building) - transform.position).normalized;
        }
    }

    public void UnassignFromBuilding()
    {
        targetBuilding = null;
        currentState = ColonistState.Idle;
        ResetStuckState();
    }

    public bool IsWorking() => currentState != ColonistState.Idle;
    public ColonistState GetCurrentState() => currentState;

    public void AddCarriedResource(string resourceType, int amount)
    {
        switch (resourceType.ToLower())
        {
            case "warmleaf":
                carriedWarmleaf += amount;
                break;
            case "thunderite":
                carriedThunderite += amount;
                break;
            case "mirallite":
                carriedMirallite += amount;
                break;
        }

        Debug.Log($"{name} получил {amount} {resourceType}");
    }

    public int GetCarriedResourceCount(string resourceType)
    {
        return resourceType.ToLower() switch
        {
            "warmleaf" => carriedWarmleaf,
            "thunderite" => carriedThunderite,
            "mirallite" => carriedMirallite,
            _ => 0
        };
    }

    #endregion

    #region GIZMOS ДЛЯ ОТЛАДКИ

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Показываем точку входа для целевого здания
        if (targetBuilding != null &&
            (currentState == ColonistState.MovingToBuilding ||
             currentState == ColonistState.ReturningToWork))
        {
            Vector3 entrancePoint = GetBuildingBottomCenter(targetBuilding);

            // Красный крест для точки входа
            Gizmos.color = Color.red;
            Gizmos.DrawLine(entrancePoint + Vector3.left * 0.3f, entrancePoint + Vector3.right * 0.3f);
            Gizmos.DrawLine(entrancePoint + Vector3.down * 0.3f, entrancePoint + Vector3.up * 0.3f);

            // Также рисуем границы BoxCollider2D
            BoxCollider2D collider = targetBuilding.GetComponent<BoxCollider2D>();
            if (collider != null)
            {
                Gizmos.color = new Color(1, 0, 0, 0.3f);
                Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // Линия к цели
        if (currentState == ColonistState.MovingToBuilding ||
            currentState == ColonistState.MovingToMainBuilding ||
            currentState == ColonistState.ReturningToWork)
        {
            // Цвет в зависимости от состояния
            if (currentState == ColonistState.MovingToBuilding ||
                currentState == ColonistState.ReturningToWork)
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = isAvoiding ? Color.red : Color.yellow;
            }

            Gizmos.DrawLine(transform.position, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, 0.2f);

            // Точка обхода
            if (isAvoiding)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, avoidanceTarget);
                Gizmos.DrawWireSphere(avoidanceTarget, 0.15f);
            }
        }
    }

    #endregion
}