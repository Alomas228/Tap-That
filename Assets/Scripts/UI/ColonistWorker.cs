using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ColonistWorker : MonoBehaviour
{
    [Header("Базовые настройки")]
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float interactionRange = 0.5f;
    [SerializeField] protected float rotationSpeed = 10f;

    [Header("Система обнаружения застревания")]
    [SerializeField] protected float stuckCheckInterval = 0.5f;
    [SerializeField] protected float stuckTimeThreshold = 2f;
    [SerializeField] protected float stuckMoveThreshold = 0.2f;
    [SerializeField] protected float stuckResetDistance = 2f;

    [Header("Интеллектуальный обход зданий")]
    [SerializeField] protected float buildingAvoidanceRadius = 3f;
    [SerializeField] protected float obstacleDetectionRadius = 1.5f;
    [SerializeField] protected LayerMask buildingLayer;
    [SerializeField] protected LayerMask obstacleLayer;

    [Header("Алгоритм A* (упрощенный)")]
    [SerializeField] protected int maxSearchDepth = 50;
    [SerializeField] protected float nodeSize = 0.5f;
    [SerializeField] protected bool usePathSmoothing = true;
    [SerializeField] protected float pathUpdateInterval = 0.5f;

    [Header("Визуальные ссылки")]
    [SerializeField] public GameObject visualObject;
    [SerializeField] public SpriteRenderer spriteRenderer;
    [SerializeField] protected Color pathDebugColor = Color.cyan;

    [Header("Состояние")]
    protected ColonistState currentState = ColonistState.Idle;
    protected Transform targetBuilding;
    protected Transform mainBuilding;
    protected Vector3 targetPosition;
    protected Vector3 currentVelocity;
    protected bool isPathfinding = false;

    // Инвентарь
    protected int carriedWarmleaf = 0;
    protected int carriedThunderite = 0;
    protected int carriedMirallite = 0;

    // Система пути
    private List<Vector3> currentPath = new List<Vector3>();
    private int currentPathIndex = 0;
    private float pathUpdateTimer = 0f;
    private Vector3 lastTargetPosition = Vector3.zero;

    // Временные точки обхода
    private Vector3 avoidancePoint = Vector3.zero;
    private bool isAvoiding = false;
    private float avoidanceTimer = 0f;

    // Система обнаружения застревания
    private Vector3[] stuckCheckPositions = new Vector3[3];
    private int stuckCheckIndex = 0;
    private float stuckTimer = 0f;
    private float stuckCheckTimer = 0f;
    private bool isStuck = false;
    private Vector3 stuckStartPosition;
    private float stuckStartTime;
    private int stuckRecoveryAttempts = 0;
    private const int maxStuckRecoveryAttempts = 3;

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

    protected virtual void Start()
    {
        buildingManager = BuildingManager.Instance;
        resourceManager = ResourceManager.Instance;

        // Инициализируем слои
        buildingLayer = LayerMask.GetMask("Buildings", "Default");
        obstacleLayer = LayerMask.GetMask("Obstacles", "Buildings", "Default");

        FindMainBuilding();
        StartCoroutine(WorkRoutine());

        // Инициализируем систему обнаружения застревания
        stuckStartPosition = transform.position;
        stuckStartTime = Time.time;
        for (int i = 0; i < stuckCheckPositions.Length; i++)
        {
            stuckCheckPositions[i] = transform.position;
        }
    }

    protected virtual void Update()
    {
        if (currentState == ColonistState.MovingToBuilding ||
            currentState == ColonistState.MovingToMainBuilding ||
            currentState == ColonistState.ReturningToWork)
        {
            UpdateIntelligentMovement();
            UpdateStuckDetection(); // Добавляем проверку застревания
            UpdateVisuals();
        }
    }

    #region СИСТЕМА ОБНАРУЖЕНИЯ ЗАСТРЕВАНИЯ

    private void UpdateStuckDetection()
    {
        stuckCheckTimer += Time.deltaTime;

        if (stuckCheckTimer >= stuckCheckInterval)
        {
            stuckCheckTimer = 0f;

            // Сохраняем текущую позицию для истории
            stuckCheckPositions[stuckCheckIndex] = transform.position;
            stuckCheckIndex = (stuckCheckIndex + 1) % stuckCheckPositions.Length;

            // Проверяем движение за последние N позиций
            float totalMovement = 0f;
            for (int i = 0; i < stuckCheckPositions.Length - 1; i++)
            {
                int currentIndex = (stuckCheckIndex + i) % stuckCheckPositions.Length;
                int nextIndex = (stuckCheckIndex + i + 1) % stuckCheckPositions.Length;
                totalMovement += Vector3.Distance(stuckCheckPositions[currentIndex], stuckCheckPositions[nextIndex]);
            }

            // Если движение меньше порога - считаем что застрял
            if (totalMovement < stuckMoveThreshold)
            {
                stuckTimer += stuckCheckInterval;

                if (!isStuck && stuckTimer >= stuckTimeThreshold)
                {
                    OnStuckDetected();
                }
            }
            else
            {
                // Движется - сбрасываем таймер
                stuckTimer = Mathf.Max(0, stuckTimer - stuckCheckInterval * 0.5f);

                // Если ушли достаточно далеко от места застревания - сбрасываем состояние
                if (isStuck && Vector3.Distance(transform.position, stuckStartPosition) > stuckResetDistance)
                {
                    OnStuckRecovered();
                }
            }
        }

        // Если застрял - применяем специальную логику
        if (isStuck)
        {
            ExecuteStuckRecovery();
        }
    }

    private void OnStuckDetected()
    {
        isStuck = true;
        stuckStartPosition = transform.position;
        stuckStartTime = Time.time;
        stuckRecoveryAttempts = 0;

        

        // Прерываем текущий путь
        currentPath.Clear();
        currentPathIndex = 0;
        isAvoiding = false;

        // Уведомление для UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowNotification($"{name} застрял! Пытаюсь выйти...", 2f);
        }
    }

    private void OnStuckRecovered()
    {
        isStuck = false;
        stuckTimer = 0f;
        stuckRecoveryAttempts = 0;

        

        // Пересчитываем путь к цели
        if (targetBuilding != null)
        {
            Vector3 desiredTarget = GetOptimalApproachPoint(targetBuilding);
            CalculatePathToTarget(desiredTarget);
        }
    }

    private void ExecuteStuckRecovery()
    {
        // Проверяем не слишком ли долго застряли
        if (Time.time - stuckStartTime > stuckTimeThreshold * 3f)
        {
            stuckRecoveryAttempts++;

            if (stuckRecoveryAttempts >= maxStuckRecoveryAttempts)
            {
                // Критическое застревание - радикальные меры
                ExecuteCriticalStuckRecovery();
                return;
            }
        }

        // Определяем в какую сторону пытаться выйти
        Vector3 recoveryDirection = CalculateStuckRecoveryDirection();

        // Двигаемся в направлении выхода
        transform.position += recoveryDirection * moveSpeed * Time.deltaTime * 0.7f;

        // Обновляем визуалы для отладки
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.Lerp(Color.red, Color.yellow, Mathf.PingPong(Time.time * 2f, 1f));
            spriteRenderer.flipX = recoveryDirection.x < 0;
        }

        // Проверяем свободно ли впереди
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            recoveryDirection,
            obstacleDetectionRadius,
            obstacleLayer | buildingLayer
        );

        if (hit.collider != null &&
            hit.collider.gameObject != gameObject &&
            hit.collider.transform != targetBuilding &&
            hit.collider.transform != mainBuilding)
        {
            // Есть препятствие - пробуем другое направление
            recoveryDirection = Quaternion.Euler(0, 0, 90) * recoveryDirection;
        }
    }

    private Vector3 CalculateStuckRecoveryDirection()
    {
        // 1. Пробуем движение перпендикулярно к цели
        if (targetBuilding != null)
        {
            Vector3 toTarget = (GetOptimalApproachPoint(targetBuilding) - transform.position).normalized;
            Vector3 perpendicular = new Vector3(-toTarget.y, toTarget.x, 0);

            // Выбираем направление с лучшей проходимостью
            float rightClearance = CheckDirectionClearance(perpendicular);
            float leftClearance = CheckDirectionClearance(-perpendicular);

            return (rightClearance >= leftClearance) ? perpendicular : -perpendicular;
        }

        // 2. Пробуем случайные направления
        float angle = Random.Range(0f, 360f);
        return Quaternion.Euler(0, 0, angle) * Vector3.right;
    }

    private float CheckDirectionClearance(Vector3 direction)
    {
        float maxDistance = obstacleDetectionRadius * 1.5f;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, maxDistance, obstacleLayer | buildingLayer);

        if (hit.collider != null &&
            hit.collider.gameObject != gameObject &&
            hit.collider.transform != targetBuilding &&
            hit.collider.transform != mainBuilding)
        {
            return hit.distance;
        }

        return maxDistance;
    }

    private void ExecuteCriticalStuckRecovery()
    {
       

        // 1. Телепортация на небольшое расстояние
        Vector3 teleportDirection = CalculateStuckRecoveryDirection();
        Vector3 teleportPosition = transform.position + teleportDirection * 1.5f;

        // Проверяем можно ли телепортироваться
        Collider2D[] colliders = Physics2D.OverlapCircleAll(teleportPosition, 0.5f, obstacleLayer | buildingLayer);
        bool canTeleport = true;

        foreach (var collider in colliders)
        {
            if (collider.gameObject != gameObject &&
                collider.transform != targetBuilding &&
                collider.transform != mainBuilding)
            {
                canTeleport = false;
                break;
            }
        }

        if (canTeleport)
        {
            transform.position = teleportPosition;
            Debug.Log($"{name} телепортирован в {transform.position}");
        }
        else
        {
            // 2. Пробуем другое направление
            teleportDirection = Quaternion.Euler(0, 0, 180) * teleportDirection;
            teleportPosition = transform.position + teleportDirection * 1.5f;

            transform.position = teleportPosition;
            Debug.Log($"{name} телепортирован в обратном направлении в {transform.position}");
        }

        // Сбрасываем состояние застревания
        OnStuckRecovered();
    }

    #endregion

    #region ИСПРАВЛЕННАЯ СИСТЕМА ДВИЖЕНИЯ (с цикличностью)

    protected virtual void UpdateIntelligentMovement()
    {
        if (targetBuilding == null || currentState == ColonistState.Idle || isStuck) return;

        // Определяем целевую позицию
        Vector3 desiredTarget = GetOptimalApproachPoint(targetBuilding);

        if (currentState == ColonistState.MovingToMainBuilding && mainBuilding != null)
        {
            desiredTarget = GetOptimalApproachPoint(mainBuilding);
        }

        // Если цель изменилась или нет пути - пересчитываем путь
        pathUpdateTimer += Time.deltaTime;
        if (pathUpdateTimer >= pathUpdateInterval ||
            Vector3.Distance(desiredTarget, lastTargetPosition) > 0.5f ||
            currentPath.Count == 0)
        {
            CalculatePathToTarget(desiredTarget);
            pathUpdateTimer = 0f;
            lastTargetPosition = desiredTarget;
        }

        // Если есть путь - следуем по нему
        if (currentPath.Count > 0 && currentPathIndex < currentPath.Count)
        {
            FollowPath();
        }
        else
        {
            // Резервный алгоритм прямого движения с обходом
            MoveWithSimpleAvoidance(desiredTarget);
        }

        // Проверяем достижение цели
        float distanceToTarget = Vector3.Distance(transform.position, desiredTarget);
        if (distanceToTarget <= interactionRange)
        {
            OnReachedTarget();
        }
    }

    #region ПОЛНЫЙ ЦИКЛ РАБОТЫ (исправленный)

    protected virtual IEnumerator WorkRoutine()
    {
        // Ждем пока назначим здание
        while (targetBuilding == null)
        {
            yield return new WaitForSeconds(0.5f);
        }

        // ОДИН РАЗ при старте работы
        Debug.Log($"{name} начал работу на {targetBuilding.name}");

        // БЕСКОНЕЧНЫЙ ЦИКЛ РАБОТЫ
        while (true)
        {
            // 1. Идем к зданию
            currentState = ColonistState.MovingToBuilding;

            // Ждем пока дойдет до здания ИЛИ застрянет на слишком долго
            yield return StartCoroutine(MoveToTargetWithTimeout(targetBuilding, 30f));

            // Проверяем не застряли ли мы
            if (isStuck)
            {
                yield return new WaitUntil(() => !isStuck);
                continue; // Начинаем цикл заново
            }

            // Проверяем что действительно дошли
            if (currentState != ColonistState.Interacting)
            {
                continue;
            }

            // 2. Взаимодействуем со зданием (без лишнего логирования)
            yield return StartCoroutine(InteractWithBuilding());

            // Проверяем не застряли ли во взаимодействии
            if (currentState != ColonistState.Idle)
            {
                currentState = ColonistState.Idle;
            }

            // 3. Если есть ресурсы - несем в главное здание
            if (HasResourcesToDeliver())
            {
                currentState = ColonistState.MovingToMainBuilding;

                // Ждем пока дойдет до главного здания
                yield return StartCoroutine(MoveToTargetWithTimeout(mainBuilding, 30f));

                // Проверяем не застряли ли
                if (isStuck)
                {
                    yield return new WaitUntil(() => !isStuck);
                    continue;
                }

                // Проверяем что дошли
                if (currentState != ColonistState.DeliveringResources)
                {
                    continue;
                }

                // Сдаем ресурсы
                DeliverResources();

                // 4. Возвращаемся к работе
                currentState = ColonistState.ReturningToWork;

                // Ждем возвращения
                yield return StartCoroutine(MoveToTargetWithTimeout(targetBuilding, 30f));

                if (isStuck)
                {
                    yield return new WaitUntil(() => !isStuck);
                }
            }

            // Короткая пауза между циклами
            yield return new WaitForSeconds(0.5f);

            // Сбрасываем состояние застревания на случай если были небольшие проблемы
            if (!isStuck && stuckTimer > 0)
            {
                stuckTimer = Mathf.Max(0, stuckTimer - 1f);
            }
        }
    }

    protected virtual void OnDestroy()
    {
        // Базовая реализация (может быть пустой)
    }

    private IEnumerator MoveToTargetWithTimeout(Transform building, float timeout)
    {
        float startTime = Time.time;
        Vector3 initialPosition = transform.position;

        while (currentState == ColonistState.MovingToBuilding ||
               currentState == ColonistState.MovingToMainBuilding ||
               currentState == ColonistState.ReturningToWork)
        {
            // Проверяем таймаут
            if (Time.time - startTime > timeout)
            {
                Debug.LogWarning($"{name} таймаут движения к {building.name}. Перезапускаю...");

                // Сбрасываем путь и пробуем заново
                currentPath.Clear();
                currentPathIndex = 0;
                pathUpdateTimer = pathUpdateInterval;

                // Проверяем не на месте ли мы уже
                float distance = Vector3.Distance(transform.position, GetOptimalApproachPoint(building));
                if (distance <= interactionRange * 1.5f)
                {
                    // Мы близко - считаем что дошли
                    break;
                }

                // Проверяем движение
                if (Vector3.Distance(transform.position, initialPosition) < stuckMoveThreshold * 3f)
                {
                    // Не сдвинулись с места - активируем застревание
                    OnStuckDetected();
                }

                startTime = Time.time; // Сброс таймера
            }

            yield return null;
        }
    }

    #endregion

    public void OnReachedTarget()
    {
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

        // Сбрасываем путь
        currentPath.Clear();
        currentPathIndex = 0;
        isAvoiding = false;

        // Сбрасываем застревание при успешном достижении цели
        if (isStuck)
        {
            OnStuckRecovered();
        }
    }

    #endregion

    #region ОСТАЛЬНЫЕ МЕТОДЫ (оптимизированные)

    private void CalculatePathToTarget(Vector3 target)
    {
        currentPath.Clear();
        currentPathIndex = 0;

        Vector3 start = transform.position;

        // Прямая линия к цели
        if (IsPathClear(start, target))
        {
            currentPath.Add(target);
            return;
        }

        // Ищем обходной путь
        List<Vector3> path = FindAlternativePath(start, target);

        if (path.Count > 0)
        {
            currentPath = path;

            if (usePathSmoothing && currentPath.Count > 2)
            {
                SmoothPath();
            }
        }
    }

    private List<Vector3> FindAlternativePath(Vector3 start, Vector3 target)
    {
        List<Vector3> path = new List<Vector3>();
        Vector3 mainObstacle = FindMainObstacle(start, target);

        if (mainObstacle != Vector3.zero)
        {
            Vector3 avoidanceDirection = CalculateOptimalAvoidance(start, target, mainObstacle);
            Vector3 intermediatePoint = start + avoidanceDirection * buildingAvoidanceRadius;

            if (IsPathClear(start, intermediatePoint) && IsPathClear(intermediatePoint, target))
            {
                path.Add(intermediatePoint);
                path.Add(target);
            }
        }

        if (path.Count == 0)
        {
            path.Add(CalculateSimpleAvoidancePoint(start, target));
            path.Add(target);
        }

        return path;
    }

    private bool IsPathClear(Vector3 from, Vector3 to)
    {
        Vector3 direction = (to - from).normalized;
        float distance = Vector3.Distance(from, to);

        RaycastHit2D[] hits = Physics2D.RaycastAll(from, direction, distance, buildingLayer | obstacleLayer);

        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.gameObject != gameObject &&
                hit.collider.transform != targetBuilding && hit.collider.transform != mainBuilding)
            {
                return false;
            }
        }

        return true;
    }

    private void FollowPath()
    {
        if (currentPathIndex >= currentPath.Count) return;

        Vector3 currentWaypoint = currentPath[currentPathIndex];
        Vector3 direction = (currentWaypoint - transform.position).normalized;

        // Двигаемся к точке
        transform.position += direction * moveSpeed * Time.deltaTime;

        // Поворачиваем спрайт
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction.x < 0;
        }

        // Проверяем достижение точки
        if (Vector3.Distance(transform.position, currentWaypoint) <= nodeSize)
        {
            currentPathIndex++;
        }
    }

    private void MoveWithSimpleAvoidance(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, target);

        RaycastHit2D hit = Physics2D.CircleCast(
            transform.position, 0.3f, direction,
            Mathf.Min(obstacleDetectionRadius, distanceToTarget),
            obstacleLayer | buildingLayer
        );

        if (hit.collider != null && hit.collider.gameObject != gameObject &&
            hit.collider.transform != targetBuilding && hit.collider.transform != mainBuilding)
        {
            Vector3 avoidanceDir = CalculateAvoidanceDirection(direction, hit.normal);
            transform.position += avoidanceDir * moveSpeed * Time.deltaTime;
            isAvoiding = true;
            avoidanceTimer = 0.5f;
        }
        else
        {
            if (!isAvoiding || avoidanceTimer <= 0f)
            {
                transform.position += direction * moveSpeed * Time.deltaTime;
            }
            else
            {
                avoidanceTimer -= Time.deltaTime;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
    }

    protected Vector3 GetOptimalApproachPoint(Transform building)
    {
        if (building == null) return transform.position + Vector3.right;

        BoxCollider2D collider = building.GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            Bounds bounds = collider.bounds;

            Vector3[] approachPoints = new Vector3[]
            {
                new Vector3(bounds.min.x + 0.3f, bounds.min.y, 0),
                new Vector3(bounds.max.x - 0.3f, bounds.min.y, 0),
                new Vector3(bounds.center.x, bounds.min.y, 0)
            };

            Vector3 bestPoint = approachPoints[2];
            float bestScore = float.MaxValue;

            foreach (Vector3 point in approachPoints)
            {
                float distance = Vector3.Distance(transform.position, point);
                float clearance = GetPointClearance(point);
                float score = distance * 0.7f + (10f - clearance) * 0.3f;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestPoint = point;
                }
            }

            // Смещение от здания
            Vector3 toColonist = (transform.position - bestPoint).normalized;
            return bestPoint + toColonist * 0.3f;
        }

        return building.position + new Vector3(0, -1f, 0);
    }

    #endregion

    #region ПУБЛИЧНЫЕ МЕТОДЫ (с добавлением AddCarriedResource)

    public void AssignToBuilding(Transform building)
    {
        if (building == null) return;

        // Если уже назначен на это здание - ничего не делаем
        if (targetBuilding == building && currentState != ColonistState.Idle)
        {
            return;
        }

        targetBuilding = building;
        currentState = ColonistState.MovingToBuilding;

        // Сбрасываем все состояния
        currentPath.Clear();
        currentPathIndex = 0;
        pathUpdateTimer = pathUpdateInterval;
        isStuck = false;
        stuckTimer = 0f;

        Debug.Log($"{name} назначен на здание {building.name}");
    }

    public void UnassignFromBuilding()
    {
        if (targetBuilding == null) return;

        Debug.Log($"{name} снят с здания {targetBuilding.name}");
        targetBuilding = null;
        currentState = ColonistState.Idle;
        currentPath.Clear();
    }

    // ДОБАВЛЯЕМ ОБРАТНО МЕТОД AddCarriedResource для совместимости с дочерними классами
    public void AddCarriedResource(string resourceType, int amount)
    {
        switch (resourceType.ToLower())
        {
            case "warmleaf":
                carriedWarmleaf += amount;
                Debug.Log($"{name} получил {amount} теплолиста. Всего: {carriedWarmleaf}");
                break;
            case "thunderite":
                carriedThunderite += amount;
                Debug.Log($"{name} получил {amount} грозалита. Всего: {carriedThunderite}");
                break;
            case "mirallite":
                carriedMirallite += amount;
                Debug.Log($"{name} получил {amount} мираллита. Всего: {carriedMirallite}");
                break;
            default:
                Debug.LogWarning($"{name}: неизвестный тип ресурса '{resourceType}'");
                break;
        }
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

    public bool IsWorking() => currentState != ColonistState.Idle;
    public ColonistState GetCurrentState() => currentState;

    #endregion

    #region ВИЗУАЛИЗАЦИЯ И ОТЛАДКА

    protected virtual void UpdateVisuals()
    {
        if (spriteRenderer == null) return;

        if (isStuck)
        {
            // Мигающий красный при застревании
            float pulse = Mathf.PingPong(Time.time * 3f, 1f);
            spriteRenderer.color = Color.Lerp(Color.red, Color.yellow, pulse);
        }
        else if (isAvoiding)
        {
            // Желтый при обходе
            spriteRenderer.color = Color.Lerp(Color.white, Color.yellow, 0.5f);
        }
        else if (currentPath.Count > 0)
        {
            // Голубой при следовании по пути
            spriteRenderer.color = Color.Lerp(Color.white, Color.cyan, 0.3f);
        }
        else
        {
            spriteRenderer.color = Color.white;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // Путь
        Gizmos.color = pathDebugColor;
        for (int i = 0; i < currentPath.Count; i++)
        {
            Gizmos.DrawSphere(currentPath[i], 0.1f);
            if (i < currentPath.Count - 1)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            }
        }

        // Зона застревания
        if (isStuck)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(stuckStartPosition, 0.5f);
            Gizmos.DrawLine(transform.position, stuckStartPosition);
        }

        // История позиций для обнаружения застревания
        Gizmos.color = new Color(1, 0.5f, 0, 0.5f);
        for (int i = 0; i < stuckCheckPositions.Length - 1; i++)
        {
            if (stuckCheckPositions[i] != Vector3.zero && stuckCheckPositions[i + 1] != Vector3.zero)
            {
                Gizmos.DrawLine(stuckCheckPositions[i], stuckCheckPositions[i + 1]);
                Gizmos.DrawSphere(stuckCheckPositions[i], 0.05f);
            }
        }
    }

    #endregion

    #region БАЗОВЫЕ МЕТОДЫ

    protected virtual void FindMainBuilding()
    {
        GameObject mainBuildingObj = GameObject.FindGameObjectWithTag("MainBuilding");
        if (mainBuildingObj != null)
        {
            mainBuilding = mainBuildingObj.transform;
        }
    }

    protected virtual IEnumerator InteractWithBuilding()
    {
        if (visualObject != null)
        {
            visualObject.SetActive(false);
        }

        float interactionTime = GetInteractionTime();
        yield return new WaitForSeconds(interactionTime);

        CollectResourcesFromBuilding();

        if (visualObject != null)
        {
            visualObject.SetActive(true);
        }

        currentState = ColonistState.Idle;
    }

    protected virtual float GetInteractionTime()
    {
        return 3f;
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
            carriedWarmleaf = 0;
        }

        if (carriedThunderite > 0)
        {
            resourceManager.AddResource("thunderite", carriedThunderite);
            carriedThunderite = 0;
        }

        if (carriedMirallite > 0)
        {
            resourceManager.AddResource("mirallite", carriedMirallite);
            carriedMirallite = 0;
        }
    }

    // Вспомогательные методы для пути
    private Vector3 CalculateOptimalAvoidance(Vector3 start, Vector3 target, Vector3 obstacle)
    {
        Vector3 toTarget = (target - start).normalized;
        Vector3 perpendicular = new Vector3(-toTarget.y, toTarget.x, 0);
        float rightClearance = CheckDirectionClearance(perpendicular);
        float leftClearance = CheckDirectionClearance(-perpendicular);
        return (rightClearance >= leftClearance) ? perpendicular : -perpendicular;
    }

    private Vector3 CalculateSimpleAvoidancePoint(Vector3 start, Vector3 target)
    {
        Vector3 direction = (target - start).normalized;
        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0);
        return start + perpendicular * buildingAvoidanceRadius;
    }

    private Vector3 FindMainObstacle(Vector3 start, Vector3 target)
    {
        Vector3 direction = (target - start).normalized;
        float distance = Vector3.Distance(start, target);
        RaycastHit2D hit = Physics2D.Raycast(start, direction, distance, buildingLayer | obstacleLayer);

        if (hit.collider != null && hit.collider.gameObject != gameObject &&
            hit.collider.transform != targetBuilding && hit.collider.transform != mainBuilding)
        {
            return hit.point;
        }

        return Vector3.zero;
    }

    private Vector3 CalculateAvoidanceDirection(Vector3 moveDirection, Vector3 obstacleNormal)
    {
        Vector3 perpendicular1 = new Vector3(-moveDirection.y, moveDirection.x, 0);
        Vector3 perpendicular2 = new Vector3(moveDirection.y, -moveDirection.x, 0);
        float clearance1 = CheckDirectionClearance(perpendicular1);
        float clearance2 = CheckDirectionClearance(perpendicular2);
        Vector3 avoidanceDirection = (clearance1 >= clearance2) ? perpendicular1 : perpendicular2;
        return Vector3.Lerp(moveDirection, avoidanceDirection, 0.7f).normalized;
    }

    private void SmoothPath()
    {
        if (currentPath.Count < 3) return;
        List<Vector3> smoothedPath = new List<Vector3> { currentPath[0] };

        for (int i = 1; i < currentPath.Count - 1; i++)
        {
            if (!IsPathClear(smoothedPath[smoothedPath.Count - 1], currentPath[i + 1]))
            {
                smoothedPath.Add(currentPath[i]);
            }
        }

        smoothedPath.Add(currentPath[currentPath.Count - 1]);
        currentPath = smoothedPath;
    }

    private float GetPointClearance(Vector3 point)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(point, 0.5f, obstacleLayer | buildingLayer);
        float clearance = 1f;

        foreach (var collider in colliders)
        {
            if (collider.gameObject != gameObject &&
                collider.transform != targetBuilding &&
                collider.transform != mainBuilding)
            {
                float distance = Vector3.Distance(point, collider.transform.position);
                clearance = Mathf.Min(clearance, distance);
            }
        }

        return clearance;
    }

    #endregion
}