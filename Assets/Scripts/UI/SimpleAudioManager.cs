using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Фоновая музыка")]
    public AudioClip mainMenuMusic;
    public AudioClip gameplayMusic;

    [Header("Звуки эффектов")]
    public AudioClip buttonClickSound;

    // Свойства для доступа к громкости
    public float MusicVolume { get; private set; } = 0.5f;
    public float SFXVolume { get; private set; } = 0.8f;
    public float EffectsVolume { get; private set; } = 0.8f; // НОВЫЙ: громкость игровых эффектов

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioSource effectsSource; // НОВЫЙ: отдельный источник для эффектов

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            SetupAudioSources();

            // Загружаем сохраненные настройки
            LoadVolumeSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        PlayMusicForCurrentScene();
    }

    private void SetupAudioSources()
    {
        // Источник для музыки
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = MusicVolume;

        // Источник для звуков интерфейса (UI)
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.volume = SFXVolume;

        // НОВЫЙ: Источник для игровых эффектов (ресурсы, здания и т.д.)
        effectsSource = gameObject.AddComponent<AudioSource>();
        effectsSource.playOnAwake = false;
        effectsSource.volume = EffectsVolume;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForCurrentScene();
    }

    private void PlayMusicForCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        AudioClip musicToPlay = GetMusicForScene(sceneName);
        PlayMusic(musicToPlay);
    }

    private AudioClip GetMusicForScene(string sceneName)
    {
        if (sceneName.Contains("Menu") || sceneName.Contains("Main"))
            return mainMenuMusic;

        if (sceneName.Contains("Game") || sceneName.Contains("Level"))
            return gameplayMusic;

        // По умолчанию - музыка для игры
        return gameplayMusic;
    }

    // === ПУБЛИЧНЫЕ МЕТОДЫ ===

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;

        // Если уже играет эта музыка - ничего не делаем
        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.volume = MusicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    // Звуки интерфейса (UI)
    public void PlayButtonClick()
    {
        if (buttonClickSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(buttonClickSound, SFXVolume);
        }
    }

    // === НОВЫЕ МЕТОДЫ ДЛЯ ИГРОВЫХ ЭФФЕКТОВ ===

    public void PlayEffect(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip != null && effectsSource != null)
        {
            effectsSource.PlayOneShot(clip, EffectsVolume * volumeMultiplier);
        }
    }

    public void PlayEffectAtPosition(AudioClip clip, Vector3 position, float volumeMultiplier = 1f)
    {
        if (clip == null) return;

        // Создаем временный объект для 3D звука
        GameObject soundObject = new GameObject("TempEffectSound");
        soundObject.transform.position = position;

        AudioSource tempSource = soundObject.AddComponent<AudioSource>();
        tempSource.clip = clip;
        tempSource.volume = EffectsVolume * volumeMultiplier;
        tempSource.spatialBlend = 1f; // Полностью 3D звук
        tempSource.minDistance = 5f;
        tempSource.maxDistance = 50f;
        tempSource.rolloffMode = AudioRolloffMode.Logarithmic;

        tempSource.Play();
        Destroy(soundObject, clip.length + 0.1f);
    }

    // === МЕТОДЫ ДЛЯ НАСТРОЙКИ ГРОМКОСТИ ===

    public void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = MusicVolume;
        }
        SaveVolumeSettings();
    }

    public void SetSFXVolume(float volume)
    {
        SFXVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = SFXVolume;
        }

        // Проигрываем тестовый звук кнопки
        if (SFXVolume > 0.05f)
        {
            PlayButtonClick();
        }

        SaveVolumeSettings();
    }

    // НОВЫЙ: Установка громкости игровых эффектов
    public void SetEffectsVolume(float volume)
    {
        EffectsVolume = Mathf.Clamp01(volume);
        if (effectsSource != null)
        {
            effectsSource.volume = EffectsVolume;
        }

        // Можно проиграть тестовый звук эффекта
        // PlayEffect(testEffectSound);

        SaveVolumeSettings();
    }

    // === СОХРАНЕНИЕ И ЗАГРУЗКА НАСТРОЕК ===

    private void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat("MusicVolume", MusicVolume);
        PlayerPrefs.SetFloat("SFXVolume", SFXVolume);
        PlayerPrefs.SetFloat("EffectsVolume", EffectsVolume); // НОВОЕ
        PlayerPrefs.Save();

        Debug.Log($"Настройки сохранены: Музыка={MusicVolume}, UI={SFXVolume}, Эффекты={EffectsVolume}");
    }

    private void LoadVolumeSettings()
    {
        MusicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        SFXVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        EffectsVolume = PlayerPrefs.GetFloat("EffectsVolume", 0.8f); // НОВОЕ

        Debug.Log($"Настройки загружены: Музыка={MusicVolume}, UI={SFXVolume}, Эффекты={EffectsVolume}");

        // Применяем загруженные настройки
        if (musicSource != null) musicSource.volume = MusicVolume;
        if (sfxSource != null) sfxSource.volume = SFXVolume;
        if (effectsSource != null) effectsSource.volume = EffectsVolume;
    }

    // === ДОПОЛНИТЕЛЬНЫЕ МЕТОДЫ ===

    // Для совместимости со старым кодом
    public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
    {
        // Решаем что это за звук - UI или эффект?
        // По умолчанию считаем UI звуком
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, SFXVolume * volumeMultiplier);
        }
    }

    public void ToggleMute()
    {
        if (musicSource != null) musicSource.mute = !musicSource.mute;
        if (sfxSource != null) sfxSource.mute = !sfxSource.mute;
        if (effectsSource != null) effectsSource.mute = !effectsSource.mute;
    }

    public bool IsMuted()
    {
        return musicSource != null && musicSource.mute;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}