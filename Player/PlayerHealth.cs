using PurrNet;
using PurrNet.Prediction;
using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : PredictedIdentity<PlayerHealth.HealthState>
{
    [SerializeField] private int _maxHealth;
    [SerializeField] private Slider _healthSlider;
    public static event Action<PlayerID?> OnPlayerDeath;
    public static Action KillAllPlayers;

    [HideInInspector] public PredictedEvent<DamageInfo> _onDamageTaken;

    void Awake()
    {
        _healthSlider.value = 1f;
    }

    protected override void LateAwake()
    {
        base.LateAwake();

        _onDamageTaken = new PredictedEvent<DamageInfo>(predictionManager, this);
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
        KillAllPlayers += OnKillAllPlayers;
    }

    private void OnDisable()
    {
        KillAllPlayers -= OnKillAllPlayers;
    }


    private void Die()
    {
        OnPlayerDeath?.Invoke(owner);
        predictionManager.hierarchy.Delete(gameObject);
    }

    private void OnKillAllPlayers()
    {
        predictionManager.hierarchy.Delete(gameObject);
    }

    [ContextMenu("Kill Player")]
    private void KillPlayer()
    {
        ChangeHealth(-9999);
    }

    public void ChangeHealth(int change)
    {
        currentState.health += change;

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
            Die();
        }
        //Debug.Log($"health changed to {currentState.health}");
    }

    // visuals can be called multiple times if they are being called directly in Simulate(). Using updateview() prevents that from ocurring.
    protected override void UpdateView(HealthState viewState, HealthState? verified)
    {
        base.UpdateView(viewState, verified);

        if (_healthSlider)
        {
            _healthSlider.value = (float)currentState.health / _maxHealth;
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
