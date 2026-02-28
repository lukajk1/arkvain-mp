using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioMixer _audioMixer;

    private void Awake()
    {
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
    }

    private void Start()
    {
        // Apply settings from GameSettings on start
        ApplyVolumeSettings();
    }

    /// <summary>
    /// Apply volume settings from GameSettings to the AudioMixer
    /// </summary>
    public void ApplyVolumeSettings()
    {
        if (GameSettings.Instance != null)
        {
            SetMasterVolume(GameSettings.Instance.data.masterVolume);
            SetMusicVolume(GameSettings.Instance.data.musicVolume);
            SetSFXVolume(GameSettings.Instance.data.sfxVolume);
        }
    }

    public void SetMasterVolume(float volume)
    {
        SetMixerVolume("MasterVol", volume);
    }

    public void SetSFXVolume(float volume)
    {
        SetMixerVolume("SFXVol", volume);
    }

    public void SetMusicVolume(float volume)
    {
        SetMixerVolume("MusicVol", volume);
    }

    public void SetEnvVolume(float volume)
    {
        SetMixerVolume("EnvVol", volume);
    }

    /// <summary>
    /// Converts linear volume (0-1) to logarithmic decibels and sets mixer parameter
    /// </summary>
    private void SetMixerVolume(string parameterName, float volume)
    {
        if (_audioMixer == null)
        {
            Debug.LogWarning($"AudioMixer not assigned to AudioManager!");
            return;
        }

        float db = volume <= 0.02f ? -80f : Mathf.Log10(volume) * 20f;
        _audioMixer.SetFloat(parameterName, db);
    }
}
