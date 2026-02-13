using PurrDiction;
using PurrNet.Prediction;
using UnityEngine;

public class TrackingGunLogic : PredictedIdentity<TrackingGunLogic.ShootInput, TrackingGunLogic.ShootState>, IWeaponLogic
{
    [Header("Stats")]
    [SerializeField] private float _fireRate;
    [SerializeField] private int _damage;
    [SerializeField] private float _headShotModifier;
    [SerializeField] private int _clipSize = 100;
    [SerializeField] private float _reloadTime;
    [SerializeField] private float _maxRange = 9f;

    [Header("Refs")]
    [SerializeField] private Vector3 _centerOfCamera;
    [SerializeField] private LayerMask _shotLayerMask;

    [Header("References")]
    [SerializeField] private PlayerMovement _playerMovement;

    public float shootCooldown => 1 / _fireRate;

    // IWeaponLogic interface properties
    public int CurrentAmmo => currentState.currentAmmo;
    public int MaxAmmo => _clipSize;

    // IWeaponLogic interface events (public for HitmarkerManager)
    public event System.Action<HitInfo> OnHit;
    public event System.Action OnShoot;
    public event System.Action OnEquipped;
    public event System.Action OnHolstered;

    // Additional events for TrackingGunVisual
    public event System.Action onReload;

    private PredictedEvent _onShootEvent;
    private PredictedEvent<HitInfo> _onHitEvent;
    private PredictedEvent _onReloadEvent;

    protected override void LateAwake()
    {
        base.LateAwake();
        _onShootEvent = new PredictedEvent(predictionManager, this);
        _onShootEvent.AddListener(OnShootEventHandler);
        _onHitEvent = new PredictedEvent<HitInfo>(predictionManager, this);
        _onHitEvent.AddListener(OnHitEventHandler);
        _onReloadEvent = new PredictedEvent(predictionManager, this);
        _onReloadEvent.AddListener(OnReloadEventHandler);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _onShootEvent.RemoveListener(OnShootEventHandler);
        _onHitEvent.RemoveListener(OnHitEventHandler);
        _onReloadEvent.RemoveListener(OnReloadEventHandler);
    }

    protected override void Simulate(ShootInput input, ref ShootState state, float delta)
    {
        // Count down reload timer
        if (state.reloadTimer > 0)
        {
            state.reloadTimer -= delta;
            if (state.reloadTimer <= 0)
            {
                // Reload complete
                state.currentAmmo = _clipSize;
                state.isReloading = false;
            }
            return; // Can't shoot while reloading
        }

        // Count down shoot cooldown
        if (state.cooldownTimer > 0)
        {
            state.cooldownTimer -= delta;
            return;
        }

        // Manual reload input
        if (input.reload && !state.isReloading && state.currentAmmo < _clipSize)
        {
            StartReload(ref state);
            return;
        }

        // Auto-reload if out of ammo
        if (state.currentAmmo <= 0 && !state.isReloading)
        {
            StartReload(ref state);
            return;
        }

        // Try to shoot
        if (!input.shoot) return;

        // Can't shoot if no ammo
        if (state.currentAmmo <= 0)
        {
            return;
        }

        state.cooldownTimer = shootCooldown;
        Shoot(ref state);

        // Consume ammo
        state.currentAmmo--;
    }

    private void StartReload(ref ShootState state)
    {
        state.reloadTimer = _reloadTime;
        state.isReloading = true;
        _onReloadEvent?.Invoke();
    }

    private void Shoot(ref ShootState state)
    {
        _onShootEvent?.Invoke();

        var aimDirection = _playerMovement.currentInput.cameraForward ?? state.lastKnownForward;
        state.lastKnownForward = aimDirection;

        var position = transform.TransformPoint(_centerOfCamera);

        // Raycast with limited range
        if (!Physics.Raycast(position + aimDirection * 0.5f, aimDirection, out RaycastHit hit, _maxRange, _shotLayerMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        bool hitPlayer = false;
        bool isHeadshot = false;
        if (hit.collider.TryGetComponent(out A_Hurtbox hurtbox))
        {
            // Apply headshot multiplier if hitting a head hurtbox
            int baseDamage = _damage;
            if (hurtbox is HurtboxHead head)
            {
                baseDamage = Mathf.RoundToInt(_damage * _headShotModifier);
                head.health.ChangeHealth(-baseDamage, owner);
                isHeadshot = true;
            }
            else
            {
                hurtbox.health.ChangeHealth(-baseDamage, owner);
            }

            hitPlayer = true;
        }

        _onHitEvent?.Invoke(new HitInfo
        {
            position = hit.point,
            hitPlayer = hitPlayer,
            isHeadshot = isHeadshot,
            fireDirection = aimDirection,
            surfaceNormal = hitPlayer ? Vector3.zero : hit.normal
        });
    }

    /// <summary>
    /// Internal handler for PredictedEvent. Invokes public C# event for TrackingGunVisual and HitmarkerManager.
    /// </summary>
    private void OnShootEventHandler()
    {
        OnShoot?.Invoke();
    }

    /// <summary>
    /// Internal handler for PredictedEvent. Invokes public C# event for TrackingGunVisual and HitmarkerManager.
    /// </summary>
    private void OnHitEventHandler(HitInfo hitInfo)
    {
        OnHit?.Invoke(hitInfo);
    }

    /// <summary>
    /// Internal handler for PredictedEvent. Invokes public C# event for TrackingGunVisual.
    /// </summary>
    private void OnReloadEventHandler()
    {
        onReload?.Invoke();
    }

    /// <summary>
    /// Called by WeaponManager when this weapon is equipped.
    /// </summary>
    public void TriggerEquipped()
    {
        OnEquipped?.Invoke();
    }

    /// <summary>
    /// Called by WeaponManager when this weapon is holstered.
    /// </summary>
    public void TriggerHolstered()
    {
        OnHolstered?.Invoke();
    }

    protected override void UpdateInput(ref ShootInput input)
    {
        input.shoot |= InputManager.Instance.Player.Attack.IsPressed();
        input.reload |= InputManager.Instance.Player.Reload.WasPressedThisFrame();
    }

    protected override void ModifyExtrapolatedInput(ref ShootInput input)
    {
        input.shoot = false;
        input.reload = false;
    }

    protected override ShootState GetInitialState()
    {
        return new ShootState
        {
            currentAmmo = _clipSize
        };
    }

    public struct ShootInput : IPredictedData<ShootInput>
    {
        public bool shoot;
        public bool reload;

        public void Dispose()
        {
        }
    }

    public struct ShootState : IPredictedData<ShootState>
    {
        public float cooldownTimer;
        public Vector3 lastKnownForward;
        public int currentAmmo;
        public float reloadTimer;
        public bool isReloading;

        public void Dispose()
        {
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        var position = transform.TransformPoint(_centerOfCamera);

        if (!Application.isPlaying) return;
        if (_playerMovement == null) return;

        var forward = _playerMovement.currentInput.cameraForward ?? currentState.lastKnownForward;

        // Draw forward direction with max range
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(position, position + forward * _maxRange);
    }
#endif
}
