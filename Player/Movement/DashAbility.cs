using UnityEngine;
using PurrNet.Prediction;

public class DashAbility : BaseAbilityLogic<DashAbility.DashInput, DashAbility.State>
{
    public override float CooldownNormalized => _dashCooldown > 0f ? Mathf.Clamp01(1f - currentState.cooldown / _dashCooldown) : 1f;
    public override float CooldownRemaining => Mathf.Max(0f, currentState.cooldown);

    [SerializeField] private PlayerManualMovement _movement;
    [SerializeField] private float _dashDistance = 12f;
    [SerializeField] private float _dashCooldown = 5f;
    [SerializeField] private AudioClip _dashClip;
    [SerializeField] private AudioClip _cooldownNotUpClip;

    [HideInInspector] public PredictedEvent _onDash;
    [HideInInspector] public PredictedEvent _onDashCooldownNotUp;

    protected override void LateAwake()
    {
        base.LateAwake();
        _onDash = new PredictedEvent(predictionManager, this);
        _onDashCooldownNotUp = new PredictedEvent(predictionManager, this);

        _onDash.AddListener(OnDash);
        _onDashCooldownNotUp.AddListener(OnDashCooldownNotUp);
    }

    protected override void Simulate(DashInput input, ref State state, float delta)
    {
        state.cooldown -= delta;

        if (input.dash && state.cooldown <= 0f)
        {
            state.cooldown = _dashCooldown;
            var dir = (transform.forward * input.dashDirection.y + transform.right * input.dashDirection.x).normalized;
            _movement.QueueBlink(dir, _dashDistance);
            _onDash.Invoke();
        }
        else if (input.dash && state.cooldown > 0f)
        {
            _onDashCooldownNotUp.Invoke();
        }
    }

    private void OnDash()
    {
        if (isOwner)
            SoundManager.PlayNonDiegetic(_dashClip, varyPitch: false, varyVolume: false);
        else
            SoundManager.PlayDiegetic(_dashClip, transform.position, varyPitch: false, varyVolume: false);
    }

    private void OnDashCooldownNotUp()
    {
        if (isOwner)
            SoundManager.PlayNonDiegetic(_cooldownNotUpClip, varyPitch: false, varyVolume: false);
    }

    protected override void GetFinalInput(ref DashInput input)
    {
        var move = InputManager.Instance.Player.Move.ReadValue<Vector2>();
        input.dashDirection = move.sqrMagnitude > 0.01f ? move : Vector2.up;
    }

    protected override void UpdateInput(ref DashInput input)
    {
        input.dash |= InputManager.Instance.Player.UseAbility.WasPressedThisFrame();
    }

    protected override void ModifyExtrapolatedInput(ref DashInput input)
    {
        input.dash = false;
    }

    public struct State : IPredictedData<State>
    {
        public float cooldown;
        public void Dispose() { }
    }

    public struct DashInput : IPredictedData<DashInput>
    {
        public bool dash;
        public Vector2 dashDirection;
        public void Dispose() { }
    }
}
