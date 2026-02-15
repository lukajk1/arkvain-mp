using UnityEngine;



public struct SoundData
{
    public AudioClip clip;
    public Vector3 soundPos;
    public float volume;
    public float pitch;
    public SoundBlend soundBlend;
    public Type type;

    public float minDist;
    public float maxDist;
    public bool isLooping;

    public bool varyPitch;
    public bool varyVolume;

    public SoundData(
        AudioClip clip,
        Type type = Type.SFX,
        Vector3? soundPos = null,
        SoundBlend blend = SoundBlend.NonSpatial,
        float volume = 1f, // not sure if I even want this as an option.. better to normalize and bake it into the clip directly
        float pitch = 1f,
        float minDist = 4.5f,
        float maxDist = 100f,
        bool isLooping = false,
        bool varyPitch = true,
        bool varyVolume = true
        )
    {
        this.clip = clip;
        this.soundPos = soundPos ?? Vector3.zero; // assign vec3.zero if null. Can't be assigned as a default in the parameter declaration
        this.volume = volume;
        this.pitch = pitch;
        this.type = type;
        this.soundBlend = blend;
        this.minDist = minDist;
        this.maxDist = maxDist;
        this.isLooping = isLooping;
        this.varyPitch = varyPitch;
        this.varyVolume = varyVolume;
    }
    public enum SoundBlend
    {
        Spatial,
        NonSpatial
    }

    public enum Type
    {
        Music,
        SFX,
        Env
    }
}
