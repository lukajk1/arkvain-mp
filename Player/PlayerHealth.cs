using PurrNet;
using PurrNet.Prediction;
using System;
using UnityEngine;

public class PlayerHealth : PredictedIdentity<PlayerHealth.HealthState>
{
    [SerializeField] public int _maxHealth;
    [SerializeField] private float _yHeightKillThreshold = -25f;
    [SerializeField] private PlayerMovement _playerMovement;

    [HideInInspector] public PredictedEvent<(int health, int maxHealth)> _onHealthChanged;
    // predicted event even though only the server can call this (ie no resimulation) to essentially have a tick-associated observer rpc
    [HideInInspector] public PredictedEvent<PlayerInfo?> _onDeathPredictedEvent;
    protected override void LateAwake()
    {
        base.LateAwake();
        if (isOwner) gameObject.name = "local player";

        _onHealthChanged = new PredictedEvent<(int, int)>(predictionManager, this);
        _onDeathPredictedEvent = new PredictedEvent<PlayerInfo?>(predictionManager, this);
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
        // rb is safe to read from in simulate()
        if (predictionManager.isServer && !state.isDead && _playerMovement._rigidbody.position.y < _yHeightKillThreshold)
        {
            state.health = 0;
            ProcessDeath(ref state, null);
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

        currentState.health += change;

        // Only the server can confirm a death
        if (predictionManager.isServer && currentState.health <= 0 && !currentState.isDead)
        {
            ProcessDeath(ref currentState, attacker);
        }
    }

    private void ProcessDeath(ref HealthState state, PlayerInfo? attacker)
    {
        state.isDead = true;
        state.attacker = attacker;
        _onDeathPredictedEvent.Invoke(attacker);
        
        // Final death execution (scoring and deletion)
        ExecuteDeath(attacker);
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
