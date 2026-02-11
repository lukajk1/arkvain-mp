using UnityEngine;

public class DeadPlayer : MonoBehaviour
{
    private void Start()
    {
        ScreenspaceEffectManager.SetGrayscale(true);
        ScreenspaceEffectManager.FlashBloom();
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
