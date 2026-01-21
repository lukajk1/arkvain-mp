using PurrNet.Prediction;
using UnityEngine;

public class PlayerHealth : PredictedIdentity<PlayerHealth.HealthState>
{
    [SerializeField] private int _maxHealth;

    protected override HealthState GetInitialState()
    {
        return new HealthState()
        {
            health = _maxHealth
        };
    }

    public void ChangeHealth(int change)
    {
        currentState.health += change;

        if (currentState.health <= 0)
        {
            Debug.Log("died");
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
