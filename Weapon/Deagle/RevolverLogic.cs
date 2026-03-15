using PurrDiction;
using PurrNet.Prediction;
using UnityEngine;

public class RevolverLogic : BaseWeaponLogic<RevolverLogic.ShootInput, RevolverLogic.ShootState>
{
    [Header("Stats")]
    [SerializeField] private float _fireRate;
    [SerializeField] private int _damage;
    [SerializeField] private float _headShotModifier;
    [SerializeField] private int _clipSize = 7;
    [SerializeField] private float _reloadTime;

    [Header("Refs")]
    [SerializeField] private Vector3 _centerOfCamera;
    [SerializeField] private LayerMask _shotLayerMask;

    [Header("Damage Falloff")]
    [Tooltip("Damage ratio out to 100m away from target")]
    [SerializeField] private AnimationCurve _damageFalloff;

    [Header("References")]
    [SerializeField] private PlayerMovement _playerMovement;

    public float shootCooldown => 1 / _fireRate;

    // IWeaponLogic interface properties
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
        // Gating logic: Only run if this is the currently selected weapon
        if (!IsCurrent) return;

        // Count down reload timer
        if (state.reloadTimer > 0)
        {
            state.reloadTimer -= delta;
            if (state.reloadTimer <= 0)
            {
                // Reload complete
                state.currentAmmo = _clipSize;
                state.isReloading = false;
                InvokeReloadCompleteEvent();
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
        if (state.currentAmmo <= 0) return;

        state.cooldownTimer = shootCooldown;
        Shoot(ref state);

        // Consume ammo
        //state.currentAmmo--;
    }

    private void StartReload(ref ShootState state)
    {
        state.reloadTimer = _reloadTime;
        state.isReloading = true;
        _onReloadEvent?.Invoke();
        InvokeReloadEvent();
    }

    private void Shoot(ref ShootState state)
    {
        var aimDirection = _playerMovement.currentInput.cameraForward ?? state.lastKnownForward;
        state.lastKnownForward = aimDirection;

        _onShootEvent?.Invoke();

        var position = transform.TransformPoint(_centerOfCamera);

        // Raycast with layermask
        if (!Physics.Raycast(position + aimDirection * 0.5f, aimDirection, out RaycastHit hit, Mathf.Infinity, _shotLayerMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        bool hitPlayer = false;
        bool isHeadshot = false;
        if (hit.collider.TryGetComponent(out A_Hurtbox hurtbox))
        {
            PlayerInfo? attackerInfo = owner.HasValue ? new PlayerInfo(owner.Value) : null;
            
            // Calculate distance to target
            float distance = Vector3.Distance(position, hit.point);
            float curveTime = Mathf.Clamp01(distance / 100f);
            float damageMultiplier = _damageFalloff.Evaluate(curveTime);

            if (hurtbox is HurtboxHead head)
            {
                int damage = Mathf.RoundToInt(_damage * _headShotModifier * damageMultiplier);
                head.health.ChangeHealth(-damage, attackerInfo);
                isHeadshot = true;
            }
            else
            {
                int damage = Mathf.RoundToInt(_damage * damageMultiplier);
                hurtbox.health.ChangeHealth(-damage, attackerInfo);
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

    private void OnShootEventHandler()
    {
        InvokeOnShoot(currentState.lastKnownForward);
    }

    private void OnHitEventHandler(HitInfo hitInfo)
    {
        InvokeOnHit(hitInfo);
    }

    private void OnReloadEventHandler()
    {
        InvokeReloadEvent();
    }

    protected override void UpdateInput(ref ShootInput input)
    {
        input.shoot |= PersistentClient.Instance.inputManager.Player.Attack.IsPressed();
        input.reload |= PersistentClient.Instance.inputManager.Player.Reload.WasPressedThisFrame();
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

        // Draw origin sphere
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(position, 0.1f);

        if (!Application.isPlaying) return;
        if (_playerMovement == null) return;

        var forward = _playerMovement.currentInput.cameraForward ?? currentState.lastKnownForward;

        // Draw forward direction
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(position, position + forward * 10f);
    }
#endif
}
