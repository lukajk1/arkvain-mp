using PurrDiction;
using PurrNet.Prediction;
using UnityEngine;

public class CrossbowLogic : PredictedIdentity<CrossbowLogic.ShootInput, CrossbowLogic.ShootState>, IWeaponLogic
{
    [Header("Stats")]
    [SerializeField] private float _fireRate;
    [SerializeField] private float _headShotModifier;
    [SerializeField] private int _damage;
    [SerializeField] private int _clipSize;
    [SerializeField] private float _reloadTime;

    [Header("Refs")]
    [SerializeField] private Vector3 _centerOfCamera;
    [SerializeField] private LayerMask _shotLayerMask;
    [SerializeField] private PlayerMovement _playerMovement;

    [Header("Recoil")]
    [SerializeField] private RecoilProfile _recoilProfile;
    [SerializeField] private Recoil _recoil; 

    public float shootCooldown => 1 / _fireRate;

    // IWeaponLogic interface properties
    public int CurrentAmmo => currentState.currentAmmo;
    public int MaxAmmo => _clipSize;

    // IWeaponLogic interface events (public for HitmarkerManager)
    public event System.Action<HitInfo> OnHit;
    public event System.Action OnShoot;

    // Additional events for CrossbowVisual
    public event System.Action onReload;
    public event System.Action onSwitchToActive;

    private PredictedEvent _onShootEvent;
    private PredictedEvent<HitInfo> _onHitEvent;
    private PredictedEvent _onReloadEvent;

    protected override void LateAwake()
    {
        base.LateAwake();

        Debug.Log("[CrossbowLogic] LateAwake started");

        _onShootEvent = new PredictedEvent(predictionManager, this);
        _onShootEvent.AddListener(OnShootEventHandler);
        _onHitEvent = new PredictedEvent<HitInfo>(predictionManager, this);
        _onHitEvent.AddListener(OnHitEventHandler);
        _onReloadEvent = new PredictedEvent(predictionManager, this);
        _onReloadEvent.AddListener(OnReloadEventHandler);

        Debug.Log($"[CrossbowLogic] LateAwake complete. PredictionManager: {(predictionManager != null ? "Valid" : "NULL")}, RecoilProfile: {(_recoilProfile != null ? "Valid" : "NULL")}");
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
        if (_recoilProfile == null)
        {
            Debug.LogWarning("[CrossbowLogic] RecoilProfile is null!");
            return;
        }

        // Gradually recover from recoil
        state.recoilOffset = Vector3.Lerp(state.recoilOffset, Vector3.zero, _recoilProfile.recoverySpeed * delta);

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

        // Check if we need to reload
        if (state.currentAmmo <= 0 && !state.isReloading)
        {
            StartReload(ref state);
            return;
        }

        // Try to shoot
        if (!input.shoot)
        {
            return;
        }

        // Can't shoot if no ammo
        if (state.currentAmmo <= 0)
        {
            return;
        }

        Debug.Log($"[CrossbowLogic] SHOOT! Input received. Cooldown: {shootCooldown}s");

        state.cooldownTimer = shootCooldown;

        // Shoot first, THEN apply recoil (recoil affects next shot)
        Shoot(ref state);

        // Consume ammo
        state.currentAmmo--;

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

    private void StartReload(ref ShootState state)
    {
        state.reloadTimer = _reloadTime;
        state.isReloading = true;
        _onReloadEvent?.Invoke();
    }

    private void Shoot(ref ShootState state)
    {
        Debug.Log("[CrossbowLogic] Shoot() called");

        _onShootEvent?.Invoke();

        // Use the camera forward direction directly - it already includes visual recoil
        // from the previous tick's UpdateView() call
        var aimDirection = _playerMovement.currentInput.cameraForward ?? state.lastKnownForward;
        state.lastKnownForward = aimDirection;

        var position = transform.TransformPoint(_centerOfCamera);

        Debug.Log($"[CrossbowLogic] Raycast from {position}, direction {aimDirection}, layermask {_shotLayerMask.value}");

        // add a slight forward offset to origin of ray
        if (!Physics.Raycast(position + aimDirection * 0.5f, aimDirection, out RaycastHit hit, Mathf.Infinity, _shotLayerMask, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("[CrossbowLogic] Raycast missed - no hit");
            return;
        }

        Debug.Log($"[CrossbowLogic] Raycast HIT: {hit.collider.gameObject.name} on layer {hit.collider.gameObject.layer}");

        // Use hit.collider.gameObject instead of hit.transform.gameObject
        // because hit.transform returns the root parent, not the GameObject with the collider
        bool hitPlayer = false;
        bool isHeadshot = false;
        if (hit.collider.TryGetComponent(out A_Hurtbox hurtbox))
        {
            if (hurtbox is HurtboxHead head)
            {
                int result = Mathf.RoundToInt(_damage * _headShotModifier);
                head.health.ChangeHealth(-result, owner);
                isHeadshot = true;
            }
            else
            {
                hurtbox.health.ChangeHealth(-_damage, owner);
            }
            hitPlayer = true;
        }

        _onHitEvent?.Invoke(new HitInfo { position = hit.point, hitPlayer = hitPlayer, isHeadshot = isHeadshot });
    }

    /// <summary>
    /// Internal handler for PredictedEvent. Invokes public C# event for CrossbowVisual and HitmarkerManager.
    /// </summary>
    private void OnShootEventHandler()
    {
        OnShoot?.Invoke();
    }

    /// <summary>
    /// Internal handler for PredictedEvent. Invokes public C# event for CrossbowVisual and HitmarkerManager.
    /// </summary>
    private void OnHitEventHandler(HitInfo hitInfo)
    {
        OnHit?.Invoke(hitInfo);
    }

    /// <summary>
    /// Internal handler for PredictedEvent. Invokes public C# event for CrossbowVisual.
    /// </summary>
    private void OnReloadEventHandler()
    {
        onReload?.Invoke();
    }

    /// <summary>
    /// Call this when the crossbow is switched to as the active weapon.
    /// </summary>
    public void SwitchToActive()
    {
        onSwitchToActive?.Invoke();
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


    protected override void UpdateInput(ref ShootInput input)
    {
        bool attackPressed = InputManager.Instance.Player.Attack.IsPressed();

        if (attackPressed)
        {
            Debug.Log("[CrossbowLogic] UpdateInput: Attack button IS pressed");
        }

        input.shoot |= attackPressed;
    }

    protected override void ModifyExtrapolatedInput(ref ShootInput input)
    {
        // 'predict' to be false
        input.shoot = false;
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
