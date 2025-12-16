using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject settingsPanel;
    public TextMeshProUGUI titleText;
    public Button startButton;
    public Button continueButton;
    public Button settingsButton;
    public Button exitButton;

    [Header("Confirmation Dialog")]
    public GameObject confirmationPanel;
    public Button yesButton;
    public Button noButton;

    [Header("Settings Panel Close Button")]
    public Button settingsCloseButton;

    [Header("Background")]
    public Image backgroundImage;
    public Sprite[] backgroundSprites;

    private LanguageManager languageManager;

    void Start()
    {
        // Получаем LanguageManager (если есть)
        languageManager = FindFirstObjectByType<LanguageManager>();

        // Настраиваем кнопки
        SetupButtons();

        // Обновляем язык
        UpdateLanguage();

        // Настраиваем фон
        SetupBackground();

        // Скрываем панели при старте
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // Подписываемся на события смены языка
        if (languageManager != null)
        {
            languageManager.OnLanguageChanged += UpdateLanguage;
        }
    }

    void SetupButtons()
    {
        // Основные кнопки меню
        startButton.onClick.AddListener(ShowConfirmationDialog);
        continueButton.onClick.AddListener(ContinueGame);
        settingsButton.onClick.AddListener(OpenSettings);
        exitButton.onClick.AddListener(ExitGame);

        // Кнопки подтверждения
        yesButton.onClick.AddListener(StartNewGame);
        noButton.onClick.AddListener(HideConfirmationDialog);

        // Кнопка закрытия настроек
        if (settingsCloseButton != null)
        {
            settingsCloseButton.onClick.AddListener(CloseSettings);
        }

        continueButton.interactable = CheckForSavedGame();
    }

    void ShowConfirmationDialog()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(true);
        }
    }

    void HideConfirmationDialog()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }
    }

    void StartNewGame()
    {
        PlayerPrefs.DeleteKey("GameProgress");
        PlayerPrefs.Save();
        HideConfirmationDialog();
        SceneManager.LoadScene("GameScene");
    }

    void SetupBackground()
    {
        if (backgroundSprites != null && backgroundSprites.Length > 0 && backgroundImage != null)
        {
            int bgIndex = PlayerPrefs.GetInt("SelectedBackground", 0);
            if (bgIndex < backgroundSprites.Length)
            {
                backgroundImage.sprite = backgroundSprites[bgIndex];
            }
        }
    }

    void UpdateLanguage()
    {
        if (languageManager == null) return;

        titleText.text = languageManager.GetLocalizedValue("game_title");
        startButton.GetComponentInChildren<TextMeshProUGUI>().text = languageManager.GetLocalizedValue("start_game");
        continueButton.GetComponentInChildren<TextMeshProUGUI>().text = languageManager.GetLocalizedValue("continue_game");
        settingsButton.GetComponentInChildren<TextMeshProUGUI>().text = languageManager.GetLocalizedValue("settings");
        exitButton.GetComponentInChildren<TextMeshProUGUI>().text = languageManager.GetLocalizedValue("exit_game");
    }

    void ContinueGame()
    {
        if (CheckForSavedGame())
        {
            SceneManager.LoadScene("GameScene");
        }
        else
        {
            Debug.LogWarning("Сохранений не найдено!");
        }
    }

    void OpenSettings()
    {
        if (settingsPanel == null) return;

        // Активируем панель настроек
        settingsPanel.SetActive(true);

        // Анимация открытия
        StartCoroutine(OpenSettingsWithAnimation());
    }

    IEnumerator OpenSettingsWithAnimation()
    {
        CanvasGroup canvasGroup = settingsPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            float duration = 0.15f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel == null) return;

        // Анимация закрытия
        StartCoroutine(CloseSettingsWithAnimation());
    }

    IEnumerator CloseSettingsWithAnimation()
    {
        CanvasGroup canvasGroup = settingsPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            float duration = 0.15f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                yield return null;
            }
        }

        settingsPanel.SetActive(false);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    bool CheckForSavedGame()
    {
        return PlayerPrefs.HasKey("GameProgress");
    }

    void OnDestroy()
    {
        if (languageManager != null)
        {
            languageManager.OnLanguageChanged -= UpdateLanguage;
        }
    }
}