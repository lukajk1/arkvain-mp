using Animancer;
using Animancer.TransitionLibraries;
using PurrNet.Prediction;
using UnityEngine;

/// <summary>
/// Variant of NetworkedAnimation that fully serializes Animancer state into AnimState,
/// following the standard PurrDiction GetUnityState/SetUnityState contract.
///
/// Animancer state (active transition index, clip time, weight, fade duration) is captured in
/// GetUnityState and restored in SetUnityState, seeking clips by time rather than replaying
/// them so rollback is deterministic and side-effect-free.
/// SmoothedVector2Parameter current value and velocity are also captured so the smoother
/// converges correctly after rollback.
///
/// _latestSimulatedState is still written in LateSimulate and read in UpdateView to avoid
/// the module update ordering race (UpdateView can fire before the tick cycle in the same
/// Unity Update frame). UpdateRollbackInterpolationState remains suppressed for the same
/// reason — it snapshots pre-simulate state.
///
/// GetUnityState/SetUnityState correctness is therefore decoupled from UpdateView timing:
/// rollback resimulation uses the captured Animancer state; visual display uses the
/// post-simulate cache.
/// </summary>
public class NetworkedAnimationWithStateCapture : PredictedIdentity<NetworkedAnimationWithStateCapture.AnimInput, NetworkedAnimationWithStateCapture.AnimState>
{
    [Header("Animation Setup")]
    [SerializeField] private AnimancerComponent _animancer;
    [SerializeField] private PlayerManualMovement _playerMovement;

    [Header("Transition Indices")]
    [SerializeField] private byte _locomotionMixerIndex = 0;
    [SerializeField] private byte _jumpStartIndex = 1;
    [SerializeField] private byte _airborneIndex = 2;
    [SerializeField] private byte _landingIndex = 3;

    public enum JumpPhase { None, JumpStart, Airborne, Landing }

    [Header("Blending")]
    [SerializeField] private float _landToLocomotionFade = 0.15f;

    [Header("Parameter Smoothing")]
    [SerializeField] private StringAsset _parameterX;
    [SerializeField] private StringAsset _parameterY;
    [SerializeField] private float _parameterSmoothTime = 0.25f;

    private SmoothedVector2Parameter _smoothedParameter;
    private Vector2MixerState _mixerState;

    private JumpPhase _lastPlayedPhase = JumpPhase.None;
    private AnimState _latestSimulatedState;

    private bool _pendingJumpStartEnded = false;
    private bool _pendingLanding = false;
    private bool _landEventSubscribed = false;

    // -------------------------------------------------------------------------
    // Initialization
    // -------------------------------------------------------------------------

    private void Update()
    {
        if (!_landEventSubscribed && _playerMovement != null && _playerMovement._onLand != null)
        {
            _playerMovement._onLand.AddListener(OnLand);
            _landEventSubscribed = true;
        }
    }

    protected override void LateAwake()
    {
        base.LateAwake();

        if (_playerMovement == null)
            Debug.LogError("[NetworkedAnimationWithStateCapture] _playerMovement is null.");

        if (_animancer == null)
            _animancer = GetComponent<AnimancerComponent>();

        if (_animancer == null || _animancer.Graph.Transitions == null)
        {
            Debug.LogError("[NetworkedAnimationWithStateCapture] AnimancerComponent or TransitionLibrary missing.");
            return;
        }

        if (!_animancer.Graph.Transitions.TryGetTransition(_locomotionMixerIndex, out TransitionModifierGroup transitionGroup))
        {
            Debug.LogError($"[NetworkedAnimationWithStateCapture] Locomotion mixer index {_locomotionMixerIndex} not found in TransitionLibrary.");
            return;
        }

        _mixerState = _animancer.Play(transitionGroup.Transition) as Vector2MixerState;

        if (_mixerState == null)
        {
            Debug.LogError($"[NetworkedAnimationWithStateCapture] Transition at index {_locomotionMixerIndex} did not produce a Vector2MixerState.");
            return;
        }

        if (_animancer.Layers[0].Weight < 0.01f)
            _animancer.Layers[0].Weight = 1f;

        _mixerState.Weight = 1f;

        _smoothedParameter = new SmoothedVector2Parameter(
            _animancer,
            _parameterX,
            _parameterY,
            _parameterSmoothTime);
    }

    // -------------------------------------------------------------------------
    // Simulation — deterministic, no Animancer reads
    // -------------------------------------------------------------------------

    protected override void Simulate(AnimInput input, ref AnimState state, float delta)
    {
        if (_playerMovement == null) return;

        var movementState = _playerMovement.currentState.movementState;

        switch (state.jumpPhase)
        {
            case JumpPhase.None:
                if (movementState == PlayerManualMovement.MovementState.Jumping ||
                    movementState == PlayerManualMovement.MovementState.Airborne)
                    state.jumpPhase = JumpPhase.JumpStart;
                break;

            case JumpPhase.JumpStart:
                if (input.jumpStartEnded)
                    state.jumpPhase = JumpPhase.Airborne;
                break;

            case JumpPhase.Airborne:
                if (input.landingTriggered)
                    state.jumpPhase = JumpPhase.Landing;
                break;

            case JumpPhase.Landing:
                if (input.jumpStartEnded)
                    state.jumpPhase = JumpPhase.None;
                break;
        }

        if (state.jumpPhase == JumpPhase.None)
        {
            state.moveDirectionX = input.moveDirection.x;
            state.moveDirectionY = input.moveDirection.y;
        }
    }

    protected override void LateSimulate(AnimInput input, ref AnimState state, float delta)
    {
        _latestSimulatedState = state;
    }

    // -------------------------------------------------------------------------
    // Input pipeline
    // -------------------------------------------------------------------------

    protected override void UpdateInput(ref AnimInput input)
    {
        if (_pendingJumpStartEnded)
        {
            input.jumpStartEnded = true;
            _pendingJumpStartEnded = false;
        }
        if (_pendingLanding)
        {
            input.landingTriggered = true;
            _pendingLanding = false;
        }
    }

    protected override void GetFinalInput(ref AnimInput input)
    {
        input.moveDirection = InputManager.Instance.Player.Move.ReadValue<Vector2>();
    }

    protected override void SanitizeInput(ref AnimInput input)
    {
        if (input.moveDirection.magnitude > 1f)
            input.moveDirection.Normalize();
    }

    protected override void ModifyExtrapolatedInput(ref AnimInput input)
    {
        input.jumpStartEnded = false;
        input.landingTriggered = false;
    }

    // -------------------------------------------------------------------------
    // Unity state capture / restore (rollback support)
    // -------------------------------------------------------------------------

    protected override void GetUnityState(ref AnimState state)
    {
        if (_animancer == null) return;

        // Capture smoother state
        if (_smoothedParameter != null)
        {
            var cur = _smoothedParameter.CurrentValue;
            var vel = _smoothedParameter.Velocity;
            state.smoothParamX = cur.x;
            state.smoothParamY = cur.y;
            state.smoothVelocityX = vel.x;
            state.smoothVelocityY = vel.y;
        }

        // Capture up to two active Animancer states (fading transition = two states)
        var activeStates = _animancer.Layers[0].ActiveStates;
        int count = activeStates.Count;

        state.stateIndex0 = 255;
        state.stateIndex1 = 255;
        state.remainingFadeDuration = 0f;

        for (int i = 0; i < count && i < 2; i++)
        {
            var s = activeStates[i];
            int libIndex = _animancer.Graph.Transitions.IndexOf(s.Key);
            byte idx = libIndex >= 0 ? (byte)libIndex : (byte)255;

            if (i == 0)
            {
                state.stateIndex0 = idx;
                state.stateTime0 = s.Time;
                state.stateWeight0 = s.Weight;
                if (s.FadeGroup != null && s.TargetWeight == 1f)
                    state.remainingFadeDuration = s.FadeGroup.RemainingFadeDuration;
            }
            else
            {
                state.stateIndex1 = idx;
                state.stateTime1 = s.Time;
                state.stateWeight1 = s.Weight;
            }
        }
    }

    protected override void SetUnityState(AnimState state)
    {
        if (_animancer == null) return;

        // Restore smoother state
        if (_smoothedParameter != null)
        {
            _smoothedParameter.CurrentValue = new Vector2(state.smoothParamX, state.smoothParamY);
            _smoothedParameter.Velocity = new Vector2(state.smoothVelocityX, state.smoothVelocityY);
        }

        // Restore Animancer states by seeking, not replaying
        var layer = _animancer.Layers[0];
        layer.Stop();
        layer.Weight = 1f;

        AnimancerState firstState = null;

        // Restore secondary state first (so primary ends up on top)
        if (state.stateIndex1 != 255 &&
            _animancer.Graph.Transitions.TryGetTransition(state.stateIndex1, out var group1))
        {
            var s = layer.GetOrCreateState(group1.Transition);
            s.IsPlaying = true;
            s.Time = state.stateTime1;
            s.SetWeight(state.stateWeight1);
        }

        if (state.stateIndex0 != 255 &&
            _animancer.Graph.Transitions.TryGetTransition(state.stateIndex0, out var group0))
        {
            var s = layer.GetOrCreateState(group0.Transition);
            s.IsPlaying = true;
            s.Time = state.stateTime0;
            s.SetWeight(state.stateWeight0);
            firstState = s;
        }

        if (firstState != null && state.remainingFadeDuration > 0f)
            layer.Play(firstState, state.remainingFadeDuration);
        else if (firstState != null)
            layer.Play(firstState, 0f);

        // Re-register OnEnd callbacks for clip-driven transitions
        if (state.jumpPhase == JumpPhase.JumpStart && firstState != null)
        {
            firstState.Events(this, out AnimancerEvent.Sequence ev);
            ev.OnEnd = OnJumpStartEnd;
        }
        else if (state.jumpPhase == JumpPhase.Landing && firstState != null)
        {
            firstState.Events(this, out AnimancerEvent.Sequence ev);
            ev.OnEnd = OnLandingEnd;
        }

        // Re-cache _mixerState reference if we restored the locomotion mixer
        if (state.stateIndex0 == _locomotionMixerIndex && firstState is Vector2MixerState mixer)
            _mixerState = mixer;
    }

    public override void UpdateRollbackInterpolationState(float delta, bool accumulateError) { }

    // -------------------------------------------------------------------------
    // Visual update — drives Animancer from post-simulate cached state
    // -------------------------------------------------------------------------

    protected override void UpdateView(AnimState viewState, AnimState? verified)
    {
        var state = _latestSimulatedState;

        if (state.jumpPhase != _lastPlayedPhase)
        {
            _lastPlayedPhase = state.jumpPhase;
            PlayJumpPhaseVisual(state.jumpPhase);
        }

        if (state.jumpPhase == JumpPhase.None && _smoothedParameter != null)
            _smoothedParameter.TargetValue = new Vector2(state.moveDirectionX, state.moveDirectionY);
    }

    // -------------------------------------------------------------------------
    // Animancer playback — visual only, never called during rollback
    // -------------------------------------------------------------------------

    private void PlayJumpPhaseVisual(JumpPhase phase)
    {
        if (_animancer == null) return;

        switch (phase)
        {
            case JumpPhase.JumpStart:
                if (!_animancer.Graph.Transitions.TryGetTransition(_jumpStartIndex, out var jumpGroup))
                { Debug.LogError($"[NetworkedAnimationWithStateCapture] JumpStart transition not found at index {_jumpStartIndex}"); return; }
                var jumpState = _animancer.Play(jumpGroup.Transition);
                jumpState.Events(this, out AnimancerEvent.Sequence jumpEvents);
                jumpEvents.OnEnd = OnJumpStartEnd;
                break;

            case JumpPhase.Airborne:
                if (!_animancer.Graph.Transitions.TryGetTransition(_airborneIndex, out var airGroup))
                { Debug.LogError($"[NetworkedAnimationWithStateCapture] Airborne transition not found at index {_airborneIndex}"); return; }
                _animancer.Play(airGroup.Transition);
                break;

            case JumpPhase.Landing:
                if (!_animancer.Graph.Transitions.TryGetTransition(_landingIndex, out var landGroup))
                { Debug.LogError($"[NetworkedAnimationWithStateCapture] Landing transition not found at index {_landingIndex}"); return; }
                var landState = _animancer.Play(landGroup.Transition);
                landState.Events(this, out AnimancerEvent.Sequence landEvents);
                landEvents.OnEnd = OnLandingEnd;
                break;

            case JumpPhase.None:
                if (_mixerState != null)
                {
                    var fadeState = _animancer.Play(_mixerState, _landToLocomotionFade);
                    fadeState.FadeGroup?.SetEasing(Easing.Sine.Out);
                }
                break;
        }
    }

    // -------------------------------------------------------------------------
    // Event callbacks
    // -------------------------------------------------------------------------

    private void OnJumpStartEnd() => _pendingJumpStartEnded = true;
    private void OnLandingEnd() => _pendingJumpStartEnded = true;
    private void OnLand() => _pendingLanding = true;

    // -------------------------------------------------------------------------
    // Cleanup
    // -------------------------------------------------------------------------

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_landEventSubscribed && _playerMovement != null && _playerMovement._onLand != null)
            _playerMovement._onLand.RemoveListener(OnLand);
        _smoothedParameter?.Dispose();
    }

    // -------------------------------------------------------------------------
    // Data structures
    // -------------------------------------------------------------------------

    public struct AnimInput : IPredictedData<AnimInput>
    {
        public Vector2 moveDirection;
        public bool jumpStartEnded;
        public bool landingTriggered;

        public void Dispose() { }
    }

    public struct AnimState : IPredictedData<AnimState>
    {
        public float moveDirectionX;
        public float moveDirectionY;
        public JumpPhase jumpPhase;

        // Animancer state snapshot
        public byte stateIndex0;
        public float stateTime0;
        public float stateWeight0;
        public byte stateIndex1;
        public float stateTime1;
        public float stateWeight1;
        public float remainingFadeDuration;

        // SmoothedVector2Parameter snapshot
        public float smoothParamX;
        public float smoothParamY;
        public float smoothVelocityX;
        public float smoothVelocityY;

        public void Dispose() { }
    }

    protected override AnimState Interpolate(AnimState from, AnimState to, float t)
    {
        return new AnimState
        {
            moveDirectionX = from.moveDirectionX + (to.moveDirectionX - from.moveDirectionX) * t,
            moveDirectionY = from.moveDirectionY + (to.moveDirectionY - from.moveDirectionY) * t,
            jumpPhase = to.jumpPhase,

            stateIndex0 = to.stateIndex0,
            stateTime0 = from.stateTime0 + (to.stateTime0 - from.stateTime0) * t,
            stateWeight0 = from.stateWeight0 + (to.stateWeight0 - from.stateWeight0) * t,
            stateIndex1 = to.stateIndex1,
            stateTime1 = from.stateTime1 + (to.stateTime1 - from.stateTime1) * t,
            stateWeight1 = from.stateWeight1 + (to.stateWeight1 - from.stateWeight1) * t,
            remainingFadeDuration = from.remainingFadeDuration + (to.remainingFadeDuration - from.remainingFadeDuration) * t,

            smoothParamX = from.smoothParamX + (to.smoothParamX - from.smoothParamX) * t,
            smoothParamY = from.smoothParamY + (to.smoothParamY - from.smoothParamY) * t,
            smoothVelocityX = from.smoothVelocityX + (to.smoothVelocityX - from.smoothVelocityX) * t,
            smoothVelocityY = from.smoothVelocityY + (to.smoothVelocityY - from.smoothVelocityY) * t,
        };
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_animancer != null && Application.isPlaying)
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2.5f,
                $"Move: ({currentState.moveDirectionX:F2}, {currentState.moveDirectionY:F2}) Phase: {currentState.jumpPhase}"
            );
        }
    }
#endif
}
