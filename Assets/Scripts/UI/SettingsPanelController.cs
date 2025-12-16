using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelController : MonoBehaviour
{
    [Header("Слайдеры громкости")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider effectsSlider;

    [Header("Тексты процентов (опционально)")]
    [SerializeField] private TMPro.TextMeshProUGUI musicPercentText;
    [SerializeField] private TMPro.TextMeshProUGUI sfxPercentText;
    [SerializeField] private TMPro.TextMeshProUGUI effectsPercentText;

    void Start()
    {
        InitializeSliders();
    }

    void OnEnable()
    {
        LoadCurrentSettings();
    }

    void InitializeSliders()
    {
        if (musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
        }

        if (effectsSlider != null)
        {
            effectsSlider.minValue = 0f;
            effectsSlider.maxValue = 1f;
            effectsSlider.onValueChanged.AddListener(OnEffectsSliderChanged);
        }
    }

    void LoadCurrentSettings()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("AudioManager не найден!");
            return;
        }

        if (musicSlider != null)
        {
            musicSlider.value = AudioManager.Instance.MusicVolume;
            UpdateMusicText();
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = AudioManager.Instance.SFXVolume;
            UpdateSFXText();
        }

        if (effectsSlider != null)
        {
            effectsSlider.value = AudioManager.Instance.EffectsVolume;
            UpdateEffectsText();
        }
    }

    void OnMusicSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }
        UpdateMusicText();
    }

    void OnSFXSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
        }
        UpdateSFXText();
    }

    void OnEffectsSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetEffectsVolume(value);
        }
        UpdateEffectsText();

        // Проигрываем тестовый звук при изменении
        PlayTestEffectSound(value);
    }

    private void PlayTestEffectSound(float volumeValue)
    {
        // Можно добавить тестовый звук для эффектов
        Debug.Log($"Громкость эффектов изменена: {volumeValue * 100:F0}%");

        // Пример: проиграть тестовый звук если есть
        // if (AudioManager.Instance != null && volumeValue > 0.1f)
        // {
        //     AudioManager.Instance.PlayEffect(testSound, 1f);
        // }
    }

    void UpdateMusicText()
    {
        if (musicPercentText != null && musicSlider != null)
        {
            int percent = Mathf.RoundToInt(musicSlider.value * 100);
            musicPercentText.text = $"{percent}%";
        }
    }

    void UpdateSFXText()
    {
        if (sfxPercentText != null && sfxSlider != null)
        {
            int percent = Mathf.RoundToInt(sfxSlider.value * 100);
            sfxPercentText.text = $"{percent}%";
        }
    }

    void UpdateEffectsText()
    {
        if (effectsPercentText != null && effectsSlider != null)
        {
            int percent = Mathf.RoundToInt(effectsSlider.value * 100);
            effectsPercentText.text = $"{percent}%";
        }
    }

    void OnDestroy()
    {
        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(OnSFXSliderChanged);

        if (effectsSlider != null)
            effectsSlider.onValueChanged.RemoveListener(OnEffectsSliderChanged);
    }
}