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
    [SerializeField] private AudioClip _deathClip;
    [SerializeField] private Vector3 _offsetForRagdoll;

    private bool _deathEventSubscribed;

    private void Update()
    {
        // Safe subscription to the predicted event
        if (!_deathEventSubscribed && _playerHealth != null && _playerHealth._onDeathPredictedEvent != null)
        {
            _playerHealth._onDeathPredictedEvent.AddListener(OnDeathPredicted);
            _deathEventSubscribed = true;
            Debug.Log("DeathVisualsManager successfully subscribed to death event");
        }
    }

    private void OnDisable()
    {
        if (_deathEventSubscribed && _playerHealth != null)
        {
            _playerHealth._onDeathPredictedEvent.RemoveListener(OnDeathPredicted);
            _deathEventSubscribed = false;
        }
    }

    private void OnDeathPredicted(PlayerInfo? attacker)
    {
        Debug.Log("DeathVisualsManager predicted event was triggered");

        // 1. Kill Notification (If we were the killer)
        if (attacker.HasValue && NetworkManager.main != null && attacker.Value.playerID == NetworkManager.main.localPlayer)
        {
            HitmarkerManager.Instance?.ReportKillConfirmed();
        }

        // 2. Local Effects (If we are the victim)
        if (isOwner)
        {
            Debug.Log("DeathVisualsManager this should be playing for the remote..");

            ScreenspaceEffectManager.SetGrayscale(true);
            
            // Spawn the "corpse" prefab locally
            if (_deadPlayerPrefab != null)
            {
                Instantiate(_deadPlayerPrefab, transform.position + Vector3.up, transform.rotation);
            }
            if (_deathClip != null)
            {
                SoundManager.PlayNonDiegetic(_deathClip, varyPitch: false, varyVolume: false);
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
