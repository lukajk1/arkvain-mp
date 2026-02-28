using UnityEngine;
using System.IO;

/// <summary>
/// Window mode options
/// </summary>
public enum WindowMode
{
    Windowed = 0,
    Borderless = 1,
    Fullscreen = 2
}

/// <summary>
/// Serializable struct containing all game settings
/// </summary>
[System.Serializable]
public struct GameSettingsData
{
    // Audio
    public float masterVolume;
    public float musicVolume;
    public float sfxVolume;

    // Input
    public float mouseSensitivity;
    public bool invertY;
    public int mouseDPI;
    public float cmPer360;

    // Graphics
    public bool vsyncEnabled;
    public int targetFrameRate;
    public WindowMode windowMode;
    public int resolutionWidth;
    public int resolutionHeight;

    /// <summary>
    /// Returns default settings. In the case that the json file is not found, values populate from here.
    /// </summary>
    public static GameSettingsData GetDefaults()
    {
        return new GameSettingsData
        {
            masterVolume = 0.5f,
            musicVolume = 1.0f,
            sfxVolume = 1.0f,

            mouseSensitivity = 1.0f,
            invertY = false,
            mouseDPI = 800,
            cmPer360 = 35f,

            vsyncEnabled = true,
            targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value,
            windowMode = WindowMode.Borderless,
            resolutionWidth = Screen.currentResolution.width,
            resolutionHeight = Screen.currentResolution.height
        };
    }
}

[CreateAssetMenu(fileName = "GameSettings", menuName = "Settings/Game Settings")]
public class GameSettings : ScriptableObject
{
    private const string SETTINGS_FILENAME = "gamesettings.json";

    [HideInInspector] public GameSettingsData data = GameSettingsData.GetDefaults();

    private static GameSettings _instance;
    public static GameSettings Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GameSettings>("GameSettings");
                if (_instance == null)
                {
                    Debug.LogError("GameSettings asset not found in Resources folder! Create one at Assets/Resources/GameSettings.asset");
                }
            }
            return _instance;
        }
    }

    private static string SettingsFilePath => Path.Combine(Application.persistentDataPath, SETTINGS_FILENAME);

    public static void Initialize()
    {
        if (Instance != null)
        {
            Instance.LoadFromFile();
            Instance.ApplySettings();
        }
        else
        {
            Debug.LogError("GameSettings Instance is null during Initialize!");
        }
    }
    /// <summary>
    /// Load settings from JSON file in persistent data path
    /// </summary>
    public void LoadFromFile()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                data = JsonUtility.FromJson<GameSettingsData>(json);
                Debug.Log($"Settings loaded from {SettingsFilePath}");
            }
            else
            {
                Debug.Log("No settings file found, using defaults");
                ResetToDefaults();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load settings: {e.Message}");
            ResetToDefaults();
        }
    }

    /// <summary>
    /// Save settings to JSON file in persistent data path
    /// </summary>
    public void SaveToFile()
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SettingsFilePath, json);
            Debug.Log($"Settings saved to {SettingsFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save settings: {e.Message}");
        }
    }

    /// <summary>
    /// Apply settings to the game (graphics, audio, etc.)
    /// </summary>
    public void ApplySettings()
    {
        // Apply graphics settings
        QualitySettings.vSyncCount = data.vsyncEnabled ? 1 : 0;

        // Only set target frame rate if VSync is disabled
        // (VSync overrides targetFrameRate and locks to monitor refresh rate)
        if (!data.vsyncEnabled)
        {
            Application.targetFrameRate = data.targetFrameRate;
        }
        else
        {
            Application.targetFrameRate = -1; // Let VSync control frame rate
        }

        Debug.Log($"Applied settings - VSync: {data.vsyncEnabled}, TargetFPS: {Application.targetFrameRate}, QualityVSync: {QualitySettings.vSyncCount}");

        // Apply resolution (skip in editor to avoid issues)
        #if !UNITY_EDITOR
        Screen.SetResolution(data.resolutionWidth, data.resolutionHeight, Screen.fullScreenMode);
        #endif

        // Apply window mode
        switch (data.windowMode)
        {
            case WindowMode.Windowed:
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;
            case WindowMode.Borderless:
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                break;
            case WindowMode.Fullscreen:
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;
        }

        // Apply audio settings via AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ApplyVolumeSettings();
        }

        // Apply input settings to ClientGame static fields
        PersistentClient.playerDPI = data.mouseDPI;
        PersistentClient.cm360 = data.cmPer360;
    }

    /// <summary>
    /// Reset all settings to defaults
    /// </summary>
    public void ResetToDefaults()
    {
        data = GameSettingsData.GetDefaults();
        SaveToFile();
        ApplySettings();
    }
}
