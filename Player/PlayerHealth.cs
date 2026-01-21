using PurrNet;
using PurrNet.Prediction;
using System;
using UnityEngine;

public class PlayerHealth : PredictedIdentity<PlayerHealth.HealthState>
{
    [SerializeField] private int _maxHealth;
    public static event Action<PlayerID?> OnPlayerDeath;
    public static Action KillAllPlayers;

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

        if (currentState.health <= 0)
        {
            Die();
        }
        Debug.Log($"health changed to {currentState.health}");
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
}
