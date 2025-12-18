using UnityEngine;
using System.Collections.Generic;

public class ColonistManager : MonoBehaviour
{
    public static ColonistManager Instance { get; private set; }

    [Header("Настройки колонистов")]
    [SerializeField] private float colonistArrivalInterval = 8f;
    [SerializeField] private int baseMaxColonistsPerHouse = 5;
    [SerializeField] private int startingColonists = 0;

    [Header("Звуки колонистов")]
    [SerializeField] private AudioClip arrivalSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private float colonistSoundVolume = 0.8f;

    private void PlayColonistSound(AudioClip clip)
    {
        if (clip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEffect(clip, colonistSoundVolume);
        }
    }

    [Header("Система очереди и терпения")]
    [SerializeField] private float basePatienceSpeed = 1f; // Vbase - базовая скорость падения терпения (% в секунду)
    [SerializeField] private float maxPatience = 100f;     // Максимальное терпение (100%)

    [Header("Состояние")]
    private int totalColonists = 0;
    private int availableColonists = 0;
    private int assignedColonists = 0;
    private int totalHousingCapacity = 0;
    private int maxColonistsPerHouse = 5; // Текущее значение с учетом технологий

    // Очередь ожидания
    private Queue<ColonistInQueue> waitingQueue = new Queue<ColonistInQueue>();

    // Терпение
    private float currentPatience = 100f;
    private bool isPatienceActive = false;

    private float colonistTimer = 0f;
    private BuildingManager buildingManager;

    // Класс для колониста в очереди
    private class ColonistInQueue
    {
        public float timeEnteredQueue;

        public ColonistInQueue()
        {
            timeEnteredQueue = Time.time;
        }
    }

    // События
    public delegate void ColonistChangedDelegate(int total, int available, int capacity, int queueLength, float patience);
    public event ColonistChangedDelegate OnColonistChanged;

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
        buildingManager = BuildingManager.Instance;
        totalColonists = startingColonists;
        availableColonists = startingColonists;
        currentPatience = maxPatience;

        // Применяем начальные бонусы от технологий
        ApplyTechnologyBonuses();

        colonistTimer = colonistArrivalInterval;

        Debug.Log($"ColonistManager инициализирован. Начальные колонисты: {totalColonists}");
    }

    void Update()
    {
        UpdateColonistArrival();
        UpdateHousingCapacity();
        UpdatePatienceSystem();

        // Обновляем бонусы от технологий (на случай если их исследовали)
        ApplyTechnologyBonuses();
    }

    private void ApplyTechnologyBonuses()
    {
        // Получаем бонусы к вместимости жилища
        int capacityBonus = 0;
        if (TechnologyManager.Instance != null)
        {
            capacityBonus = TechnologyManager.Instance.GetColonistCapacityBonus();
        }

        // Обновляем максимальное количество колонистов на дом
        maxColonistsPerHouse = baseMaxColonistsPerHouse + capacityBonus;

        // Пересчитываем общую вместимость
        UpdateHousingCapacity();
    }

    private void UpdateColonistArrival()
    {
        colonistTimer -= Time.deltaTime;

        if (colonistTimer <= 0f)
        {
            AddNewColonistToSystem();
            colonistTimer = colonistArrivalInterval;
        }
    }

    private void AddNewColonistToSystem()
    {
        // Пытаемся сразу заселить
        if (totalColonists < totalHousingCapacity)
        {
            // Есть свободное место - заселяем сразу
            AddColonist(1);
            PlayColonistSound(arrivalSound);
            Debug.Log($"Прибыл новый колонист! Заселен сразу. Всего: {totalColonists}/{totalHousingCapacity}");
        }
        else
        {
            // Нет места - отправляем в очередь
            waitingQueue.Enqueue(new ColonistInQueue());

            // Активируем систему терпения если очередь стала непустой
            if (waitingQueue.Count == 1)
            {
                isPatienceActive = true;
                Debug.Log($"Очередь ожидания создана. Активирована система терпения.");
            }

            Debug.Log($"Прибыл новый колонист! Отправлен в очередь. Очередь: {waitingQueue.Count}");
            LogPatienceStatus();
        }
    }

    private void UpdateHousingCapacity()
    {
        if (buildingManager == null) return;

        BuildingData houseData = buildingManager.GetHouseData();
        if (houseData == null) return;

        int houseCount = buildingManager.GetBuildingCount(houseData);
        int newCapacity = houseCount * maxColonistsPerHouse;

        if (newCapacity != totalHousingCapacity)
        {
            int oldCapacity = totalHousingCapacity;
            totalHousingCapacity = newCapacity;

            // Если вместимость увеличилась, пытаемся заселить из очереди
            if (newCapacity > oldCapacity)
            {
                int newSpots = newCapacity - oldCapacity;
                TrySettleFromQueue(newSpots);
            }
            // Если вместимость уменьшилась
            else if (newCapacity < oldCapacity)
            {
                int lostSpots = oldCapacity - newCapacity;
                HandleLostHousing(lostSpots);
            }

            NotifyColonistChanged();
            Debug.Log($"Вместимость жилья: {houseCount} домов × {maxColonistsPerHouse} = {totalHousingCapacity} мест");
        }
    }

    private void UpdatePatienceSystem()
    {
        if (!isPatienceActive || waitingQueue.Count == 0) return;

        // Формула: V(n) = Vbase * n
        float patienceDecreaseSpeed = basePatienceSpeed * waitingQueue.Count;
        currentPatience -= patienceDecreaseSpeed * Time.deltaTime;

        // Логируем каждую секунду при сильном падении терпения
        if (Time.frameCount % 60 == 0 && currentPatience < 50f) // Каждую секунду при 60 FPS
        {
            LogPatienceStatus();
        }

        // Проверяем Game Over
        if (currentPatience <= 0f)
        {
            currentPatience = 0f;
            TriggerGameOver();
        }
    }

    private void TrySettleFromQueue(int availableSpots)
    {
        if (availableSpots <= 0 || waitingQueue.Count == 0) return;

        int spotsToFill = Mathf.Min(availableSpots, waitingQueue.Count);

        for (int i = 0; i < spotsToFill; i++)
        {
            waitingQueue.Dequeue(); // Убираем из очереди
            AddColonist(1);         // Добавляем в систему
            PlayColonistSound(arrivalSound);
        }

        // Если очередь опустела - сбрасываем терпение
        if (waitingQueue.Count == 0)
        {
            ResetPatience();
            Debug.Log($"Очередь опустела! Заселено {spotsToFill} колонистов. Терпение восстановлено.");
        }
        else
        {
            Debug.Log($"Заселено {spotsToFill} колонистов из очереди. Осталось в очереди: {waitingQueue.Count}");
        }

        LogPatienceStatus();
    }

    private void HandleLostHousing(int lostSpots)
    {
        if (lostSpots <= 0) return;

        // Сначала пытаемся выселить доступных колонистов
        int colonistsToRemove = Mathf.Min(availableColonists, lostSpots);
        RemoveColonist(colonistsToRemove);

        // Если все равно недостаточно места
        int remainingSpots = lostSpots - colonistsToRemove;
        if (remainingSpots > 0)
        {
            // Нужно выселить назначенных колонистов
            UnassignColonist(Mathf.Min(assignedColonists, remainingSpots));
            RemoveColonist(remainingSpots);
        }

        Debug.Log($"Потеряно {lostSpots} мест для жилья. Выселено колонистов: {lostSpots}");
    }

    #region Система терпения

    private void ResetPatience()
    {
        currentPatience = maxPatience;
        isPatienceActive = false;
        Debug.Log($"Терпение полностью восстановлено: {currentPatience}%");
    }

    private void TriggerGameOver()
    {
        Debug.Log($"=== GAME OVER ===");
        Debug.Log($"Терпение колонистов иссякло!");
        Debug.Log($"Очередь: {waitingQueue.Count} человек");
        Debug.Log($"Жилье: {totalColonists}/{totalHousingCapacity}");
        Debug.Log($"Игра завершена из-за недовольства колонистов.");
        Debug.Log($"=================");

        // Здесь можно вызвать GameManager для завершения игры
        // GameManager.Instance?.SetGameOver(true);
    }

    private void LogPatienceStatus()
    {
        Debug.Log($"Терпение: {currentPatience:F1}% | Очередь: {waitingQueue.Count} | Скорость падения: {basePatienceSpeed * waitingQueue.Count:F1}%/сек");
    }

    #endregion

    #region Публичные методы

    public bool AddColonist(int amount = 1)
    {
        if (amount <= 0) return false;

        if (totalColonists + amount > totalHousingCapacity)
        {
            Debug.Log($"Недостаточно жилья! Нужно мест: {totalColonists + amount}, есть: {totalHousingCapacity}");
            return false;
        }

        totalColonists += amount;
        availableColonists += amount;

        NotifyColonistChanged();
        return true;
    }

    public bool RemoveColonist(int amount = 1)
    {
        if (amount <= 0 || totalColonists < amount) return false;

        int availableToRemove = Mathf.Min(availableColonists, amount);
        availableColonists -= availableToRemove;

        int assignedToRemove = amount - availableToRemove;
        if (assignedToRemove > 0)
        {
            UnassignColonist(assignedToRemove);
        }

        totalColonists -= amount;
        PlayColonistSound(deathSound);
        NotifyColonistChanged();
        return true;
    }

    public bool AssignColonist(int amount = 1)
    {
        if (amount <= 0 || availableColonists < amount)
        {
            Debug.Log($"Нельзя назначить {amount} колонистов. Доступно: {availableColonists}");
            return false;
        }

        availableColonists -= amount;
        assignedColonists += amount;

        NotifyColonistChanged();

        Debug.Log($"Назначено {amount} колонистов. Доступно: {availableColonists}, Назначено: {assignedColonists}");
        return true;
    }

    public bool UnassignColonist(int amount = 1)
    {
        if (amount <= 0 || assignedColonists < amount)
        {
            Debug.Log($"Нельзя снять {amount} колонистов. Назначено: {assignedColonists}");
            return false;
        }

        assignedColonists -= amount;
        availableColonists += amount;

        NotifyColonistChanged();

        Debug.Log($"Снято {amount} колонистов. Доступно: {availableColonists}, Назначено: {assignedColonists}");
        return true;
    }

    #endregion

    #region Вспомогательные методы

    private void NotifyColonistChanged()
    {
        OnColonistChanged?.Invoke(
            totalColonists,
            availableColonists,
            totalHousingCapacity,
            waitingQueue.Count,
            currentPatience
        );

        // Краткое логирование состояния
        if (Time.frameCount % 300 == 0) // Каждые 5 секунд при 60 FPS
        {
            Debug.Log($"Колонисты: {totalColonists}/{totalHousingCapacity} | Очередь: {waitingQueue.Count} | Терпение: {currentPatience:F1}%");
        }
    }

    #endregion

    #region Публичные геттеры

    public int GetTotalColonists() => totalColonists;
    public int GetAvailableColonists() => availableColonists;
    public int GetAssignedColonists() => assignedColonists;
    public int GetHousingCapacity() => totalHousingCapacity;
    public float GetColonistArrivalInterval() => colonistArrivalInterval;
    public float GetColonistTimer() => colonistTimer;
    public int GetMaxColonistsPerHouse() => maxColonistsPerHouse;
    public int GetBaseMaxColonistsPerHouse() => baseMaxColonistsPerHouse;

    public int GetWaitingQueueLength() => waitingQueue.Count;
    public float GetCurrentPatience() => currentPatience;
    public float GetPatiencePercentage() => currentPatience / maxPatience;
    public bool IsPatienceActive() => isPatienceActive;

    public bool HasAvailableColonists() => availableColonists > 0;
    public bool HasHousingSpace() => totalColonists < totalHousingCapacity;
    public float GetHousingUsagePercentage() => totalHousingCapacity > 0 ?
        (float)totalColonists / totalHousingCapacity : 0f;

    #endregion

    #region Настройки для балансировки

    public void SetColonistArrivalInterval(float interval)
    {
        colonistArrivalInterval = Mathf.Clamp(interval, 1f, 60f);
        Debug.Log($"Интервал прибытия колонистов: {colonistArrivalInterval} сек");
    }

    public void SetBaseMaxColonistsPerHouse(int max)
    {
        baseMaxColonistsPerHouse = Mathf.Clamp(max, 1, 20);
        Debug.Log($"Базовая вместимость жилища: {baseMaxColonistsPerHouse}");
        ApplyTechnologyBonuses();
    }

    public void SetBasePatienceSpeed(float speed)
    {
        basePatienceSpeed = Mathf.Clamp(speed, 0.1f, 10f);
        Debug.Log($"Базовая скорость падения терпения: {basePatienceSpeed}%/сек");
    }

    #endregion
}