using UnityEngine;

public class DeadPlayer : MonoBehaviour
{
    private void OnEnable()
    {
        PlayerHealth.RespawnAllPlayers += OnRespawnAllPlayers;
    }

    private void OnDisable()
    {
        PlayerHealth.RespawnAllPlayers -= OnRespawnAllPlayers;
    }

    private void OnRespawnAllPlayers()
    {
        Destroy(gameObject);
    }
}
