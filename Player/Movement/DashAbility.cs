using UnityEngine;
using PurrNet.Prediction;

public class DashAbility : BaseAbilityLogic<DashAbility.DashInput, DashAbility.State>
{
    public override float CooldownNormalized => _dashCooldown > 0f ? Mathf.Clamp01(1f - currentState.cooldown / _dashCooldown) : 1f;

    [SerializeField] private PlayerManualMovement _movement;
    [SerializeField] private float _dashForce = 12f;
    [SerializeField] private float _dashCooldown = 1f;
    [SerializeField] private AudioClip _dashClip;
    [SerializeField] private AudioClip _cooldownNotUpClip;

    protected override void Simulate(DashInput input, ref State state, float delta)
    {
        state.cooldown -= delta;
        state.triedDashOnCooldown = false;

        if (input.dash && state.cooldown <= 0f)
        {
            state.cooldown = _dashCooldown;
            var dir = (transform.forward * input.dashDirection.y + transform.right * input.dashDirection.x).normalized;
            _movement.QueueLaunchImpulse(dir * _dashForce);
        }
        else if (input.dash && state.cooldown > 0f)
        {
            state.triedDashOnCooldown = true;
        }
    }

    private float? _lastViewCooldown;
    private bool _lastTriedDashOnCooldown;

    protected override void UpdateView(State viewState, State? verified)
    {
        base.UpdateView(viewState, verified);

        if (_lastViewCooldown.HasValue)
        {
            if (viewState.cooldown > _lastViewCooldown.Value)
                SoundManager.Play(new SoundData(_dashClip, varyPitch: false, varyVolume: false));
            else if (viewState.triedDashOnCooldown && !_lastTriedDashOnCooldown)
                SoundManager.Play(new SoundData(_cooldownNotUpClip, varyPitch: false, varyVolume: false));
        }

        _lastViewCooldown = viewState.cooldown;
        _lastTriedDashOnCooldown = viewState.triedDashOnCooldown;
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
        public bool triedDashOnCooldown;
        public void Dispose() { }
    }

    public struct DashInput : IPredictedData<DashInput>
    {
        public bool dash;
        public Vector2 dashDirection;
        public void Dispose() { }
    }
}
