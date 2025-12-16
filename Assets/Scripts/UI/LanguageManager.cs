using UnityEngine;
using System.Collections.Generic;
using System;

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance { get; private set; }

    public enum Language { Russian, English }

    [SerializeField] private Language currentLanguage = Language.Russian;

    private Dictionary<string, string> russianLocalization;
    private Dictionary<string, string> englishLocalization;

    public event Action OnLanguageChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLocalization();
            LoadLanguage();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeLocalization()
    {
        // Русская локализация
        russianLocalization = new Dictionary<string, string>
        {
            {"game_title", "ЭКЕВОВ"},
            {"start_game", "Начать игру"},
            {"continue_game", "Продолжить игру"},
            {"settings", "Настройки"},
            {"exit_game", "Выход из игры"},
            {"language", "Язык"},
            {"back", "Назад"},
            {"resolution", "Разрешение"},
            {"fullscreen", "Полный экран"},
            {"volume", "Громкость"}
        };

        // Английская локализация
        englishLocalization = new Dictionary<string, string>
        {
            {"game_title", "EKEVOV"},
            {"start_game", "Start Game"},
            {"continue_game", "Continue Game"},
            {"settings", "Settings"},
            {"exit_game", "Exit Game"},
            {"language", "Language"},
            {"back", "Back"},
            {"resolution", "Resolution"},
            {"fullscreen", "Fullscreen"},
            {"volume", "Volume"}
        };
    }

    public void SetLanguage(Language language)
    {
        currentLanguage = language;
        PlayerPrefs.SetInt("Language", (int)language);
        PlayerPrefs.Save();

        OnLanguageChanged?.Invoke();
    }

    public Language GetCurrentLanguage()
    {
        return currentLanguage;
    }

    public string GetLocalizedValue(string key)
    {
        Dictionary<string, string> currentDictionary = currentLanguage == Language.Russian ?
            russianLocalization : englishLocalization;

        if (currentDictionary.ContainsKey(key))
        {
            return currentDictionary[key];
        }

        return $"#{key}#"; // Возвращаем ключ, если перевод не найден
    }

    void LoadLanguage()
    {
        if (PlayerPrefs.HasKey("Language"))
        {
            currentLanguage = (Language)PlayerPrefs.GetInt("Language");
        }
    }
}