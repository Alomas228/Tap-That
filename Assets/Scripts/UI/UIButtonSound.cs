using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Звуки")]
    [SerializeField] private AudioClip clickSound; // Если null, будет использовать AudioManager
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip hoverExitSound; // Опционально: звук при уходе курсора

    [Header("Громкость")]
    [Range(0f, 1f)][SerializeField] private float hoverVolume = 0.5f;
    [Range(0f, 1f)][SerializeField] private float clickVolume = 1f;

    [Header("Настройки")]
    [SerializeField] private bool playHoverSound = true;
    [SerializeField] private bool playClickSound = true;
    [SerializeField] private float hoverCooldown = 0.1f; // Задержка между звуками наведения

    private Button button;
    private float lastHoverTime;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    void OnEnable()
    {
        if (button != null && playClickSound)
        {
            button.onClick.RemoveListener(OnButtonClick);
            button.onClick.AddListener(OnButtonClick);
        }
    }

    void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }

    // Наведение курсора
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!playHoverSound || hoverSound == null) return;

        // Проверяем кд, чтобы не спамить звуками при быстром перемещении
        if (Time.time - lastHoverTime < hoverCooldown) return;

        PlayHoverSound();
        lastHoverTime = Time.time;
    }

    // Уход курсора (опционально)
    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverExitSound != null)
        {
            PlaySound(hoverExitSound, hoverVolume * 0.5f); // Тише чем hover
        }
    }

    void OnButtonClick()
    {
        if (!playClickSound) return;

        if (clickSound != null)
        {
            PlaySound(clickSound, clickVolume);
        }
        else
        {
            // Используем стандартный звук из AudioManager
            AudioManager.Instance?.PlayButtonClick();
        }
    }

    void PlayHoverSound()
    {
        PlaySound(hoverSound, hoverVolume);
    }

    void PlaySound(AudioClip clip, float volume)
    {
        if (clip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip, volume);
        }
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }
}