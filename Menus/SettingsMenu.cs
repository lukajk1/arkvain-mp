using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI controller for the settings menu.
/// Reads from and writes to GameSettings ScriptableObject.
/// Can be used in any scene - main menu, pause menu, etc.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private GameObject _confirmationBoxPrefab;
    [SerializeField] private EscapeMenu _escapeMenu;

    [Header("Audio")]
    [SerializeField] private Slider _masterVolumeSlider;
    [SerializeField] private TMP_InputField _masterVolumeInputField;
    [SerializeField] private Slider _musicVolumeSlider;
    [SerializeField] private Slider _sfxVolumeSlider;

    [Header("Input")]
    [SerializeField] private Slider _mouseSensitivitySlider;
    [SerializeField] private TMP_InputField _mouseDPIInputField;
    [SerializeField] private TMP_InputField _cmPer360InputField;

    [Header("Graphics")]
    [SerializeField] private TMP_Dropdown _resolutionDropdown;
    [SerializeField] private TMP_Dropdown _windowModeDropdown;
    [SerializeField] private Toggle _vsyncToggle;
    [SerializeField] private TMP_InputField _targetFrameRateInputField;

    private Resolution[] _availableResolutions;

    [Header("Buttons")]
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _resetButton;

    private void Start()
    {
        PopulateResolutionDropdown();
        SetState(false);
    }

    public void SetState(bool value)
    {
        _canvas.gameObject.SetActive(value);

        if (value) LoadSettingsToUI();

        PersistentClient.ModifyCursorUnlockList(value, this);
        InputManager.Instance.ModifyPlayerControlsLockList(value, this);
    }

    #region onenable/disable subscriptions
    private void OnEnable()
    {
        // Subscribe to UI changes
        if (_masterVolumeSlider != null)
            _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeSliderChanged);

        if (_masterVolumeInputField != null)
            _masterVolumeInputField.onEndEdit.AddListener(OnMasterVolumeInputChanged);

        if (_musicVolumeSlider != null)
            _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        if (_sfxVolumeSlider != null)
            _sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        if (_mouseSensitivitySlider != null)
            _mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);

        if (_mouseDPIInputField != null)
            _mouseDPIInputField.onEndEdit.AddListener(OnMouseDPIInputChanged);

        if (_cmPer360InputField != null)
            _cmPer360InputField.onEndEdit.AddListener(OnCmPer360InputChanged);

        if (_resolutionDropdown != null)
            _resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        if (_windowModeDropdown != null)
            _windowModeDropdown.onValueChanged.AddListener(OnWindowModeChanged);

        if (_vsyncToggle != null)
            _vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);

        if (_targetFrameRateInputField != null)
            _targetFrameRateInputField.onEndEdit.AddListener(OnTargetFrameRateInputChanged);

        // Subscribe to buttons
        if (_saveButton != null)
            _saveButton.onClick.AddListener(OnSaveClicked);

        if (_resetButton != null)
            _resetButton.onClick.AddListener(OnResetClicked);
    }

    private void OnDisable()
    {
        // Unsubscribe from UI changes
        if (_masterVolumeSlider != null)
            _masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeSliderChanged);

        if (_masterVolumeInputField != null)
            _masterVolumeInputField.onEndEdit.RemoveListener(OnMasterVolumeInputChanged);

        if (_musicVolumeSlider != null)
            _musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);

        if (_sfxVolumeSlider != null)
            _sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);

        if (_mouseSensitivitySlider != null)
            _mouseSensitivitySlider.onValueChanged.RemoveListener(OnMouseSensitivityChanged);

        if (_mouseDPIInputField != null)
            _mouseDPIInputField.onEndEdit.RemoveListener(OnMouseDPIInputChanged);

        if (_cmPer360InputField != null)
            _cmPer360InputField.onEndEdit.RemoveListener(OnCmPer360InputChanged);

        if (_resolutionDropdown != null)
            _resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);

        if (_windowModeDropdown != null)
            _windowModeDropdown.onValueChanged.RemoveListener(OnWindowModeChanged);

        if (_vsyncToggle != null)
            _vsyncToggle.onValueChanged.RemoveListener(OnVSyncChanged);

        if (_targetFrameRateInputField != null)
            _targetFrameRateInputField.onEndEdit.RemoveListener(OnTargetFrameRateInputChanged);

        // Unsubscribe from buttons
        if (_saveButton != null)
            _saveButton.onClick.RemoveListener(OnSaveClicked);

        if (_resetButton != null)
            _resetButton.onClick.RemoveListener(OnResetClicked);
    }
    #endregion
    /// <summary>
    /// Load current settings from GameSettings into UI controls
    /// </summary>
    private void LoadSettingsToUI()
    {
        if (GameSettings.Instance == null) return;

        if (_masterVolumeSlider != null)
            _masterVolumeSlider.value = GameSettings.Instance.data.masterVolume;

        if (_masterVolumeInputField != null)
            _masterVolumeInputField.text = Mathf.RoundToInt(GameSettings.Instance.data.masterVolume * 100f).ToString();

        if (_musicVolumeSlider != null)
            _musicVolumeSlider.value = GameSettings.Instance.data.musicVolume;

        if (_sfxVolumeSlider != null)
            _sfxVolumeSlider.value = GameSettings.Instance.data.sfxVolume;

        if (_mouseSensitivitySlider != null)
            _mouseSensitivitySlider.value = GameSettings.Instance.data.mouseSensitivity;

        if (_mouseDPIInputField != null)
            _mouseDPIInputField.text = GameSettings.Instance.data.mouseDPI.ToString();

        if (_cmPer360InputField != null)
            _cmPer360InputField.text = GameSettings.Instance.data.cmPer360.ToString("F1");

        if (_resolutionDropdown != null)
        {
            // Find matching resolution in dropdown
            int currentResolutionIndex = FindResolutionIndex(
                GameSettings.Instance.data.resolutionWidth,
                GameSettings.Instance.data.resolutionHeight
            );
            if (currentResolutionIndex >= 0)
            {
                _resolutionDropdown.SetValueWithoutNotify(currentResolutionIndex);
            }
        }

        if (_windowModeDropdown != null)
            _windowModeDropdown.SetValueWithoutNotify((int)GameSettings.Instance.data.windowMode);

        if (_vsyncToggle != null)
            _vsyncToggle.SetIsOnWithoutNotify(GameSettings.Instance.data.vsyncEnabled);

        if (_targetFrameRateInputField != null)
            _targetFrameRateInputField.SetTextWithoutNotify(GameSettings.Instance.data.targetFrameRate.ToString());

        UpdateTargetFrameRateInputState();
    }

    // Audio callbacks
    private void OnMasterVolumeSliderChanged(float value)
    {
        GameSettings.Instance.data.masterVolume = value;

        // Update input field to match slider (prevent feedback loop)
        if (_masterVolumeInputField != null)
        {
            _masterVolumeInputField.SetTextWithoutNotify(Mathf.RoundToInt(value * 100f).ToString());
        }

        // Apply immediately for preview
        AudioManager.Instance?.SetMasterVolume(value);
    }

    private void OnMasterVolumeInputChanged(string text)
    {
        if (int.TryParse(text, out int volumePercent))
        {
            // Clamp to 0-100
            volumePercent = Mathf.Clamp(volumePercent, 0, 100);
            float volumeFloat = volumePercent / 100f;

            GameSettings.Instance.data.masterVolume = volumeFloat;

            // Update slider to match input (prevent feedback loop)
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.SetValueWithoutNotify(volumeFloat);
            }

            // Update input field with clamped value
            if (_masterVolumeInputField != null)
            {
                _masterVolumeInputField.text = volumePercent.ToString();
            }

            // Apply immediately for preview
            AudioManager.Instance?.SetMasterVolume(volumeFloat);
        }
        else
        {
            // Invalid input - reset to current value
            if (_masterVolumeInputField != null)
            {
                _masterVolumeInputField.text = Mathf.RoundToInt(GameSettings.Instance.data.masterVolume * 100f).ToString();
            }
        }
    }

    private void OnMusicVolumeChanged(float value)
    {
        GameSettings.Instance.data.musicVolume = value;
        AudioManager.Instance?.SetMusicVolume(value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        GameSettings.Instance.data.sfxVolume = value;
        AudioManager.Instance?.SetSFXVolume(value);
    }

    // Input callbacks
    private void OnMouseSensitivityChanged(float value)
    {
        GameSettings.Instance.data.mouseSensitivity = value;
    }

    private void OnMouseDPIInputChanged(string text)
    {
        if (int.TryParse(text, out int dpiValue))
        {
            // Clamp to reasonable values
            dpiValue = Mathf.Clamp(dpiValue, 100, 10000);
            GameSettings.Instance.data.mouseDPI = dpiValue;

            // Update input field with clamped value
            if (_mouseDPIInputField != null)
            {
                _mouseDPIInputField.text = dpiValue.ToString();
            }
        }
        else
        {
            // Invalid input - reset to current value
            if (_mouseDPIInputField != null)
            {
                _mouseDPIInputField.text = GameSettings.Instance.data.mouseDPI.ToString();
            }
        }
    }

    private void OnCmPer360InputChanged(string text)
    {
        if (float.TryParse(text, out float cmValue))
        {
            // Clamp to reasonable values
            cmValue = Mathf.Clamp(cmValue, 5f, 100f);
            GameSettings.Instance.data.cmPer360 = cmValue;

            // Update input field with clamped value
            if (_cmPer360InputField != null)
            {
                _cmPer360InputField.text = cmValue.ToString("F1");
            }
        }
        else
        {
            // Invalid input - reset to current value
            if (_cmPer360InputField != null)
            {
                _cmPer360InputField.text = GameSettings.Instance.data.cmPer360.ToString("F1");
            }
        }
    }

    // Graphics callbacks
    private void OnResolutionChanged(int value)
    {
        if (_availableResolutions != null && value >= 0 && value < _availableResolutions.Length)
        {
            Resolution resolution = _availableResolutions[value];
            GameSettings.Instance.data.resolutionWidth = resolution.width;
            GameSettings.Instance.data.resolutionHeight = resolution.height;
            GameSettings.Instance.ApplySettings();
        }
    }

    private void OnWindowModeChanged(int value)
    {
        GameSettings.Instance.data.windowMode = (WindowMode)value;
        GameSettings.Instance.ApplySettings();
    }

    private void OnVSyncChanged(bool value)
    {
        GameSettings.Instance.data.vsyncEnabled = value;
        GameSettings.Instance.ApplySettings();
        UpdateTargetFrameRateInputState();
    }

    private void OnTargetFrameRateInputChanged(string text)
    {
        if (int.TryParse(text, out int frameRate))
        {
            // Clamp to reasonable values
            frameRate = Mathf.Clamp(frameRate, 30, 500);
            GameSettings.Instance.data.targetFrameRate = frameRate;

            // Update input field with clamped value
            if (_targetFrameRateInputField != null)
            {
                _targetFrameRateInputField.text = frameRate.ToString();
            }

            GameSettings.Instance.ApplySettings();
        }
        else
        {
            // Invalid input - reset to current value
            if (_targetFrameRateInputField != null)
            {
                _targetFrameRateInputField.text = GameSettings.Instance.data.targetFrameRate.ToString();
            }
        }
    }

    /// <summary>
    /// Updates the interactable state of the target frame rate input field based on VSync setting
    /// </summary>
    private void UpdateTargetFrameRateInputState()
    {
        if (_targetFrameRateInputField != null && GameSettings.Instance != null)
        {
            _targetFrameRateInputField.interactable = !GameSettings.Instance.data.vsyncEnabled;
        }
    }

    // Button callbacks
    private void OnSaveClicked()
    {
        // in case I want to have more logic handling here prior to calling saveandclose()
        SaveAndClose();
    }

    private void SaveAndClose()
    {
        GameSettings.Instance.ApplySettings();
        GameSettings.Instance.SaveToFile();
        SetState(false);
        _escapeMenu.SetState(false);
    }

    private void OnResetClicked()
    {
        if (_confirmationBoxPrefab == null || _canvas == null)
        {
            // No confirmation box - save directly
            SaveAndClose();
            return;
        }

        GameObject confirmBoxObj = Instantiate(_confirmationBoxPrefab, _canvas.transform);
        ConfirmationBox confirmBox = confirmBoxObj.GetComponent<ConfirmationBox>();

        if (confirmBox != null)
        {
            confirmBox.Initialize(
                onConfirm: () => ResetSettings(),
                onCancel: null,
                message: "Reset settings?",
                confirmText: "Yes",
                cancelText: "Cancel"
            );
        }
        else
        {
            // Fallback if component not found
            Destroy(confirmBoxObj);
            SaveAndClose();
        }
    }

    private void ResetSettings()
    {
        GameSettings.Instance.ResetToDefaults();
        LoadSettingsToUI();
        Debug.Log("Settings reset to defaults!");
    }

    /// <summary>
    /// Populate resolution dropdown with available resolutions up to native resolution
    /// </summary>
    private void PopulateResolutionDropdown()
    {
        if (_resolutionDropdown == null) return;

        // Get all available resolutions
        Resolution[] allResolutions = Screen.resolutions;
        Resolution nativeResolution = Screen.currentResolution;

        // Filter out resolutions higher than native and remove duplicates (different refresh rates)
        System.Collections.Generic.List<Resolution> filteredResolutions = new System.Collections.Generic.List<Resolution>();
        System.Collections.Generic.HashSet<string> addedResolutions = new System.Collections.Generic.HashSet<string>();

        foreach (Resolution res in allResolutions)
        {
            // Skip resolutions higher than native
            if (res.width > nativeResolution.width || res.height > nativeResolution.height)
                continue;

            // Create unique key for width x height (ignore refresh rate)
            string resolutionKey = $"{res.width}x{res.height}";

            // Skip if already added
            if (addedResolutions.Contains(resolutionKey))
                continue;

            filteredResolutions.Add(res);
            addedResolutions.Add(resolutionKey);
        }

        // Sort resolutions highest to lowest
        filteredResolutions.Sort((a, b) =>
        {
            // Sort by width first, then height (descending)
            if (a.width != b.width)
                return b.width.CompareTo(a.width);
            return b.height.CompareTo(a.height);
        });

        // Store filtered resolutions
        _availableResolutions = filteredResolutions.ToArray();

        // Populate dropdown options
        _resolutionDropdown.ClearOptions();
        System.Collections.Generic.List<string> options = new System.Collections.Generic.List<string>();

        foreach (Resolution res in _availableResolutions)
        {
            options.Add($"{res.width} x {res.height}");
        }

        _resolutionDropdown.AddOptions(options);
    }

    /// <summary>
    /// Find the index of a resolution in the available resolutions array
    /// </summary>
    private int FindResolutionIndex(int width, int height)
    {
        if (_availableResolutions == null) return -1;

        for (int i = 0; i < _availableResolutions.Length; i++)
        {
            if (_availableResolutions[i].width == width && _availableResolutions[i].height == height)
            {
                return i;
            }
        }

        // If exact match not found, return closest resolution
        return 0;
    }
}
