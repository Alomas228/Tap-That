using UnityEngine;
using TMPro;

public class ResourceUI : MonoBehaviour
{
    [Header("Тексты ресурсов")]
    [SerializeField] private TextMeshProUGUI warmleafText;
    [SerializeField] private TextMeshProUGUI thunderiteText;
    [SerializeField] private TextMeshProUGUI miralliteText;
    [SerializeField] private TextMeshProUGUI colonistText; // НОВЫЙ: для колонистов

    [Header("Иконки (опционально)")]
    [SerializeField] private UnityEngine.UI.Image warmleafIcon;
    [SerializeField] private UnityEngine.UI.Image thunderiteIcon;
    [SerializeField] private UnityEngine.UI.Image miralliteIcon;
    [SerializeField] private UnityEngine.UI.Image colonistIcon; // НОВЫЙ: иконка колонистов

    [Header("Настройки отображения колонистов")]
    [SerializeField] private bool showColonistQueue = true; // Показывать очередь в скобках
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = new Color(1, 0.8f, 0); // Желтый при очереди
    [SerializeField] private Color dangerColor = Color.red; // Красный при низком терпении

    private ResourceManager resourceManager;
    private ColonistManager colonistManager;

    void Start()
    {
        resourceManager = ResourceManager.Instance;
        colonistManager = ColonistManager.Instance;

        // Подписываемся на события ресурсов
        if (resourceManager != null)
        {
            resourceManager.OnResourceChanged.AddListener(OnResourceChanged);
        }

        // Подписываемся на события колонистов
        if (colonistManager != null)
        {
            colonistManager.OnColonistChanged += OnColonistChanged;
        }

        // Инициализируем отображение
        UpdateAllResources();
        UpdateColonistDisplay();
    }

    void Update()
    {
        // Оптимизация: обновляем колонистов каждые N кадров
        if (Time.frameCount % 30 == 0) // Каждые 30 кадров (~0.5 сек при 60 FPS)
        {
            UpdateColonistDisplay();
        }
    }

    #region Обработка событий ресурсов

    private void OnResourceChanged(string resourceId, int newAmount)
    {
        switch (resourceId)
        {
            case "warmleaf":
                if (warmleafText != null)
                    warmleafText.text = newAmount.ToString();
                break;

            case "thunderite":
                if (thunderiteText != null)
                    thunderiteText.text = newAmount.ToString();
                break;

            case "mirallite":
                if (miralliteText != null)
                    miralliteText.text = newAmount.ToString();
                break;
        }
    }

    private void UpdateAllResources()
    {
        if (resourceManager == null) return;

        if (warmleafText != null)
            warmleafText.text = resourceManager.GetWarmleafAmount().ToString();

        if (thunderiteText != null)
            thunderiteText.text = resourceManager.GetThunderiteAmount().ToString();

        if (miralliteText != null)
            miralliteText.text = resourceManager.GetMiralliteAmount().ToString();
    }

    #endregion

    #region Обработка колонистов

    private void OnColonistChanged(int total, int available, int capacity, int queueLength, float patience)
    {
        UpdateColonistDisplay();
    }

    private void UpdateColonistDisplay()
    {
        if (colonistManager == null || colonistText == null) return;

        int totalColonists = colonistManager.GetTotalColonists();
        int housingCapacity = colonistManager.GetHousingCapacity();
        int queueLength = colonistManager.GetWaitingQueueLength();
        float patience = colonistManager.GetCurrentPatience();

        // Форматируем текст
        string colonistString = FormatColonistText(totalColonists, housingCapacity, queueLength);

        // Обновляем текст
        colonistText.text = colonistString;

        // Меняем цвет в зависимости от состояния
        UpdateColonistColor(colonistText, queueLength, patience, housingCapacity, totalColonists);

        // Можно обновить иконку если нужно
        if (colonistIcon != null)
        {
            UpdateColonistIconColor(colonistIcon, queueLength, patience);
        }
    }

    private string FormatColonistText(int total, int capacity, int queue)
    {
        if (showColonistQueue && queue > 0)
        {
            // Формат: "5/10 (+3)" - колонисты/вместимость (+очередь)
            return $"{total}/{capacity} (+{queue})";
        }
        else
        {
            // Формат: "5/10" - колонисты/вместимость
            return $"{total}/{capacity}";
        }
    }

    private void UpdateColonistColor(TextMeshProUGUI text, int queueLength, float patience, int capacity, int total)
    {
        if (text == null) return;

        // Цвет по приоритету:
        // 1. Опасность: терпение < 20% ИЛИ очередь > 3
        // 2. Предупреждение: очередь > 0 ИЛИ заполнение > 90%
        // 3. Нормально: всё хорошо

        float fillPercentage = capacity > 0 ? (float)total / capacity : 0f;

        if (patience < 20f || queueLength >= 3)
        {
            text.color = dangerColor;
        }
        else if (queueLength > 0 || fillPercentage > 0.9f)
        {
            text.color = warningColor;
        }
        else
        {
            text.color = normalColor;
        }
    }

    private void UpdateColonistIconColor(UnityEngine.UI.Image icon, int queueLength, float patience)
    {
        if (icon == null) return;

        // Можно менять цвет иконки или добавлять эффекты
        if (patience < 30f)
        {
            // Мигание при низком терпении
            float blink = Mathf.PingPong(Time.time * 2f, 1f);
            icon.color = Color.Lerp(normalColor, dangerColor, blink);
        }
        else if (queueLength > 0)
        {
            icon.color = warningColor;
        }
        else
        {
            icon.color = normalColor;
        }
    }

    #endregion

    #region Утилиты

    // Публичный метод для обновления вручную
    public void ForceUpdateAll()
    {
        UpdateAllResources();
        UpdateColonistDisplay();
    }

    #endregion

    void OnDestroy()
    {
        // Отписываемся от событий
        if (resourceManager != null)
        {
            resourceManager.OnResourceChanged.RemoveListener(OnResourceChanged);
        }

        if (colonistManager != null)
        {
            colonistManager.OnColonistChanged -= OnColonistChanged;
        }
    }
}