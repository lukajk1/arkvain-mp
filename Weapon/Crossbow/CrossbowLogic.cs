using PurrDiction;
using PurrNet.Prediction;
using UnityEngine;

public class CrossbowLogic : BaseWeaponLogic<CrossbowLogic.ShootInput, CrossbowLogic.ShootState>
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

    public float shootCooldown => 1 / _fireRate;

    // IWeaponLogic interface properties (from BaseWeaponLogic)
    public override int CurrentAmmo => currentState.currentAmmo;
    public override int MaxAmmo => _clipSize;

    private Transform _selfRoot;
    private PredictedEvent _onShootEvent;
    private PredictedEvent<HitInfo> _onHitEvent;
    private PredictedEvent _onReloadEvent;

    protected override void LateAwake()
    {
        base.LateAwake();
        _selfRoot = transform.root;

        //Debug.Log("[CrossbowLogic] LateAwake started");

        _onShootEvent = new PredictedEvent(predictionManager, this);
        _onShootEvent.AddListener(OnShootEventHandler);
        _onHitEvent = new PredictedEvent<HitInfo>(predictionManager, this);
        _onHitEvent.AddListener(OnHitEventHandler);
        _onReloadEvent = new PredictedEvent(predictionManager, this);
        _onReloadEvent.AddListener(OnReloadEventHandler);

        //Debug.Log($"[CrossbowLogic] LateAwake complete. PredictionManager: {(predictionManager != null ? "Valid" : "NULL")}");
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
                InvokeReloadCompleteEvent(); // Call base class helper
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
        if (!input.shoot)
        {
            return;
        }

        // Can't shoot if no ammo
        if (state.currentAmmo <= 0)
        {
            return;
        }

        //Debug.Log($"[CrossbowLogic] SHOOT! Input received. Cooldown: {shootCooldown}s");

        state.cooldownTimer = shootCooldown;

        // Shoot
        Shoot(ref state);

        // Consume ammo
        //state.currentAmmo--;
    }

    private void StartReload(ref ShootState state)
    {
        state.reloadTimer = _reloadTime;
        state.isReloading = true;
        _onReloadEvent?.Invoke();
        InvokeReloadEvent(); // Call base class helper
    }

    private void Shoot(ref ShootState state)
    {
        //Debug.Log("[CrossbowLogic] Shoot() called");

        // Use the camera forward direction directly - it already includes visual recoil
        // from the previous tick's UpdateView() call
        var aimDirection = _playerMovement.currentInput.cameraForward ?? state.lastKnownForward;
        state.lastKnownForward = aimDirection;

        _onShootEvent?.Invoke();

        var position = transform.TransformPoint(_centerOfCamera);

        //Debug.Log($"[CrossbowLogic] Raycast from {position}, direction {aimDirection}, layermask {_shotLayerMask.value}");

        // add a slight forward offset to origin of ray
        if (!Physics.Raycast(position + aimDirection * 0.5f, aimDirection, out RaycastHit hit, Mathf.Infinity, _shotLayerMask, QueryTriggerInteraction.Ignore))
        {
            //Debug.Log("[CrossbowLogic] Raycast missed - no hit");
            return;
        }

        if (hit.collider.transform.root == _selfRoot)
            return;

        //Debug.Log($"[CrossbowLogic] Raycast HIT: {hit.collider.gameObject.name} on layer {hit.collider.gameObject.layer}");

        // Use hit.collider.gameObject instead of hit.transform.gameObject
        // because hit.transform returns the root parent, not the GameObject with the collider
        bool hitPlayer = false;
        bool isHeadshot = false;
        if (hit.collider.TryGetComponent(out A_Hurtbox hurtbox))
        {
            PlayerInfo? attackerInfo = owner.HasValue ? new PlayerInfo(owner.Value) : null;

            if (hurtbox is HurtboxHead head)
            {
                int result = Mathf.RoundToInt(_damage * _headShotModifier);
                head.health.ChangeHealth(-result, attackerInfo);
                isHeadshot = true;
            }
            else
            {
                hurtbox.health.ChangeHealth(-_damage, attackerInfo);
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
    /// Internal handler for PredictedEvent. Invokes public C# event for CrossbowVisual and HitmarkerManager.
    /// </summary>
    private void OnShootEventHandler()
    {
        InvokeOnShoot(currentState.lastKnownForward);
    }

    /// <summary>
    /// Internal handler for PredictedEvent. Invokes public C# event for CrossbowVisual and HitmarkerManager.
    /// </summary>
    private void OnHitEventHandler(HitInfo hitInfo)
    {
        InvokeOnHit(hitInfo);
    }

    /// <summary>
    /// Internal handler for PredictedEvent. Invokes public C# event for CrossbowVisual.
    /// </summary>
    private void OnReloadEventHandler()
    {
        InvokeReloadEvent();
    }

    protected override void UpdateView(ShootState viewState, ShootState? verified)
    {
        base.UpdateView(viewState, verified);
    }


    protected override void UpdateInput(ref ShootInput input)
    {
        bool attackPressed = InputManager.Instance.Player.Attack.IsPressed();

        if (attackPressed)
        {
            //Debug.Log("[CrossbowLogic] UpdateInput: Attack button IS pressed");
        }

        input.shoot |= attackPressed;
        input.reload |= InputManager.Instance.Player.Reload.WasPressedThisFrame();
    }

    protected override void ModifyExtrapolatedInput(ref ShootInput input)
    {
        // 'predict' to be false
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
