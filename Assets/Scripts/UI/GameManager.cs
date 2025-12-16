using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Настройки игры")]
    
    [SerializeField] private float gameSpeed = 1.0f;

    [Header("Системные ссылки")]
    [SerializeField] private UIManager uiManager;

    [Header("Менеджеры")]
    [SerializeField] private ResourceManager resourceManager;

    // Состояние игры
    private bool isGamePaused = false;
    private bool isGameOver = false;

    // Input System
#if ENABLE_INPUT_SYSTEM
    private Keyboard currentKeyboard;
#endif

    void Awake()
    {
        // Инициализация синглтона
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Автоматическое нахождение UIManager если не назначен
        if (uiManager == null)
            uiManager = FindAnyObjectByType<UIManager>();

        // Инициализация Input System
        InitializeInputSystem();

        Debug.Log("GameManager инициализирован");
    }

    void Start()
    {
        InitializeGame();
    }

    void Update()
    {
        HandleGlobalInput();
    }

    private void InitializeInputSystem()
    {
#if ENABLE_INPUT_SYSTEM
        currentKeyboard = Keyboard.current;
#endif
    }

    private void InitializeGame()
    {
        Debug.Log("Инициализация игры...");

        // Устанавливаем начальную скорость игры
        Time.timeScale = gameSpeed;
    }

    private void HandleGlobalInput()
    {
        bool escapePressed = false;
        bool f5Pressed = false;
        bool f9Pressed = false;

        // Обработка ввода с поддержкой обеих Input System
#if ENABLE_INPUT_SYSTEM
        if (currentKeyboard != null)
        {
            escapePressed = currentKeyboard.escapeKey.wasPressedThisFrame;
            f5Pressed = currentKeyboard.f5Key.wasPressedThisFrame;
            f9Pressed = currentKeyboard.f9Key.wasPressedThisFrame;
        }
        else
#endif
        {
            // Fallback на старую систему ввода
#if !ENABLE_INPUT_SYSTEM
            escapePressed = Input.GetKeyDown(KeyCode.Escape);
            f5Pressed = Input.GetKeyDown(KeyCode.F5);
            f9Pressed = Input.GetKeyDown(KeyCode.F9);
#endif
        }

        // Обработка нажатий
        if (escapePressed)
        {
            TogglePause();
        }

        if (f5Pressed)
        {
            SaveGame();
        }

        if (f9Pressed)
        {
            LoadGame();
        }
    }

    #region Управление состоянием игры

    public void SetGamePaused(bool paused)
    {
        isGamePaused = paused;
        Time.timeScale = paused ? 0f : gameSpeed;

        Debug.Log($"Игра {(paused ? "приостановлена" : "возобновлена")}");
    }

    public void TogglePause()
    {
        SetGamePaused(!isGamePaused);

        // Уведомляем UI если есть
        if (uiManager != null)
        {
            if (isGamePaused)
                uiManager.ShowPauseMenu();
            else
                uiManager.CloseAllMenus();
        }
    }

    public void SetGameOver(bool gameOver)
    {
        isGameOver = gameOver;

        if (gameOver)
        {
            Time.timeScale = 0f;
            Debug.Log("Игра окончена");

            // Здесь можно показать экран Game Over
        }
    }

    public void SetGameSpeed(float speed)
    {
        gameSpeed = Mathf.Clamp(speed, 0.1f, 10f);

        if (!isGamePaused)
            Time.timeScale = gameSpeed;

        Debug.Log($"Скорость игры установлена: {gameSpeed}x");
    }

    public bool IsGamePaused() => isGamePaused;
    public bool IsGameOver() => isGameOver;
    public float GetGameSpeed() => gameSpeed;

    #endregion

    #region Сохранение и загрузка

    public void SaveGame()
    {
        Debug.Log("Сохранение игры...");

        // Базовое сохранение для демонстрации
        PlayerPrefs.SetInt("GameSaved", 1);
        PlayerPrefs.Save();
        if (uiManager != null)
            uiManager.ShowPauseMenu();

        if (uiManager != null)
        {
            uiManager.ShowNotification("Игра сохранена!");
        }

        // Здесь другие менеджеры будут добавлять свои данные
    }

    public void LoadGame()
    {
        if (uiManager != null)
            uiManager.CloseAllMenus();
        if (PlayerPrefs.HasKey("GameSaved"))
        {
            Debug.Log("Загрузка игры...");

            if (uiManager != null)
            {
                uiManager.ShowNotification("Игра загружена");
            }

            // Здесь другие менеджеры будут загружать свои данные
        }
        else
        {
            Debug.Log("Сохранение не найдено");

            if (uiManager != null)
            {
                uiManager.ShowNotification("Сохранение не найдено");
            }
        }
    }

    public void StartNewGame()
    {
        Debug.Log("Начало новой игры...");

        // Сбрасываем сохранения
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        if (uiManager != null)
            uiManager.ShowNotification("Игра сохранена!");

        // Сбрасываем состояние
        isGamePaused = false;
        isGameOver = false;
        Time.timeScale = gameSpeed;

        // Перезагружаем сцену
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        Debug.Log("Новая игра начата");
    }

    public void ResetGame()
    {
        Debug.Log("Сброс игры");

        // Здесь можно уведомить все системы о сбросе
        StartNewGame();
    }

    #endregion

    #region Утилиты

    public UIManager GetUIManager() => uiManager;

    public void ExitToMainMenu()
    {
        // Сохраняем перед выходом
        SaveGame();

        // Загружаем главное меню
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        // Сохраняем перед выходом
        SaveGame();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion
}