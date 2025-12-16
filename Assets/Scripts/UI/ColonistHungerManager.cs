using UnityEngine;

public class ColonistHungerManager : MonoBehaviour
{
    public static ColonistHungerManager Instance { get; private set; }

    [Header("Настройки потребления")]
    [SerializeField] private float idleMirallitePerMinute = 2f;     // X единиц мираллита в минуту на неработающего колониста
    [SerializeField] private float workingMirallitePerMinute = 4f;  // X единиц мираллита в минуту на работающего колониста
    [SerializeField] private float consumptionInterval = 15f;       // Интервал списания (секунды) - 15 секунд

    [Header("Настройки голода")]
    [SerializeField] private float starvationPhase1Duration = 60f; // Фаза истощения (секунды)
    [SerializeField] private float starvationPhase2Interval = 8f;  // Интервал смерти в фазе 2 (секунды)
    [SerializeField] private float speedDebuffMultiplier = 0.5f;   // Множитель скорости при голоде (50%)

    [Header("Состояние")]
    private bool isStarving = false;
    private float starvationTimer = 0f;
    private float consumptionTimer = 0f;
    private float deathTimer = 0f;
    private bool isPhase2 = false;

    // Ссылки на менеджеры
    private ColonistManager colonistManager;
    private DistributionManager distributionManager;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        colonistManager = ColonistManager.Instance;
        distributionManager = DistributionManager.Instance;

        consumptionTimer = consumptionInterval;
        Debug.Log("ColonistHungerManager инициализирован");
        Debug.Log($"Потребление: {idleMirallitePerMinute} мираллита/минуту на неработающего");
        Debug.Log($"Потребление: {workingMirallitePerMinute} мираллита/минуту на работающего");
        Debug.Log($"Интервал списания: {consumptionInterval} секунд");
    }

    void Update()
    {
        UpdateConsumption();
        UpdateStarvation();
    }

    private void UpdateConsumption()
    {
        consumptionTimer -= Time.deltaTime;

        if (consumptionTimer <= 0f)
        {
            ConsumeMirallite();
            consumptionTimer = consumptionInterval;
        }
    }

    private void ConsumeMirallite()
    {
        if (ResourceManager.Instance == null || colonistManager == null || distributionManager == null) return;

        int totalColonists = colonistManager.GetTotalColonists();
        if (totalColonists <= 0) return;

        // Рассчитываем сколько работающих колонистов
        int workingColonists = distributionManager.GetTotalAssignedWorkers();
        int idleColonists = totalColonists - workingColonists;

        // Рассчитываем потребление за интервал
        float idleConsumptionPerSecond = idleMirallitePerMinute / 60f;
        float workingConsumptionPerSecond = workingMirallitePerMinute / 60f;

        float idleMiralliteNeeded = idleConsumptionPerSecond * consumptionInterval * idleColonists;
        float workingMiralliteNeeded = workingConsumptionPerSecond * consumptionInterval * workingColonists;

        float totalMiralliteNeeded = idleMiralliteNeeded + workingMiralliteNeeded;
        int miralliteToConsume = Mathf.CeilToInt(totalMiralliteNeeded);

        // Получаем текущее количество мираллита
        int currentMirallite = ResourceManager.Instance.GetMiralliteAmount();

        if (currentMirallite > 0)
        {
            // Есть еда - списываем
            int consumed = Mathf.Min(miralliteToConsume, currentMirallite);
            bool spent = ResourceManager.Instance.TrySpendResource("mirallite", consumed);

            if (spent)
            {
                // Если смогли списать всю нужную сумму
                if (consumed >= miralliteToConsume)
                {
                    // Еда есть - снимаем голод если был
                    if (isStarving)
                    {
                        EndStarvation();
                    }

                    // Логируем каждые 4 интервала (примерно минута)
                    if (Time.frameCount % (int)(consumptionInterval * 4 * 60) == 0) // 60 FPS * 4 интервала
                    {
                        Debug.Log($"Колонисты съели {consumed} мираллита за {consumptionInterval} сек. " +
                                 $"Работающих: {workingColonists}, Неработающих: {idleColonists}. " +
                                 $"Осталось мираллита: {currentMirallite - consumed}");
                    }
                }
                else
                {
                    // Не смогли списать всю сумму - начинаем голод
                    if (!isStarving)
                    {
                        StartStarvation();
                    }

                    Debug.Log($"Недостаточно мираллита! Нужно {miralliteToConsume}, съели {consumed}. " +
                             $"Работающих: {workingColonists}, Неработающих: {idleColonists}. Начинается голод...");
                }
            }
        }
        else
        {
            // Нет мираллита вообще
            if (!isStarving)
            {
                StartStarvation();
            }

            // Логируем только при переходе в голод или при серьезной ситуации
            if (isStarving && Time.frameCount % (int)(consumptionInterval * 60) == 0) // Каждые 15 секунд при 60 FPS
            {
                Debug.Log($"Мираллит закончился! Колонисты голодают... " +
                         $"Работающих: {workingColonists}, Неработающих: {idleColonists}");
            }
        }
    }

    // Остальные методы оставляем как есть, только добавим логику с разным потреблением
    private void StartStarvation()
    {
        isStarving = true;
        starvationTimer = 0f;
        isPhase2 = false;
        deathTimer = 0f;

        Debug.Log("=== НАЧАЛСЯ ГОЛОД ===");
        Debug.Log("Фаза 1: Истощение (60 секунд до ухудшения)");

        // Уведомление для игрока
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowNotification("Колонисты голодают!", 3f);
        }
    }

    private void UpdateStarvation()
    {
        if (!isStarving) return;

        starvationTimer += Time.deltaTime;

        // Фаза 1: Истощение (первые 60 секунд)
        if (!isPhase2 && starvationTimer >= starvationPhase1Duration)
        {
            StartPhase2();
        }

        // Фаза 2: Вымирание
        if (isPhase2)
        {
            deathTimer += Time.deltaTime;

            if (deathTimer >= starvationPhase2Interval)
            {
                KillColonist();
                deathTimer = 0f;
            }
        }
    }

    private void StartPhase2()
    {
        isPhase2 = true;

        Debug.Log("=== ФАЗА 2: ВЫМИРАНИЕ ===");
        Debug.Log($"Дебафф скорости: -{(1f - speedDebuffMultiplier) * 100}%");
        Debug.Log($"Колонисты начнут умирать каждые {starvationPhase2Interval} секунд");

        // Уведомление для игрока
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowNotification("Колонисты умирают от голода!", 4f);
        }
    }

    private void KillColonist()
    {
        if (colonistManager == null) return;

        int totalColonists = colonistManager.GetTotalColonists();
        if (totalColonists <= 0) return;

        // Убиваем одного колониста
        bool killed = colonistManager.RemoveColonist(1);

        if (killed)
        {
            int remaining = colonistManager.GetTotalColonists();
            Debug.Log($"Колонист умер от голода! Осталось: {remaining}");

            // Уведомление для игрока каждую 3-ю смерть
            if (UIManager.Instance != null && remaining % 3 == 0)
            {
                UIManager.Instance.ShowNotification($"Колонист умер от голода! Осталось: {remaining}", 3f);
            }
        }
    }

    private void EndStarvation()
    {
        isStarving = false;
        isPhase2 = false;
        starvationTimer = 0f;
        deathTimer = 0f;

        Debug.Log("=== ГОЛОД ПРЕКРАТИЛСЯ ===");
        Debug.Log("Колонисты снова сыты. Дебаффы сняты.");

        // Уведомление для игрока
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowNotification("Колонисты накормлены!", 2f);
        }
    }

    #region Публичные методы

    public bool IsStarving() => isStarving;
    public bool IsInPhase2() => isPhase2;
    public float GetStarvationTimer() => starvationTimer;
    public float GetTimeUntilPhase2()
    {
        if (!isStarving || isPhase2) return 0f;
        return Mathf.Max(0f, starvationPhase1Duration - starvationTimer);
    }

    // Новые методы для получения информации о потреблении
    public float GetIdleConsumptionPerMinute() => idleMirallitePerMinute;
    public float GetWorkingConsumptionPerMinute() => workingMirallitePerMinute;

    public float GetTotalConsumptionPerMinute()
    {
        if (colonistManager == null || distributionManager == null) return 0f;

        int totalColonists = colonistManager.GetTotalColonists();
        int workingColonists = distributionManager.GetTotalAssignedWorkers();
        int idleColonists = totalColonists - workingColonists;

        return (idleColonists * idleMirallitePerMinute) + (workingColonists * workingMirallitePerMinute);
    }

    public float GetConsumptionPerSecond()
    {
        return GetTotalConsumptionPerMinute() / 60f;
    }

    // Метод для UI чтобы показать детальное потребление
    public (int working, int idle, float workingConsumption, float idleConsumption, float total) GetConsumptionDetails()
    {
        if (colonistManager == null || distributionManager == null)
            return (0, 0, 0, 0, 0);

        int totalColonists = colonistManager.GetTotalColonists();
        int workingColonists = distributionManager.GetTotalAssignedWorkers();
        int idleColonists = totalColonists - workingColonists;

        float workingConsumption = workingColonists * workingMirallitePerMinute;
        float idleConsumption = idleColonists * idleMirallitePerMinute;
        float totalConsumption = workingConsumption + idleConsumption;

        return (workingColonists, idleColonists, workingConsumption, idleConsumption, totalConsumption);
    }

    #endregion

    #region Настройки для балансировки

    public void SetIdleConsumption(float consumptionPerMinute)
    {
        idleMirallitePerMinute = Mathf.Clamp(consumptionPerMinute, 0.1f, 100f);
        Debug.Log($"Потребление неработающих: {idleMirallitePerMinute} мираллита/минуту");
    }

    public void SetWorkingConsumption(float consumptionPerMinute)
    {
        workingMirallitePerMinute = Mathf.Clamp(consumptionPerMinute, 0.1f, 100f);
        Debug.Log($"Потребление работающих: {workingMirallitePerMinute} мираллита/минуту");
    }

    public void SetConsumptionInterval(float interval)
    {
        consumptionInterval = Mathf.Clamp(interval, 1f, 60f);
        Debug.Log($"Интервал потребления: {consumptionInterval} секунд");
    }

    public void SetStarvationPhase1Duration(float duration)
    {
        starvationPhase1Duration = Mathf.Clamp(duration, 10f, 300f);
        Debug.Log($"Длительность фазы истощения: {starvationPhase1Duration} секунд");
    }

    public void SetStarvationPhase2Interval(float interval)
    {
        starvationPhase2Interval = Mathf.Clamp(interval, 1f, 30f);
        Debug.Log($"Интервал смерти в фазе 2: {starvationPhase2Interval} секунд");
    }

    public void SetSpeedDebuffMultiplier(float multiplier)
    {
        speedDebuffMultiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
        Debug.Log($"Множитель скорости при голоде: {speedDebuffMultiplier * 100}%");
    }

    #endregion

    #region Утилиты для отладки

    public void ForceStarvation()
    {
        StartStarvation();
        starvationTimer = starvationPhase1Duration - 1f; // Почти фаза 2
        Debug.Log("Голод принудительно активирован");
    }

    public void ForceEndStarvation()
    {
        EndStarvation();
        Debug.Log("Голод принудительно прекращен");
    }

    public void AddTestMirallite(int amount)
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddResource("mirallite", amount);
            Debug.Log($"Добавлено {amount} мираллита для теста");
        }
    }

    #endregion
}