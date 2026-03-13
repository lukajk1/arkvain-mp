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
    public PredictedEvent<PlayerInfo?> _onDeathConfirmed;

    // Regular C# event that external components can safely subscribe to
    public event Action<PlayerInfo?> OnDeath;
    // to signal subscription is safe
    public event Action _eventsReady;

    // Local tracking (NOT in state, not replicated)
    private bool _wasDeadLastSimulate;

    protected override void LateAwake()
    {
        base.LateAwake();
        if (isOwner) gameObject.name = "local player";

        _onDeathConfirmed = new PredictedEvent<PlayerInfo?>(predictionManager, this);
        _onHealthChanged = new PredictedEvent<(int health, int maxHealth)>(predictionManager, this);
        _eventsReady?.Invoke(); // making it nullable prevents errors if there are no subscribers

        Debug.Log($"[PlayerHealth] LateAwake - PredictedEvent Hash: {_onDeathConfirmed?.GetHashCode()}");

        // Bridge: PredictedEvent -> C# event
        if (_onDeathConfirmed != null)
        {
            _onDeathConfirmed.AddListener(attacker =>
            {
                Debug.Log($"[PlayerHealth] PredictedEvent bridge triggered! Attacker: {attacker?.playerID}, Invoking C# event...");
                OnDeath?.Invoke(attacker);
                Debug.Log($"[PlayerHealth] C# OnDeath invoked. Subscriber count: {OnDeath?.GetInvocationList().Length ?? 0}");
            });

            Debug.Log($"[PlayerHealth] Bridge listener added to PredictedEvent");
        }
        else
        {
            Debug.LogError("[PlayerHealth] _onDeathPredictedEvent is NULL in LateAwake!");
        }
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
            lastVisualHealth = _maxHealth,
            deathTimer = 0f,
            isDead = false
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
        if (predictionManager.isServer && state.health > 0 && _playerMovement._rigidbody.position.y < _yHeightKillThreshold)
        {
            state.health = 0;
            state.attacker = null; // Environment kill
        }

        // Death detection - check if we transitioned from alive to dead THIS tick
        // Uses local member variable (not replicated) so each client detects transition independently
        bool isDeadNow = state.health <= 0;

        if (isDeadNow && !_wasDeadLastSimulate)
        {
            state.isDead = true;
            state.deathTimer = 0.2f; // Delay deletion to allow state to replicate

            Debug.Log($"[PlayerHealth] Death transition detected! isServer: {predictionManager.isServer}, attacker: {state.attacker?.playerID}");
            _onDeathConfirmed.Invoke(state.attacker);
        }

        // Update local tracking for next tick (NOT in state, so doesn't replicate)
        _wasDeadLastSimulate = isDeadNow;

        // Delayed deletion - allows state to replicate to clients first
        if (state.isDead && state.deathTimer > 0)
        {
            state.deathTimer -= delta;
            if (state.deathTimer <= 0 && predictionManager.isServer)
            {
                ExecuteDeath(state.attacker);
            }
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
        public float deathTimer; // Delay before server deletes object (allows state to replicate)

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
