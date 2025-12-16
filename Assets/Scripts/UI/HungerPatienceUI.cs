using UnityEngine;
using TMPro;

public class HungerPatienceUI : MonoBehaviour
{
    [Header("Слайдеры-прогрессбары")]
    [SerializeField] private GradientSlider patienceSlider;
    [SerializeField] private GradientSlider hungerSlider;

    [Header("Текстовые индикаторы")]
    [SerializeField] private TextMeshProUGUI patienceText;
    [SerializeField] private TextMeshProUGUI hungerText;

    [Header("Надписи")]
    [SerializeField] private string patienceLabel = "Терпение: ";
    [SerializeField] private string hungerLabel = "Голод: ";

    [Header("Цвета текста")]
    [SerializeField] private Color textNormalColor = Color.white;
    [SerializeField] private Color textWarningColor = new Color(1, 0.8f, 0);
    [SerializeField] private Color textDangerColor = Color.red;

    [Header("Настройки отображения")]
    [SerializeField] private bool showExactPercentages = true;
    [SerializeField] private bool showHungerPhaseText = true;

    // Ссылки на менеджеры
    private ColonistManager colonistManager;
    private ColonistHungerManager hungerManager;

    // Кэшированные значения для оптимизации
    private float lastPatienceValue = -1f;
    private float lastHungerValue = -1f;

    void Start()
    {
        colonistManager = ColonistManager.Instance;
        hungerManager = ColonistHungerManager.Instance;

        InitializeSliders();

        // Подписываемся на события
        if (colonistManager != null)
        {
            colonistManager.OnColonistChanged += OnColonistChanged;
        }

        UpdateUI();
    }

    void Update()
    {
        // Обновляем каждые 3 кадра для плавности
        if (Time.frameCount % 3 == 0)
        {
            UpdateUI();
        }
    }

    void OnDestroy()
    {
        if (colonistManager != null)
        {
            colonistManager.OnColonistChanged -= OnColonistChanged;
        }
    }

    private void InitializeSliders()
    {
        // Настраиваем слайдер терпения
        if (patienceSlider != null)
        {
            patienceSlider.SetValue(1f);
            // Для терпения - нормальное заполнение слева направо
            patienceSlider.SetReverseFill(false);
        }

        // Настраиваем слайдер голода
        if (hungerSlider != null)
        {
            hungerSlider.SetValue(0f);
            // Для голода - обратное заполнение (чем больше голод, тем больше заполнение)
            hungerSlider.SetReverseFill(true);
        }
    }

    private void OnColonistChanged(int total, int available, int capacity, int queueLength, float patience)
    {
        // Триггерим обновление при изменении колонистов
        UpdatePatienceDisplay();
    }

    private void UpdateUI()
    {
        UpdatePatienceDisplay();
        UpdateHungerDisplay();
    }

    private void UpdatePatienceDisplay()
    {
        if (colonistManager == null || patienceSlider == null) return;

        float patiencePercentage = colonistManager.GetPatiencePercentage();

        // Обновляем только если значение изменилось
        if (Mathf.Abs(patiencePercentage - lastPatienceValue) > 0.001f)
        {
            lastPatienceValue = patiencePercentage;

            // Обновляем слайдер
            patienceSlider.SetValue(patiencePercentage);

            // Включаем пульсацию при низком терпении
            patienceSlider.EnablePulse(patiencePercentage < 0.3f);

            // Обновляем текст
            if (patienceText != null)
            {
                if (showExactPercentages)
                {
                    patienceText.text = $"{patienceLabel}{patiencePercentage * 100:F1}%";
                }
                else
                {
                    string status = patiencePercentage switch
                    {
                        > 0.7f => "Высокое",
                        > 0.4f => "Среднее",
                        > 0.2f => "Низкое",
                        _ => "КРИТИЧЕСКОЕ!"
                    };
                    patienceText.text = $"{patienceLabel}{status}";
                }

                // Меняем цвет текста
                UpdateTextColor(patienceText, patiencePercentage);
            }
        }
    }

    private void UpdateHungerDisplay()
    {
        if (hungerManager == null || hungerSlider == null) return;

        float hungerValue = CalculateHungerValue();

        // Обновляем только если значение изменилось
        if (Mathf.Abs(hungerValue - lastHungerValue) > 0.001f)
        {
            lastHungerValue = hungerValue;

            // Обновляем слайдер
            hungerSlider.SetValue(hungerValue);

            // Включаем пульсацию при голоде
            hungerSlider.EnablePulse(hungerManager.IsStarving());

            // Обновляем текст
            if (hungerText != null)
            {
                string hungerTextStr = GetHungerText(hungerValue);
                hungerText.text = $"{hungerLabel}{hungerTextStr}";

                // Меняем цвет текста
                UpdateTextColor(hungerText, hungerValue, true);
            }
        }
    }

    private float CalculateHungerValue()
    {
        if (!hungerManager.IsStarving()) return 0f;

        if (hungerManager.IsInPhase2())
        {
            return 1f; // Полный голод
        }
        else
        {
            // В фазе 1 - прогресс до фазы 2
            float timeUntilPhase2 = hungerManager.GetTimeUntilPhase2();
            float phase1Duration = 60f; // Базовое значение, можно получить из менеджера

            return 1f - (timeUntilPhase2 / phase1Duration); // Инвертируем: 0->1 (нет голода -> полный голод)
        }
    }

    private string GetHungerText(float hungerValue)
    {
        if (!hungerManager.IsStarving())
        {
            return showExactPercentages ? "0%" : "Нет";
        }

        if (hungerManager.IsInPhase2())
        {
            return showHungerPhaseText ? "ВЫМИРАНИЕ!" : "100%";
        }

        if (showExactPercentages)
        {
            return $"{(hungerValue * 100):F1}%";
        }
        else
        {
            return hungerValue switch
            {
                < 0.3f => "Легкий",
                < 0.6f => "Средний",
                < 0.9f => "Сильный",
                _ => "КРИТИЧЕСКИЙ!"
            };
        }
    }

    private void UpdateTextColor(TextMeshProUGUI textElement, float value, bool isHunger = false)
    {
        if (textElement == null) return;

        if (isHunger)
        {
            // Для голода - опасность на высоких значениях
            textElement.color = value switch
            {
                > 0.8f => textDangerColor,
                > 0.5f => textWarningColor,
                _ => textNormalColor
            };
        }
        else
        {
            // Для терпения - опасность на низких значениях
            textElement.color = value switch
            {
                < 0.2f => textDangerColor,
                < 0.5f => textWarningColor,
                _ => textNormalColor
            };
        }
    }

    #region Публичные методы для настройки

    public void SetPatienceVisibility(bool visible)
    {
        if (patienceSlider != null) patienceSlider.gameObject.SetActive(visible);
        if (patienceText != null) patienceText.gameObject.SetActive(visible);
    }

    public void SetHungerVisibility(bool visible)
    {
        if (hungerSlider != null) hungerSlider.gameObject.SetActive(visible);
        if (hungerText != null) hungerText.gameObject.SetActive(visible);
    }

    public void ShowAll()
    {
        SetPatienceVisibility(true);
        SetHungerVisibility(true);
    }

    public void HideAll()
    {
        SetPatienceVisibility(false);
        SetHungerVisibility(false);
    }

    public void UpdateImmediately()
    {
        lastPatienceValue = -1f;
        lastHungerValue = -1f;
        UpdateUI();
    }

    #endregion
}