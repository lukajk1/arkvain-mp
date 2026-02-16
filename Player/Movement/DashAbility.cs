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

    protected override void Simulate(DashInput input, ref State state, float delta)
    {
        state.cooldown -= delta;
        state.dashFired = false;
        state.triedDashOnCooldown = false;

        if (input.dash && state.cooldown <= 0f)
        {
            state.cooldown = _dashCooldown;
            state.dashFired = true;
            var dir = (transform.forward * input.dashDirection.y + transform.right * input.dashDirection.x).normalized;
            _movement.QueueBlink(dir, _dashDistance);
        }
        else if (input.dash && state.cooldown > 0f)
        {
            state.triedDashOnCooldown = true;
        }
    }

    private bool _lastDashFired;
    private bool _lastTriedDashOnCooldown;

    protected override void UpdateView(State viewState, State? verified)
    {
        base.UpdateView(viewState, verified);

        if (viewState.dashFired && !_lastDashFired)
        {
            SoundManager.Play(new SoundData(_dashClip, varyPitch: false, varyVolume: false));
            Debug.Log("dashed");
        }
        else if (viewState.triedDashOnCooldown && !_lastTriedDashOnCooldown)
            SoundManager.Play(new SoundData(_cooldownNotUpClip, varyPitch: false, varyVolume: false));

        _lastDashFired = viewState.dashFired;
        // last tried dash is for visual feedback only--cd not up sound, etc
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
        public bool dashFired;
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
