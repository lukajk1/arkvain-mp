using PurrDiction;
using PurrNet.Prediction;
using UnityEngine;

public class DeagleLogic : PredictedIdentity<DeagleLogic.ShootInput, DeagleLogic.ShootState>, IWeaponLogic
{
    [SerializeField] private float _fireRate = 1;
    [SerializeField] private int _damage = 45;
    [SerializeField] private Vector3 _centerOfCamera;
    [SerializeField] private LayerMask _shotLayerMask;

    [Header("Damage Falloff")]
    [Tooltip("Damage ratio out to 100m away from target")]
    [SerializeField] private AnimationCurve _damageFalloff;

    [Header("References")]
    [SerializeField] private PlayerMovement _playerMovement;

    public float shootCooldown => 1 / _fireRate;

    // Public events for DeagleVisual to subscribe to
    public event System.Action onShoot;
    public event System.Action<HitInfo> onHit;
    public event System.Action onReload;
    public event System.Action onSwitchToActive;

    private PredictedEvent _onShootEvent;
    private PredictedEvent<HitInfo> _onHitEvent;

    protected override void LateAwake()
    {
        base.LateAwake();
        _onShootEvent = new PredictedEvent(predictionManager, this);
        _onShootEvent.AddListener(OnShootEventHandler);
        _onHitEvent = new PredictedEvent<HitInfo>(predictionManager, this);
        _onHitEvent.AddListener(OnHitEventHandler);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _onShootEvent.RemoveListener(OnShootEventHandler);
        _onHitEvent.RemoveListener(OnHitEventHandler);
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
        Shoot(ref state);
    }

    private void Shoot(ref ShootState state)
    {
        _onShootEvent?.Invoke();

        var aimDirection = _playerMovement.currentInput.cameraForward ?? state.lastKnownForward;
        state.lastKnownForward = aimDirection;

        var position = transform.TransformPoint(_centerOfCamera);

        // Raycast with layermask
        if (!Physics.Raycast(position + aimDirection * 0.5f, aimDirection, out RaycastHit hit, Mathf.Infinity, _shotLayerMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        bool hitPlayer = false;
        if (hit.collider.TryGetComponent(out A_Hurtbox hurtbox))
        {
            // Calculate distance to target
            float distance = Vector3.Distance(position, hit.point);

            // Map distance (0-100m) to curve time (0-1)
            float curveTime = Mathf.Clamp01(distance / 100f);

            // Sample curve to get damage multiplier
            float damageMultiplier = _damageFalloff.Evaluate(curveTime);

            // Calculate final damage
            int finalDamage = Mathf.RoundToInt(_damage * damageMultiplier);

            hurtbox.health.ChangeHealth(-finalDamage, owner);
            hitPlayer = true;
        }

        _onHitEvent?.Invoke(new HitInfo { position = hit.point, hitPlayer = hitPlayer });
    }

    /// <summary>
    /// Internal handler for PredictedEvent. Invokes public C# event for DeagleVisual.
    /// </summary>
    private void OnShootEventHandler()
    {
        onShoot?.Invoke();
    }

    /// <summary>
    /// Internal handler for PredictedEvent. Invokes public C# event for DeagleVisual.
    /// </summary>
    private void OnHitEventHandler(HitInfo hitInfo)
    {
        onHit?.Invoke(hitInfo);
    }

    /// <summary>
    /// Call this when the deagle is switched to as the active weapon.
    /// </summary>
    public void SwitchToActive()
    {
        onSwitchToActive?.Invoke();
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
