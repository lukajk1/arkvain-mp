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
    [SerializeField] private GameObject _menuObject;
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
    [SerializeField] private Dropdown _graphicsQualityDropdown;
    [SerializeField] private Toggle _vsyncToggle;
    [SerializeField] private TMP_InputField _targetFrameRateInputField;

    [Header("Buttons")]
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _resetButton;

    private void Start()
    {
        SetState(false);
    }

    public void SetState(bool value)
    {
        _menuObject.SetActive(value);

        if (value) LoadSettingsToUI();

        ClientGame.ModifyCursorUnlockList(value, this);
        InputManager.Instance.ModifyPlayerControlsLockList(value, this);
    }

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

        if (_graphicsQualityDropdown != null)
            _graphicsQualityDropdown.onValueChanged.AddListener(OnGraphicsQualityChanged);

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

        if (_graphicsQualityDropdown != null)
            _graphicsQualityDropdown.onValueChanged.RemoveListener(OnGraphicsQualityChanged);

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

        if (_graphicsQualityDropdown != null)
            _graphicsQualityDropdown.value = GameSettings.Instance.data.graphicsQuality;

        if (_vsyncToggle != null)
            _vsyncToggle.isOn = GameSettings.Instance.data.vsyncEnabled;

        if (_targetFrameRateInputField != null)
            _targetFrameRateInputField.text = GameSettings.Instance.data.targetFrameRate.ToString();
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
    private void OnGraphicsQualityChanged(int value)
    {
        GameSettings.Instance.data.graphicsQuality = value;
        GameSettings.Instance.ApplySettings();
    }

    private void OnVSyncChanged(bool value)
    {
        GameSettings.Instance.data.vsyncEnabled = value;
        GameSettings.Instance.ApplySettings();
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

    // Button callbacks
    private void OnSaveClicked()
    {
        GameSettings.Instance.ApplySettings();
        GameSettings.Instance.SaveToFile();
        SetState(false);
    }

    private void OnResetClicked()
    {
        GameSettings.Instance.ResetToDefaults();
        LoadSettingsToUI();
        Debug.Log("Settings reset to defaults!");
    }

    /// <summary>
    /// Show the settings menu
    /// </summary>
}
