using UnityEngine;
using PurrDiction;
using PurrNet.Prediction;

public class PlayerShooter : PredictedIdentity<PlayerShooter.ShootInput, PlayerShooter.ShootState>
{
    [SerializeField] private float _fireRate = 3;
    [SerializeField] private int _damage = 35;
    [SerializeField] private Vector3 _centerOfCamera;


    public float shootCooldown => 1 / _fireRate;

    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private ParticleSystem _muzzleFlashParticles;
    private PredictedEvent _onShoot;

    protected override void LateAwake()
    {
        base.LateAwake();
        _onShoot = new PredictedEvent(predictionManager, this);
        _onShoot.AddListener(OnShootEvent);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _onShoot.RemoveListener(OnShootEvent);
    }


    protected override void Simulate(ShootInput input, ref ShootState state, float delta)
    {
        if (state.cooldownTimer > 0)
        {
            state.cooldownTimer -= delta;
            return;
        }

        if (!input.shoot) return;

        state.cooldownTimer = shootCooldown;
        Shoot();
        Debug.Log("shooting");
    }

    private void Shoot()
    {
        _onShoot?.Invoke();
        var forward = _playerMovement.currentInput.cameraForward ?? currentState.lastKnownForward;
        currentState.lastKnownForward = forward;

        var position = transform.TransformPoint(_centerOfCamera);

        // add a slight forward offset to origin of ray
        if (!Physics.Raycast(position + forward * 0.5f, forward, out RaycastHit hit)) return;
        if (hit.transform.TryGetComponent(out PlayerHealth otherHealth))
        {
            otherHealth.ChangeHealth(-_damage);
        }
    }

    private void OnShootEvent()
    {
        _muzzleFlashParticles.Play();   
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
        public Vector3 lastKnownForward;
        public void Dispose()
        {
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        var position = transform.TransformPoint(_centerOfCamera);

        Debug.Log($"Transform pos: {transform.position}, Camera center: {_centerOfCamera}, Calculated position: {position}");

        // Draw origin sphere
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(position, 0.1f);

        if (!Application.isPlaying) return;
        if (_playerMovement == null) return;

        var forward = _playerMovement.currentInput.cameraForward ?? currentState.lastKnownForward;

        // Draw forward direction
        Gizmos.color = Color.red;
        Gizmos.DrawLine(position, position + forward * 10f);
    }
#endif
}
