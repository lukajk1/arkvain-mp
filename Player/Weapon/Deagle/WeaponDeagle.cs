using PurrDiction;
using PurrNet.Prediction;
using UnityEngine;
using static CrossbowLogic;

public class WeaponDeagle : PredictedIdentity<WeaponDeagle.ShootInput, WeaponDeagle.ShootState>
{
    private float _fireRate = 1;
    private int _damage = 45;
    [SerializeField] private Vector3 _centerOfCamera;
    [Tooltip("dmg ratio out to 100m away from target")]
    [SerializeField] private AnimationCurve _damageFalloff;

    public float shootCooldown => 1 / _fireRate;

    [SerializeField] private PlayerMovement _playerMovement;

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
            // Calculate distance to target
            float distance = Vector3.Distance(position, hit.point);

            // Map distance (0-100m) to curve time (0-1)
            float curveTime = Mathf.Clamp01(distance / 100f);

            // Sample curve to get damage multiplier
            float damageMultiplier = _damageFalloff.Evaluate(curveTime);

            // Calculate final damage
            int finalDamage = Mathf.RoundToInt(_damage * damageMultiplier);

            otherHealth.ChangeHealth(-finalDamage);
            hitPlayer = true;
        }

        _onHit?.Invoke(new HitInfo { position = hit.point, hitPlayer = hitPlayer });
    }

    private void OnShootMuzzleEvent()
    {
        if (_muzzleFlashParticles != null) _muzzleFlashParticles.Play();
        SoundManager.Play(new SoundData(_bulletFire, blend: SoundData.SoundBlend.Spatial, soundPos: transform.position));
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
        input.shoot |= InputManager.Instance.Player.Attack.IsPressed();
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

        //Debug.Log($"Transform pos: {transform.position}, Camera center: {_centerOfCamera}, Calculated position: {position}");

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
