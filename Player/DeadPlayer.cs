using UnityEngine;

public class DeadPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip onDeathClip;
    private void Start()
    {
        Debug.Log("this should be called HHH");
        ScreenspaceEffectManager.SetGrayscale(true);
        ScreenspaceEffectManager.FlashBloom();

        if (onDeathClip != null) SoundManager.Play(new SoundData(onDeathClip, varyPitch: false, varyVolume: false));
    }
    private void OnEnable()
    {
        GameEvents.RespawnAllPlayers += OnRespawnAllPlayers;
    }

    private void OnDisable()
    {
        GameEvents.RespawnAllPlayers -= OnRespawnAllPlayers;
    }

    private void OnRespawnAllPlayers()
    {
        Destroy(gameObject);
    }
}
