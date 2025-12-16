using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
[RequireComponent(typeof(Slider))]
public class GradientSlider : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Slider slider;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image backgroundImage;

    [Header("Настройки градиента")]
    [SerializeField] private Color startColor = new Color(0.5686f, 0.8157f, 0.4863f); // #91D07C
    [SerializeField] private Color endColor = new Color(0.7294f, 0f, 0.0118f);        // #BA0003

    [Header("Дополнительные эффекты")]
    [SerializeField] private bool usePulseEffect = false;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseMinAlpha = 0.5f;
    [SerializeField] private float pulseMaxAlpha = 1f;

    [Header("Направление заполнения")]
    [SerializeField] private bool reverseFill = false;

    private Material gradientMaterial;
    private bool isInitialized = false;

    void Awake()
    {
        Initialize();
    }

    void OnValidate()
    {
        Initialize();
        UpdateVisuals();
    }

    void Update()
    {
        if (usePulseEffect && slider.value < 0.3f)
        {
            UpdatePulseEffect();
        }
        else if (fillImage != null)
        {
            Color color = fillImage.color;
            color.a = 1f;
            fillImage.color = color;
        }
    }

    private void Initialize()
    {
        if (isInitialized) return;

        // Получаем ссылки если не назначены
        if (slider == null)
            slider = GetComponent<Slider>();

        if (fillImage == null && slider != null && slider.fillRect != null)
            fillImage = slider.fillRect.GetComponent<Image>();

        if (backgroundImage == null)
        {
            Image[] images = GetComponentsInChildren<Image>();
            foreach (var img in images)
            {
                if (img.transform != slider.fillRect && img != fillImage)
                {
                    backgroundImage = img;
                    break;
                }
            }
        }

        // Настраиваем слайдер
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.interactable = false; // Неинтерактивный, только для отображения
        }

        isInitialized = true;
    }

    public void SetValue(float value)
    {
        if (slider == null) return;

        value = Mathf.Clamp01(value);
        slider.value = reverseFill ? 1f - value : value;
        UpdateVisuals();
    }

    public void SetColors(Color start, Color end)
    {
        startColor = start;
        endColor = end;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (fillImage == null || slider == null) return;

        // Вычисляем цвет градиента на основе текущего значения
        float normalizedValue = reverseFill ? 1f - slider.value : slider.value;
        Color gradientColor = Color.Lerp(startColor, endColor, normalizedValue);

        // Применяем цвет к fill image
        fillImage.color = gradientColor;

        // Настраиваем направление заполнения
        if (slider.direction == Slider.Direction.LeftToRight)
        {
            fillImage.transform.localScale = new Vector3(1, 1, 1);
        }

        // Настраиваем фон если есть
        if (backgroundImage != null)
        {
            Color bgColor = backgroundImage.color;
            bgColor.a = 0.2f; // Полупрозрачный фон
            backgroundImage.color = bgColor;
        }
    }

    private void UpdatePulseEffect()
    {
        if (fillImage == null) return;

        float pulseValue = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, pulseValue);

        Color color = fillImage.color;
        color.a = alpha;
        fillImage.color = color;
    }

    public void SetReverseFill(bool reverse)
    {
        reverseFill = reverse;
        UpdateVisuals();
    }

    public void EnablePulse(bool enable)
    {
        usePulseEffect = enable;
        if (!enable && fillImage != null)
        {
            Color color = fillImage.color;
            color.a = 1f;
            fillImage.color = color;
        }
    }

    public float GetValue() => slider != null ? slider.value : 0f;
}