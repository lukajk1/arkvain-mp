using Animancer;
using Animancer.TransitionLibraries;
using PurrNet.Prediction;
using UnityEngine;

/// <summary>
/// Networked animation controller using Animancer 2D directional mixer and PurrNet prediction.
///
/// Phase transitions are derived directly from PlayerManualMovement.currentState.movementState,
/// which is already authoritatively networked.
///
/// JumpStart→Airborne is animation-clip-driven (OnEnd event) and feeds back through AnimInput.
/// Landing is sourced from _onLand PredictedEvent.
///
/// _latestSimulatedState is written in LateSimulate (guaranteed post-simulate) and read in
/// UpdateView, avoiding the module update ordering race where UpdateView can run before the
/// tick cycle within the same Unity Update frame.
///
/// UpdateRollbackInterpolationState is suppressed — the interpolation buffer snapshots state
/// before simulate on normal frames, delivering stale zero values. Visual smoothing is handled
/// by SmoothedVector2Parameter instead.
/// </summary>
public class NetworkedAnimation : PredictedIdentity<NetworkedAnimation.AnimInput, NetworkedAnimation.AnimState>
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
            Debug.LogError("[NetworkedAnimation] _playerMovement is null.");

        if (_animancer == null)
            _animancer = GetComponent<AnimancerComponent>();

        if (_animancer == null || _animancer.Graph.Transitions == null)
        {
            Debug.LogError("[NetworkedAnimation] AnimancerComponent or TransitionLibrary missing.");
            return;
        }

        if (!_animancer.Graph.Transitions.TryGetTransition(_locomotionMixerIndex, out TransitionModifierGroup transitionGroup))
        {
            Debug.LogError($"[NetworkedAnimation] Locomotion mixer index {_locomotionMixerIndex} not found in TransitionLibrary.");
            return;
        }

        _mixerState = _animancer.Play(transitionGroup.Transition) as Vector2MixerState;

        if (_mixerState == null)
        {
            Debug.LogError($"[NetworkedAnimation] Transition at index {_locomotionMixerIndex} did not produce a Vector2MixerState.");
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
        state.moveDirectionX = 0f;
        state.moveDirectionY = 0f;
        state.jumpPhase = JumpPhase.None;
    }

    protected override void SetUnityState(AnimState state) { }

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
                { Debug.LogError($"[NetworkedAnimation] JumpStart transition not found at index {_jumpStartIndex}"); return; }
                var jumpState = _animancer.Play(jumpGroup.Transition);
                jumpState.Events(this, out AnimancerEvent.Sequence jumpEvents);
                jumpEvents.OnEnd = OnJumpStartEnd;
                break;

            case JumpPhase.Airborne:
                if (!_animancer.Graph.Transitions.TryGetTransition(_airborneIndex, out var airGroup))
                { Debug.LogError($"[NetworkedAnimation] Airborne transition not found at index {_airborneIndex}"); return; }
                _animancer.Play(airGroup.Transition);
                break;

            case JumpPhase.Landing:
                if (!_animancer.Graph.Transitions.TryGetTransition(_landingIndex, out var landGroup))
                { Debug.LogError($"[NetworkedAnimation] Landing transition not found at index {_landingIndex}"); return; }
                var landState = _animancer.Play(landGroup.Transition);
                landState.Events(this, out AnimancerEvent.Sequence landEvents);
                landEvents.OnEnd = OnLandingEnd;
                break;

            case JumpPhase.None:
                if (_mixerState != null)
                    _animancer.Play(_mixerState, _landToLocomotionFade);
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

        public void Dispose() { }
    }

    protected override AnimState Interpolate(AnimState from, AnimState to, float t)
    {
        return new AnimState
        {
            moveDirectionX = from.moveDirectionX + (to.moveDirectionX - from.moveDirectionX) * t,
            moveDirectionY = from.moveDirectionY + (to.moveDirectionY - from.moveDirectionY) * t,
            jumpPhase = to.jumpPhase,
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
