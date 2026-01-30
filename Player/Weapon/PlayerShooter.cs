using PurrDiction;
using PurrNet.Prediction;
using UnityEngine;
using static PlayerShooter;

public class PlayerShooter : PredictedIdentity<PlayerShooter.ShootInput, PlayerShooter.ShootState>
{
    [SerializeField] private float _fireRate = 3;
    private float _headShotModifier = 1.8f;
    [SerializeField] private int _damage = 35;
    [SerializeField] private Vector3 _centerOfCamera;
    [SerializeField] private LayerMask _shotLayerMask;

    [Header("Recoil")]
    [SerializeField] private RecoilProfile _recoilProfile;
    [SerializeField] private Recoil _recoil; 

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
        if (_recoilProfile == null) return;

        // Gradually recover from recoil
        state.recoilOffset = Vector3.Lerp(state.recoilOffset, Vector3.zero, _recoilProfile.recoverySpeed * delta);

        if (state.cooldownTimer > 0)
        {
            state.cooldownTimer -= delta;
            return;
        }

        if (!input.shoot) return;

        state.cooldownTimer = shootCooldown;

        // Shoot first, THEN apply recoil (recoil affects next shot)
        Shoot(ref state);

        // Increment shot counter for deterministic pattern
        state.shotCount++;

        // Add recoil AFTER shooting (affects next shot's aim)
        // Use Perlin noise for deterministic but natural-feeling recoil pattern
        float noiseY = Mathf.PerlinNoise(_recoilProfile.noiseSeed + state.shotCount * 0.1f, 0f);
        float noiseZ = Mathf.PerlinNoise(_recoilProfile.noiseSeed + state.shotCount * 0.1f, 100f);

        // Map Perlin noise (0-1) to range (-1 to 1), then scale by recoil amounts
        state.recoilOffset += new Vector3(
            _recoilProfile.recoilX,
            (noiseY * 2f - 1f) * _recoilProfile.recoilY,
            (noiseZ * 2f - 1f) * _recoilProfile.recoilZ
        );
    }

    private void Shoot(ref ShootState state)
    {
        _onShootMuzzle?.Invoke();

        // Use the camera forward direction directly - it already includes visual recoil
        // from the previous tick's UpdateView() call
        var aimDirection = _playerMovement.currentInput.cameraForward ?? state.lastKnownForward;
        state.lastKnownForward = aimDirection;

        var position = transform.TransformPoint(_centerOfCamera);

        // add a slight forward offset to origin of ray
        if (!Physics.Raycast(position + aimDirection * 0.5f, aimDirection, out RaycastHit hit, Mathf.Infinity, _shotLayerMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        // Use hit.collider.gameObject instead of hit.transform.gameObject
        // because hit.transform returns the root parent, not the GameObject with the collider
        bool hitPlayer = false;
        if (hit.collider.TryGetComponent(out A_Hurtbox hurtbox))
        {
            if (hurtbox is HurtboxHead head)
            {
                int result = Mathf.RoundToInt(_damage * _headShotModifier);
                head.health.ChangeHealth(-result, owner);
            }
            else
            {
                hurtbox.health.ChangeHealth(-_damage, owner);
            }
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

    protected override void UpdateView(ShootState viewState, ShootState? verified)
    {
        base.UpdateView(viewState, verified);

        // Apply server-validated recoil to visual camera rotation (owner only)
        if (isOwner && _recoil != null)
        {
            _recoil.SetRecoilOffset(viewState.recoilOffset);
        }
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
        public Vector3 recoilOffset;
        public int shotCount; // Used for deterministic recoil pattern
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
