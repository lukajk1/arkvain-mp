using UnityEngine;
using PurrDiction;
using PurrNet.Prediction;

public class PlayerShooter : PredictedIdentity<PlayerShooter.ShootInput, PlayerShooter.ShootState>
{
    [SerializeField] private float _fireRate = 3;

    public float shootCooldown => 1 / _fireRate;

    [SerializeField] private PlayerMovement _playerMovement;

    protected override void Simulate(ShootInput input, ref ShootState state, float delta)
    {
        if (state.cooldownTimer > 0)
        {
            state.cooldownTimer -= delta;
            return;
        }

        if (!input.shoot) return;

        state.cooldownTimer = shootCooldown;

        Debug.Log("shooting");
    }

    protected override void UpdateInput(ref ShootInput input)
    {
        input.shoot |= Input.GetKey(KeyCode.Mouse0);
    }

    protected override void ModifyExtrapolatedInput(ref ShootInput input)
    {
        // 'predict' to be false
        input.shoot = false;
    }

    public struct ShootInput : IPredictedData<ShootInput>
    {
        public bool shoot;

        public void Dispose()
        {
        }
    }
    public struct ShootState : IPredictedData<ShootState>
    {
        public float cooldownTimer;

        public void Dispose()
        {
        }
    }
}
