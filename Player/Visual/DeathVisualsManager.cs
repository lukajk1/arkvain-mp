using UnityEngine;
using PurrNet;
using PurrNet.Prediction;

/// <summary>
/// Handles all visual and audio feedback when a player dies.
/// Decoupled from PlayerVisualsManager for cleanliness.
/// </summary>
public class DeathVisualsManager : StatelessPredictedIdentity
{
    [Header("References")]
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private PlayerMovement _playerMovement;

    [Header("Visuals")]
    [SerializeField] private GameObject _deadPlayerPrefab;
    [SerializeField] private GameObject _ragdollPrefab;
    [SerializeField] private Vector3 _offsetForRagdoll;

    private void OnEnable()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnDeath += OnDeathEvent;
            Debug.Log("DeathVisualsManager successfully subscribed to death event");
        }
        else
        {
            Debug.LogWarning("DeathVisualsManager: PlayerHealth reference is null!");
        }
    }

    private void OnDisable()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnDeath -= OnDeathEvent;
        }
    }

    private void OnDeathEvent(PlayerInfo? attacker)
    {
        Debug.Log("DeathVisualsManager predicted event was triggered");

        // 1. Kill Notification (If we were the killer)
        // this needs to be updated to use proper setup for ui feedback
        //if (attacker.HasValue && NetworkManager.main != null && attacker.Value.playerID == NetworkManager.main.localPlayer)
        //{
        //    HitmarkerManager.Instance?.ReportKillConfirmed();
        //}

        // 2. Local Effects (If we are the victim)
        if (isOwner)
        {
            Debug.Log("DeathVisualsManager this should be playing for the remote..");

            ScreenspaceEffectManager.SetGrayscale(true);
            
            if (_deadPlayerPrefab != null)
            {
                Instantiate(_deadPlayerPrefab, transform.position + Vector3.up, transform.rotation);
            }
        }

        // 3. Ragdoll (Spawned for everyone)
        if (_ragdollPrefab != null)
        {
            Instantiate(_ragdollPrefab, transform.position + _offsetForRagdoll, transform.rotation);
            //var rb = _ragdollPrefab.GetComponent<Rigidbody>();
            //if (rb != null)
            //    rb.linearVelocity = _playerMovement._rigidbody.linearVelocity;
        }
    }
}
