using PurrNet;
using PurrNet.Prediction;
using System;
using UnityEngine;

public class PlayerHealth : PredictedIdentity<PlayerHealth.HealthState>
{
    [SerializeField] public int _maxHealth;
    [SerializeField] private float _yHeightKillThreshold = -25f;
    [SerializeField] private PlayerMovement _playerMovement;

    public static event Action<PlayerInfo?> OnPlayerDeath;
    public static event Action<PlayerInfo, PlayerInfo> OnPlayerKilled; // (attacker, victim)
    public event Action<PlayerInfo?> OnDeath;

    [HideInInspector] public PredictedEvent<DamageInfo> _onDamageTaken;
    [HideInInspector] public PredictedEvent<(int health, int maxHealth)> _onHealthChanged;

    protected override void LateAwake()
    {
        base.LateAwake();
        if (isOwner) gameObject.name = "local player";

        _onDamageTaken = new PredictedEvent<DamageInfo>(predictionManager, this);
        _onHealthChanged = new PredictedEvent<(int, int)>(predictionManager, this);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    protected override HealthState GetInitialState()
    {
        return new HealthState()
        {
            health = _maxHealth,
            lastHealth = _maxHealth
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


    private void Die(PlayerInfo? attacker = null)
    {
        PlayerInfo? ownerInfo = owner.HasValue ? new PlayerInfo(owner.Value) : null;

        OnPlayerDeath?.Invoke(ownerInfo);
        OnDeath?.Invoke(ownerInfo);

        // Broadcast kill event if there was an attacker
        if (attacker.HasValue && owner.HasValue)
        {
            OnPlayerKilled?.Invoke(attacker.Value, ownerInfo.Value);
        }

        // Record kill/death in ScoreManager (server only)
        if (predictionManager.isServer && attacker.HasValue && owner.HasValue)
        {
            GameManager1v1 scoreManager = FindFirstObjectByType<GameManager1v1>();
            if (scoreManager != null)
            {
                scoreManager.RecordKill(attacker.Value, ownerInfo.Value);
                HUDManager.Instance?.BroadcastEvent("Killed by testplayer");
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

    protected override void Simulate(ref HealthState state, float delta)
    {
        // rb is safe to read from in simulate()
        if (!state.isDead && _playerMovement._rigidbody.position.y < _yHeightKillThreshold)
        {
            state.health = 0;
            state.isDead = true;
        }
    }

    protected override void LateSimulate(ref HealthState state, float delta)
    {
        // Detect health change and fire event
        if (state.health != state.lastHealth)
        {
            _onHealthChanged?.Invoke((state.health, _maxHealth));
            state.lastHealth = state.health;
        }
    }

    private bool _lastDead = false;

    protected override void UpdateView(HealthState viewState, HealthState? verified)
    {
        base.UpdateView(viewState, verified);

        // Health changes now handled by PredictedEvent in LateSimulate()
        // Old C# event kept for backwards compatibility but not invoked from here

        if (viewState.isDead && !_lastDead)
        {
            _lastDead = true;
            Die(viewState.attacker);
        }
    }

    public void ChangeHealth(int change, PlayerInfo? attacker = null)
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

        if (currentState.health <= 0 && !currentState.isDead)
        {
            currentState.isDead = true;
            currentState.attacker = attacker;
        }
        //Debug.Log($"health changed to {currentState.health}");
    }

    public struct HealthState : IPredictedData<PlayerHealth.HealthState>
    {
        public int health;
        public bool isDead;
        public PlayerInfo? attacker;
        public int lastHealth; // Track previous tick's health for change detection

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
