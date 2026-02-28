using UnityEngine;
using System.IO;

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
    public int graphicsQuality;
    public bool vsyncEnabled;
    public int targetFrameRate;

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

            graphicsQuality = 2,
            vsyncEnabled = true,
            targetFrameRate = 144
        };
    }
}

[CreateAssetMenu(fileName = "GameSettings", menuName = "Settings/Game Settings")]
public class GameSettings : ScriptableObject
{
    private const string SETTINGS_FILENAME = "gamesettings.json";

    [HideInInspector] public GameSettingsData data;

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

    /// <summary>
    /// Called automatically before scene loads to initialize settings from file
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Instance != null)
        {
            Instance.LoadFromFile();
            Instance.ApplySettings();
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
        QualitySettings.SetQualityLevel(data.graphicsQuality);
        QualitySettings.vSyncCount = data.vsyncEnabled ? 1 : 0;
        Application.targetFrameRate = data.targetFrameRate;

        // Apply audio settings via AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ApplyVolumeSettings();
        }

        // Apply input settings to ClientGame static fields
        ClientGame.playerDPI = data.mouseDPI;
        ClientGame.targetCm360 = data.cmPer360;
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
