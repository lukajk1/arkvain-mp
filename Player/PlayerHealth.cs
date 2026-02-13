using PurrNet;
using PurrNet.Prediction;
using System;
using UnityEngine;

public class PlayerHealth : PredictedIdentity<PlayerHealth.HealthState>
{
    [SerializeField] public int _maxHealth;

    public static event Action<PlayerID?> OnPlayerDeath;
    public static event Action<PlayerID, PlayerID> OnPlayerKilled; // (attacker, victim)
    public event Action<PlayerID?> OnDeath;
    public event Action<int, int> OnHealthChanged;

    [HideInInspector] public PredictedEvent<DamageInfo> _onDamageTaken;

    protected override void LateAwake()
    {
        base.LateAwake();

        _onDamageTaken = new PredictedEvent<DamageInfo>(predictionManager, this);
        if (isOwner) gameObject.name = "local player";
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    protected override HealthState GetInitialState()
    {
        return new HealthState()
        {
            health = _maxHealth
        };
    }

    private void OnEnable()
    {
        GameEvents.RespawnAllPlayers += OnRespawnAllPlayers;
    }

    private void OnDisable()
    {
        GameEvents.RespawnAllPlayers -= OnRespawnAllPlayers;
    }


    private void Die(PlayerID? attacker = null)
    {
        OnPlayerDeath?.Invoke(owner);
        OnDeath?.Invoke(owner);

        // Broadcast kill event if there was an attacker
        if (attacker.HasValue && owner.HasValue)
        {
            OnPlayerKilled?.Invoke(attacker.Value, owner.Value);
        }

        // Record kill/death in ScoreManager (server only)
        if (predictionManager.isServer && attacker.HasValue && owner.HasValue)
        {
            GameManager1v1 scoreManager = FindFirstObjectByType<GameManager1v1>();
            if (scoreManager != null)
            {
                scoreManager.RecordKill(attacker.Value, owner.Value);
            }
        }

        predictionManager.hierarchy.Delete(gameObject);
    }

    private void OnRespawnAllPlayers()
    {
        predictionManager.hierarchy.Delete(gameObject);
    }

    [ContextMenu("Kill Player")]
    private void KillPlayer()
    {
        ChangeHealth(-9999);
    }

    public void ChangeHealth(int change, PlayerID? attacker = null)
    {
        // Server-side validation: Clamp damage to maximum reasonable value
        // This prevents a malicious client from dealing 9999 damage
        if (change < 0) // Taking damage
        {
            int maxDamage = 100; // Maximum damage per hit (headshot with most powerful weapon)

            if (change < -maxDamage)
            {
                if (predictionManager.isServer)
                {
                    Debug.LogWarning($"[PlayerHealth] Clamping damage from {-change} to {maxDamage}. Attacker: {attacker?.ToString() ?? "unknown"}");
                }
                change = -maxDamage;
            }
        }

        currentState.health = Mathf.Max(0, currentState.health + change);

        // Fire damage event if taking damage (negative change)
        if (change < 0)
        {
            _onDamageTaken?.Invoke(new DamageInfo
            {
                damage = -change,
                position = transform.position
            });
        }

        if (currentState.health <= 0)
        {
            Die(attacker);
        }
        //Debug.Log($"health changed to {currentState.health}");
    }

    private int _lastBroadcastHealth = -1;

    protected override void UpdateView(HealthState viewState, HealthState? verified)
    {
        base.UpdateView(viewState, verified);

        if (viewState.health != _lastBroadcastHealth)
        {
            _lastBroadcastHealth = viewState.health;
            OnHealthChanged?.Invoke(viewState.health, _maxHealth);
        }
    }

    public struct HealthState : IPredictedData<PlayerHealth.HealthState>
    {
        public int health;

        public void Dispose() { }

        public override string ToString()
        {
            return $"Health: {health}";
        }
    }

    public struct DamageInfo
    {
        public int damage;
        public Vector3 position;
    }
}
