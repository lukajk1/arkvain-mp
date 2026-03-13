using PurrNet;
using PurrNet.Prediction;
using System;
using UnityEngine;

public class PlayerHealth : PredictedIdentity<PlayerHealth.HealthState>
{
    [SerializeField] public int _maxHealth;
    [SerializeField] private float _yHeightKillThreshold = -25f;
    [SerializeField] private PlayerMovement _playerMovement;

    public PredictedEvent<(int health, int maxHealth)> _onHealthChanged;
    // predicted event even though only the server can call this (ie no resimulation) to essentially have a tick-associated observer rpc
    public PredictedEvent<PlayerInfo?> _onDeathPredictedEvent;

    // Regular C# event that external components can safely subscribe to
    public event Action<PlayerInfo?> OnDeath;

    protected override void LateAwake()
    {
        base.LateAwake();
        if (isOwner) gameObject.name = "local player";

        _onHealthChanged = new PredictedEvent<(int, int)>(predictionManager, this);
        _onDeathPredictedEvent = new PredictedEvent<PlayerInfo?>(predictionManager, this);

        // Bridge: PredictedEvent -> C# event
        _onDeathPredictedEvent.AddListener(attacker => OnDeath?.Invoke(attacker));
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
            lastHealth = _maxHealth,
            lastVisualHealth = _maxHealth
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

    /// <summary>
    /// Server-authoritative death execution.
    /// Triggered only when the server confirms isDead is true.
    /// </summary>
    private void ExecuteDeath(PlayerInfo? attacker = null)
    {
        if (!predictionManager.isServer) return;

        PlayerID victimId = owner ?? PlayerID.Server;
        PlayerID killerId = attacker?.playerID ?? PlayerID.Server;

        // Tallying Path: Authoritative scoring in MatchSessionManager
        if (MatchSessionManager.Instance != null && victimId != PlayerID.Server)
        {
            MatchSessionManager.Instance.ReportKill(killerId, victimId);
            Debug.Log($"[PlayerHealth] Reported death to MatchSessionManager: Victim={victimId}, Killer={killerId}");
        }

        predictionManager.hierarchy.Delete(gameObject);
    }

    private void OnRespawnAllPlayers()
    {
        if (predictionManager.isServer)
            predictionManager.hierarchy.Delete(gameObject);
    }

    [ContextMenu("Kill Player")]
    private void KillPlayer()
    {
        ChangeHealth(-9999);
    }

    protected override void Simulate(ref HealthState state, float delta)
    {
        // Server-only fall death check
        if (predictionManager.isServer && !state.isDead && _playerMovement._rigidbody.position.y < _yHeightKillThreshold)
        {
            state.health = 0;
            state.attacker = null; // Environment kill
        }

        // Death detection runs on ALL clients during simulation
        // This ensures PredictedEvent fires on all clients at the same tick
        if (state.health <= 0 && !state.isDead)
        {
            state.isDead = true;
            // Fire event during Simulate() = all clients hear it!
            _onDeathPredictedEvent.Invoke(state.attacker);
            Debug.Log("is dead fired in simulate");

            // Server-only cleanup
            if (predictionManager.isServer)
                ExecuteDeath(state.attacker);
        }
    }

    protected override void LateSimulate(ref HealthState state, float delta)
    {
        // Calculate visual health: clamp to 1 if not confirmed dead by server
        int visualHealth = state.isDead ? 0 : Mathf.Max(1, state.health);

        // Detect visual health change and fire event
        if (visualHealth != state.lastVisualHealth)
        {
            _onHealthChanged?.Invoke((visualHealth, _maxHealth));
            state.lastVisualHealth = visualHealth;
        }

        state.lastHealth = state.health;
    }

    protected override void UpdateView(HealthState viewState, HealthState? verified)
    {
        base.UpdateView(viewState, verified);
    }

    public void ChangeHealth(int change, PlayerInfo? attacker = null)
    {
        if (currentState.isDead) return;

        // Server-side validation: Clamp damage to maximum reasonable value
        if (change < 0)
        {
            int maxDamage = 100;
            if (change < -maxDamage)
            {
                if (predictionManager.isServer)
                    Debug.LogWarning($"[PlayerHealth] Clamping damage from {-change} to {maxDamage}. Attacker: {attacker?.ToString() ?? "unknown"}");
                change = -maxDamage;
            }
        }

        var state = currentState;
        state.health += change;

        // Store attacker info for death processing in Simulate()
        if (state.health <= 0)
            state.attacker = attacker;

        currentState = state;
        // Death will be processed in Simulate() on all clients
    }


    public struct HealthState : IPredictedData<PlayerHealth.HealthState>
    {
        public int health;
        public bool isDead;
        public PlayerInfo? attacker;
        public int lastHealth; 
        public int lastVisualHealth;

        public void Dispose() { }

        public override string ToString()
        {
            return $"Health: {health} (Dead: {isDead})";
        }
    }

    public struct DamageInfo
    {
        public int damage;
        public Vector3 position;
    }
}
