using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    private static SoundManager i;

    public AudioSource SoundObject;
    public AudioMixerGroup SFXMixer;
    public AudioMixerGroup MusicMixer;
    public AudioMixerGroup EnvMixer;

    private static float volumeVariance = 0.15f;
    private static float pitchVariance = 0.1f;

    [SerializeField] private int SFXPoolSize = 20;

    private static Queue<GameObject> soundPool = new Queue<GameObject>();

    private void Awake()
    {
        if (i == null) { 
        i = this;
        DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            }

        InitializePool();
    }
    #region object pool
    private void InitializePool()
    {
        for (int j = 0; j < SFXPoolSize; j++)
        {
            GameObject soundObj = Instantiate(SoundObject.gameObject);
            soundObj.SetActive(false);
            soundObj.transform.SetParent(transform);
            soundPool.Enqueue(soundObj);
        }
    }

    private static GameObject GetFromPool()
    {
        if (soundPool.Count == 0)
        {
            return null;
        }

        GameObject soundObj = soundPool.Dequeue();
        soundObj.SetActive(true);
        return soundObj;
    }

    public static void ReturnToPool(GameObject soundObj)
    {
        if (soundObj == null) return;

        soundObj.SetActive(false);
        soundObj.transform.SetParent(i.transform);
        soundObj.transform.position = Vector3.zero;

        SoundDestroyer destroyer = soundObj.GetComponent<SoundDestroyer>();
        if (destroyer != null)
        {
            destroyer.OnReturnedToPool();
        }

        soundPool.Enqueue(soundObj);
    }
    #endregion
    public static void Play(SoundData sound)
    {
        if (i == null) {
            Debug.LogError("could not play sound - no soundmanager instance");
            return;
        }
        if (sound.clip == null)
        {
            Debug.LogError("soundclip was null!");
            return;
        }

        GameObject soundObj = GetFromPool();
        if (soundObj == null)
        {
            Debug.LogWarning("Sound pool exhausted! Increase SFXPoolSize or sounds are not returning to pool.");
            return;
        }

        AudioSource audioSource = soundObj.GetComponent<AudioSource>();
        soundObj.transform.position = sound.soundPos;

        // Reset to defaults before applying new settings
        audioSource.volume = sound.volume;
        audioSource.pitch = sound.pitch;

        if (sound.varyVolume)
        {
            float randVolume = Random.Range(sound.volume - volumeVariance, sound.volume + volumeVariance);
            audioSource.volume = randVolume;
        }

        if (sound.varyPitch)
        {
            float randPitch = Random.Range(1 - pitchVariance, 1 + pitchVariance);
            audioSource.pitch = randPitch;
        }

        if (sound.soundBlend == SoundData.SoundBlend.Spatial)
        {
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = sound.minDist;
            audioSource.maxDistance = sound.maxDist;
        }
        else
        {
            audioSource.spatialBlend = 0f;
        }

        switch (sound.type)
        {
            case SoundData.Type.Music:
                audioSource.outputAudioMixerGroup = i.MusicMixer;
                break;
            case SoundData.Type.SFX:
                audioSource.outputAudioMixerGroup = i.SFXMixer;
                break;
            case SoundData.Type.Env:
                audioSource.outputAudioMixerGroup = i.EnvMixer;
                break;
        }

        audioSource.clip = sound.clip;
        audioSource.loop = sound.isLooping;
        audioSource.Play();

        SoundDestroyer destroyer = soundObj.GetComponent<SoundDestroyer>();
        if (destroyer != null)
        {
            destroyer.PlayAndReturn();
        }
    }

    public static void PlayDiegetic(
        AudioClip clip,
        Vector3 position,
        SoundData.Type type = SoundData.Type.SFX,
        float volume = 1f,
        float minDist = 4.5f,
        float maxDist = 100f,
        bool varyPitch = true,
        bool varyVolume = true)
    {
        Play(new SoundData(clip, type: type, soundPos: position, blend: SoundData.SoundBlend.Spatial,
            volume: volume, minDist: minDist, maxDist: maxDist, varyPitch: varyPitch, varyVolume: varyVolume));
    }

    public static void PlayNonDiegetic(
        AudioClip clip,
        SoundData.Type type = SoundData.Type.SFX,
        float volume = 1f,
        bool varyPitch = false,
        bool varyVolume = false)
    {
        Play(new SoundData(clip, type: type, blend: SoundData.SoundBlend.NonSpatial,
            volume: volume, varyPitch: varyPitch, varyVolume: varyVolume));
    }

    /// <summary>
    /// Starts playing a looping sound and returns the AudioSource for later control.
    /// Call StopLoop() with the returned AudioSource when you want to stop the loop.
    /// </summary>
    public static AudioSource StartLoop(SoundData sound)
    {
        if (i == null)
        {
            Debug.LogError("could not start loop - no soundmanager instance");
            return null;
        }
        if (sound.clip == null)
        {
            Debug.LogError("soundclip was null!");
            return null;
        }

        GameObject soundObj = GetFromPool();
        if (soundObj == null)
        {
            Debug.LogWarning("Sound pool exhausted! Increase SFXPoolSize or sounds are not returning to pool.");
            return null;
        }

        AudioSource audioSource = soundObj.GetComponent<AudioSource>();
        soundObj.transform.position = sound.soundPos;

        // Reset to defaults before applying new settings
        audioSource.volume = sound.volume;
        audioSource.pitch = 1f;

        if (sound.varyVolume)
        {
            float randVolume = Random.Range(sound.volume - volumeVariance, sound.volume + volumeVariance);
            audioSource.volume = randVolume;
        }

        if (sound.varyPitch)
        {
            float randPitch = Random.Range(1 - pitchVariance, 1 + pitchVariance);
            audioSource.pitch = randPitch;
        }

        if (sound.soundBlend == SoundData.SoundBlend.Spatial)
        {
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = sound.minDist;
            audioSource.maxDistance = sound.maxDist;
        }
        else
        {
            audioSource.spatialBlend = 0f;
        }

        switch (sound.type)
        {
            case SoundData.Type.Music:
                audioSource.outputAudioMixerGroup = i.MusicMixer;
                break;
            case SoundData.Type.SFX:
                audioSource.outputAudioMixerGroup = i.SFXMixer;
                break;
            case SoundData.Type.Env:
                audioSource.outputAudioMixerGroup = i.EnvMixer;
                break;
        }

        audioSource.clip = sound.clip;
        audioSource.loop = true; // Force looping
        audioSource.Play();

        // Don't auto-return looping sounds - caller must stop them manually
        return audioSource;
    }

    /// <summary>
    /// Stops a looping sound started with StartLoop() and returns it to the pool.
    /// </summary>
    public static void StopLoop(AudioSource audioSource)
    {
        if (audioSource == null) return;

        audioSource.Stop();
        audioSource.loop = false;
        ReturnToPool(audioSource.gameObject);
    }

}