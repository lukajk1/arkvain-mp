using UnityEngine;
using UnityEngine.InputSystem;
using PurrDiction;
using PurrNet.Prediction;

public class PlayerShooter : PredictedIdentity<PlayerShooter.ShootInput, PlayerShooter.ShootState>
{
    [SerializeField] private float _fireRate = 3;
    [SerializeField] private int _damage = 35;
    [SerializeField] private Vector3 _centerOfCamera;


    public float shootCooldown => 1 / _fireRate;

    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private InputActionReference _shootAction;

    [Header("Particles")]
    [SerializeField] private ParticleSystem _muzzleFlashParticles;
    [SerializeField] private GameObject _hitBodyParticles;
    [SerializeField] private GameObject _hitOtherParticles;

    [SerializeField] private AudioClip _bulletImpact;
    [SerializeField] private AudioClip _bulletFire;

    private PredictedEvent _onShootMuzzle;
    private PredictedEvent<HitInfo> _onHit;

    protected override void LateAwake()
    {
        base.LateAwake();
        _onShootMuzzle = new PredictedEvent(predictionManager, this);
        _onShootMuzzle.AddListener(OnShootMuzzleEvent);
        _onHit = new PredictedEvent<HitInfo>(predictionManager, this);
        _onHit.AddListener(OnHitEvent);

        if (isOwner && _shootAction != null)
        {
            _shootAction.action.Enable();
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _onShootMuzzle.RemoveListener(OnShootMuzzleEvent);
        _onHit.RemoveListener(OnHitEvent);
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
    }

    private void Shoot()
    {
        _onShootMuzzle?.Invoke();

        var forward = _playerMovement.currentInput.cameraForward ?? currentState.lastKnownForward;
        currentState.lastKnownForward = forward;

        var position = transform.TransformPoint(_centerOfCamera);

        // add a slight forward offset to origin of ray
        if (!Physics.Raycast(position + forward * 0.5f, forward, out RaycastHit hit)) return;

        bool hitPlayer = false;
        if (hit.transform.TryGetComponent(out PlayerHealth otherHealth))
        {
            otherHealth.ChangeHealth(-_damage);
            hitPlayer = true;
        }

        _onHit?.Invoke(new HitInfo { position = hit.point, hitPlayer = hitPlayer });
    }

    private void OnShootMuzzleEvent()
    {
        if (_muzzleFlashParticles != null) _muzzleFlashParticles.Play();
        SoundManager.Play(new SoundData(_bulletFire));
    }

    private void OnHitEvent(HitInfo hitInfo)
    {
        if (hitInfo.hitPlayer)
        {
            Instantiate(_hitBodyParticles, hitInfo.position, Quaternion.identity);
        }
        else
        {
            Instantiate(_hitOtherParticles, hitInfo.position, Quaternion.identity);
        }
        SoundManager.Play(new SoundData(_bulletImpact, blend: SoundData.SoundBlend.Spatial, soundPos: hitInfo.position));
    }

    public struct HitInfo
    {
        public Vector3 position;
        public bool hitPlayer;
    }


    protected override void UpdateInput(ref ShootInput input)
    {
        if (_shootAction != null)
        {
            input.shoot |= _shootAction.action.IsPressed();
            //if (input.shoot) Debug.Log("Shoot pressed");
        }
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
