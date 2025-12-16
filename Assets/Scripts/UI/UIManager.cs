using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("Меню")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject buildMenuPanel;
    [SerializeField] private GameObject distributionMenuPanel;
    [SerializeField] private GameObject boostersMenuPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Кнопки")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button buildButton;
    [SerializeField] private Button distributionButton;
    [SerializeField] private Button boostersButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button settingsCloseButton;
    [SerializeField] private Button[] buildButtons;

    [Header("Другие UI элементы")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private Text notificationText;

    public static UIManager Instance { get; private set; }

    private GameManager gameManager;

    void Start()
    {
        Instance = this;

        gameManager = GameManager.Instance;
        SetupButtonListeners();
        InitializeUI();

        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }

    private void InitializeUI()
    {
        // Закрываем все меню при старте
        CloseAllMenus();

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    private void SetupButtonListeners()
    {
        Debug.Log("Настройка кнопок UIManager...");

        // Основные кнопки - КАЖДОЙ СВОЙ МЕТОД!
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(TogglePauseMenu);
            Debug.Log($"Кнопка Pause настроена: {pauseButton.name}");
        }

        if (buildButton != null)
        {
            buildButton.onClick.AddListener(ToggleBuildMenu);
            Debug.Log($"Кнопка Build настроена: {buildButton.name}");
        }

        if (distributionButton != null)
        {
            distributionButton.onClick.AddListener(ToggleDistributionMenu); // БЫЛО ToggleBuildMenu
            Debug.Log($"Кнопка Distribution настроена: {distributionButton.name}");
        }

        if (boostersButton != null)
        {
            boostersButton.onClick.AddListener(ToggleBoostersMenu);
            Debug.Log($"Кнопка Boosters настроена: {boostersButton.name}");
        }

        // Кнопки меню паузы
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(OnResumeButtonClick);
            Debug.Log($"Кнопка Resume настроена");
        }

        if (newGameButton != null)
        {
            newGameButton.onClick.AddListener(OnNewGameButtonClick);
            Debug.Log($"Кнопка New Game настроена");
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitButtonClick);
            Debug.Log($"Кнопка Exit настроена");
        }

        // Кнопка настроек (в меню паузы)
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsButtonClick);
            Debug.Log($"Кнопка Settings настроена");
        }

        // Кнопка закрытия настроек
        if (settingsCloseButton != null)
        {
            settingsCloseButton.onClick.AddListener(OnSettingsCloseClick);
            Debug.Log($"Кнопка Settings Close настроена");
        }

        // Кнопки строительства
        if (buildButtons != null)
        {
            for (int i = 0; i < buildButtons.Length; i++)
            {
                if (buildButtons[i] != null)
                {
                    int index = i;
                    buildButtons[i].onClick.AddListener(() => OnBuildItemClick(index));
                    Debug.Log($"Кнопка строительства {i} настроена: {buildButtons[i].name}");
                }
            }
        }

        Debug.Log("Настройка кнопок завершена");
    }

    void Update()
    {
        // Обработка клавиши Escape
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            OnEscapePressed();
        }
    }

    private void OnEscapePressed()
    {
        Debug.Log("Нажата клавиша Escape");

        // Если открыта панель настроек - закрываем только её
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            Debug.Log("Закрываем панель настроек");
            CloseSettingsPanel();
            return;
        }

        // Если открыто меню паузы - закрываем всё
        if (pauseMenuPanel != null && pauseMenuPanel.activeSelf)
        {
            Debug.Log("Закрываем меню паузы");
            CloseAllMenus();
            return;
        }

        // Если открыто другое меню - закрываем его
        if (IsAnyMenuOpen())
        {
            Debug.Log("Закрываем открытое меню");
            CloseAllMenus();
            return;
        }

        // Если ничего не открыто - открываем меню паузы
        Debug.Log("Открываем меню паузы");
        ShowPauseMenu();
    }

    #region Управление меню

    public void TogglePauseMenu()
    {
        Debug.Log($"TogglePauseMenu вызван. Активность до: {pauseMenuPanel?.activeSelf}");

        if (pauseMenuPanel == null)
        {
            Debug.LogError("PauseMenuPanel не назначена!");
            return;
        }

        bool shouldOpen = !pauseMenuPanel.activeSelf;

        // Закрываем все другие меню (кроме настроек)
        CloseOtherMenusExceptSettings();

        if (shouldOpen)
        {
            pauseMenuPanel.SetActive(true);
            if (gameManager != null)
                gameManager.SetGamePaused(true);

            // Закрываем настройки если они открыты
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                settingsPanel.SetActive(false);
            }

            Debug.Log("Меню паузы открыто");
        }
        else
        {
            pauseMenuPanel.SetActive(false);
            if (gameManager != null)
                gameManager.SetGamePaused(false);

            Debug.Log("Меню паузы закрыто");
        }
    }

    public void ToggleBuildMenu()
    {
        Debug.Log($"ToggleBuildMenu вызван. Активность до: {buildMenuPanel?.activeSelf}");

        if (buildMenuPanel == null)
        {
            Debug.LogError("BuildMenuPanel не назначена!");
            return;
        }

        bool shouldOpen = !buildMenuPanel.activeSelf;

        // Закрываем все другие меню
        CloseAllMenus();

        if (shouldOpen)
        {
            buildMenuPanel.SetActive(true);
            Debug.Log("Меню строительства открыто");
        }
        else
        {
            Debug.Log("Меню строительства закрыто");
        }
    }

    public void ToggleDistributionMenu()
    {
        Debug.Log($"ToggleDistributionMenu вызван. Активность до: {distributionMenuPanel?.activeSelf}");

        if (distributionMenuPanel == null)
        {
            Debug.LogError("DistributionMenuPanel не назначена!");
            return;
        }

        bool shouldOpen = !distributionMenuPanel.activeSelf;

        // Закрываем все другие меню
        CloseAllMenus();

        if (shouldOpen)
        {
            distributionMenuPanel.SetActive(true);
            Debug.Log("Меню распределения открыто");
        }
        else
        {
            Debug.Log("Меню распределения закрыто");
        }
    }

    public void ToggleBoostersMenu()
    {
        Debug.Log($"ToggleBoostersMenu вызван. Активность до: {boostersMenuPanel?.activeSelf}");

        if (boostersMenuPanel == null)
        {
            Debug.LogError("BoostersMenuPanel не назначена!");
            return;
        }

        bool shouldOpen = !boostersMenuPanel.activeSelf;

        // Закрываем все другие меню
        CloseAllMenus();

        if (shouldOpen)
        {
            boostersMenuPanel.SetActive(true);
            Debug.Log("Меню бустеров открыто");
        }
        else
        {
            Debug.Log("Меню бустеров закрыто");
        }
    }

    private void CloseOtherMenusExceptSettings()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (buildMenuPanel != null)
            buildMenuPanel.SetActive(false);

        if (distributionMenuPanel != null)
            distributionMenuPanel.SetActive(false);

        if (boostersMenuPanel != null)
            boostersMenuPanel.SetActive(false);

        // НЕ закрываем settingsPanel!
    }

    public void ShowPauseMenu()
    {
        if (pauseMenuPanel == null)
        {
            Debug.LogError("PauseMenuPanel не назначена!");
            return;
        }

        // Закрываем все меню кроме настроек
        CloseOtherMenusExceptSettings();

        pauseMenuPanel.SetActive(true);
        if (gameManager != null)
            gameManager.SetGamePaused(true);

        Debug.Log("Меню паузы показано");
    }

    public void CloseAllMenus()
    {
        Debug.Log("Закрываем все меню");

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
            Debug.Log("Меню паузы закрыто");
        }

        if (buildMenuPanel != null)
        {
            buildMenuPanel.SetActive(false);
            Debug.Log("Меню строительства закрыто");
        }

        if (distributionMenuPanel != null)
        {
            distributionMenuPanel.SetActive(false);
            Debug.Log("Меню распределения закрыто");
        }

        if (boostersMenuPanel != null)
        {
            boostersMenuPanel.SetActive(false);
            Debug.Log("Меню бустеров закрыто");
        }

        // Закрываем и настройки
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            Debug.Log("Настройки закрыты");
        }

        // Снимаем паузу
        if (gameManager != null && gameManager.IsGamePaused())
        {
            gameManager.SetGamePaused(false);
            Debug.Log("Игра возобновлена");
        }
    }

    #endregion

    #region Обработка кнопок

    private void OnResumeButtonClick()
    {
        Debug.Log("Нажата кнопка Resume");
        CloseAllMenus();
    }

    private void OnNewGameButtonClick()
    {
        Debug.Log("Нажата кнопка New Game");
        if (gameManager != null)
            gameManager.StartNewGame();
    }

    private void OnExitButtonClick()
    {
        Debug.Log("Нажата кнопка Exit");
        if (gameManager != null)
            gameManager.ExitToMainMenu();
    }

    private void OnSettingsButtonClick()
    {
        Debug.Log("Нажата кнопка Settings");

        if (settingsPanel == null)
        {
            Debug.LogError("SettingsPanel не назначена!");
            return;
        }

        // Убеждаемся что меню паузы открыто
        if (pauseMenuPanel == null || !pauseMenuPanel.activeSelf)
        {
            Debug.LogWarning("Меню паузы не открыто! Сначала откройте паузу.");
            return;
        }

        // Открываем панель настроек ПОВЕРХ меню паузы
        settingsPanel.SetActive(true);
        Debug.Log("Панель настроек открыта поверх меню паузы");
    }

    private void OnSettingsCloseClick()
    {
        Debug.Log("Нажата кнопка закрытия настроек");
        CloseSettingsPanel();
    }

    private void CloseSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            Debug.Log("Панель настроек закрыта, меню паузы остается открытым");
        }
    }

    [Header("Building Manager")]
    [SerializeField] private BuildingManager buildingManager;

    private void OnBuildItemClick(int index)
    {
        Debug.Log($"Нажата кнопка строительства {index}");
        CloseAllMenus();

        if (buildingManager != null)
        {
            switch (index)
            {
                case 0:
                    buildingManager.StartBuildingHouse();
                    break;
                case 1:
                    buildingManager.StartBuildingWarmleafStation();
                    break;
                case 2:
                    buildingManager.StartBuildingResearchStation();
                    break;
                case 3:
                    buildingManager.StartBuildingEnricher();
                    break;
                default:
                    Debug.LogError($"Неизвестный индекс здания: {index}");
                    break;
            }
        }
    }

    #endregion

    #region Уведомления

    public void ShowNotification(string message, float duration = 2f)
    {
        if (notificationPanel != null && notificationText != null)
        {
            notificationText.text = message;
            notificationPanel.SetActive(true);

            CancelInvoke(nameof(HideNotification));
            Invoke(nameof(HideNotification), duration);
        }

        Debug.Log($"Уведомление: {message}");
    }

    private void HideNotification()
    {
        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }

    #endregion

    #region Публичные методы для других скриптов

    public bool IsAnyMenuOpen()
    {
        bool isOpen = (pauseMenuPanel != null && pauseMenuPanel.activeSelf) ||
               (buildMenuPanel != null && buildMenuPanel.activeSelf) ||
               (distributionMenuPanel != null && distributionMenuPanel.activeSelf) ||
               (boostersMenuPanel != null && boostersMenuPanel.activeSelf) ||
               (settingsPanel != null && settingsPanel.activeSelf);

        Debug.Log($"IsAnyMenuOpen: {isOpen}");
        return isOpen;
    }

    #endregion
}