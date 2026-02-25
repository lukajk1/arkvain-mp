using Animancer;
using Animancer.TransitionLibraries;
using PurrNet.Prediction;
using UnityEngine;

/// <summary>
/// Authoritative gameplay armature controller that drives collision/hurtbox positioning.
/// Fully serializes Animancer state for deterministic network prediction and rollback.
///
/// This is the primary animation controller - colliders should be parented to this armature.
/// Uses 0.2s parameter smoothing for animation blending while maintaining determinism.
///
/// State-only (no input) - reads movement state from PlayerManualMovement directly.
/// Visual armatures can optionally add additional smoothing on top by reading this state.
/// </summary>
public class GameplayArmatureController : PredictedIdentity<GameplayArmatureController.AnimState>
{
    [Header("Animation Setup")]
    [SerializeField] private AnimancerComponent _animancer;
    [SerializeField] private PlayerManualMovement _playerMovement;
    [SerializeField] private AvatarMask _layerMask;

    [Header("Transition Indices")]
    [SerializeField] private byte _locomotionMixerIndex = 0;
    [SerializeField] private byte _jumpStartIndex = 1;
    [SerializeField] private byte _airborneIndex = 2;
    [SerializeField] private byte _landingIndex = 3;

    [Header("Blending")]
    [SerializeField] private float _landToLocomotionFade = 0.15f;

    [Header("Parameter Smoothing")]
    [SerializeField] private StringAsset _parameterX;
    [SerializeField] private StringAsset _parameterY;
    [SerializeField] private float _parameterSmoothTime = 0.2f;

    private SmoothedVector2Parameter _smoothedParameter;
    private Vector2MixerState _mixerState;

    private PlayerManualMovement.MovementState _lastPlayedMovementState = PlayerManualMovement.MovementState.Grounded;
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
        {
            Debug.LogError("[GameplayArmatureController] _playerMovement is null.");
            return;
        }

        if (_animancer == null)
            _animancer = GetComponent<AnimancerComponent>();

        if (_animancer == null || _animancer.Graph.Transitions == null)
        {
            Debug.LogError("[GameplayArmatureController] AnimancerComponent or TransitionLibrary missing.");
            return;
        }

        if (!_animancer.Graph.Transitions.TryGetTransition(_locomotionMixerIndex, out TransitionModifierGroup transitionGroup))
        {
            Debug.LogError($"[GameplayArmatureController] Locomotion mixer index {_locomotionMixerIndex} not found in TransitionLibrary.");
            return;
        }

        _mixerState = _animancer.Play(transitionGroup.Transition) as Vector2MixerState;

        if (_mixerState == null)
        {
            Debug.LogError($"[GameplayArmatureController] Transition at index {_locomotionMixerIndex} did not produce a Vector2MixerState.");
            return;
        }

        if (_layerMask != null)
            _animancer.Layers[0].Mask = _layerMask;

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
    // Simulation — deterministic, reads from PlayerManualMovement
    // -------------------------------------------------------------------------

    protected override void Simulate(ref AnimState state, float delta)
    {
        if (_playerMovement == null) return;

        var movementState = _playerMovement.currentState.movementState;
        var moveInput = _playerMovement.currentInput.moveDirection;

        // Update movement state from PlayerManualMovement
        state.movementState = movementState;

        // Update move direction when grounded
        if (state.movementState == PlayerManualMovement.MovementState.Grounded)
        {
            state.moveDirectionX = moveInput.x;
            state.moveDirectionY = moveInput.y;
        }

        // Handle landing detection
        if (state.wasAirborne && movementState == PlayerManualMovement.MovementState.Grounded)
        {
            state.justLanded = true;
        }
        else
        {
            state.justLanded = false;
        }

        state.wasAirborne = (movementState == PlayerManualMovement.MovementState.Airborne ||
                             movementState == PlayerManualMovement.MovementState.Jumping);
    }

    protected override void LateSimulate(ref AnimState state, float delta)
    {
        _latestSimulatedState = state;
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

        if (state.movementState != _lastPlayedMovementState)
        {
            _lastPlayedMovementState = state.movementState;
            PlayMovementStateAnimation(state.movementState);
        }

        if (state.movementState == PlayerManualMovement.MovementState.Grounded && _smoothedParameter != null)
            _smoothedParameter.TargetValue = new Vector2(state.moveDirectionX, state.moveDirectionY);
    }

    // -------------------------------------------------------------------------
    // Animancer playback — visual only, never called during rollback
    // -------------------------------------------------------------------------

    private void PlayMovementStateAnimation(PlayerManualMovement.MovementState state)
    {
        if (_animancer == null) return;

        switch (state)
        {
            case PlayerManualMovement.MovementState.Jumping:
                if (_animancer.Graph.Transitions.TryGetTransition(_jumpStartIndex, out var jumpGroup))
                    _animancer.Play(jumpGroup.Transition);
                break;

            case PlayerManualMovement.MovementState.Airborne:
                if (_animancer.Graph.Transitions.TryGetTransition(_airborneIndex, out var airGroup))
                    _animancer.Play(airGroup.Transition);
                break;

            case PlayerManualMovement.MovementState.Grounded:
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

    private void OnLand()
    {
        _pendingLanding = true;
    }

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

    public struct AnimState : IPredictedData<AnimState>
    {
        public float moveDirectionX;
        public float moveDirectionY;
        public PlayerManualMovement.MovementState movementState;
        public bool wasAirborne;
        public bool justLanded;

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
            movementState = to.movementState,
            wasAirborne = to.wasAirborne,
            justLanded = to.justLanded,

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
                $"Gameplay Armature\nMove: ({currentState.moveDirectionX:F2}, {currentState.moveDirectionY:F2})\nState: {currentState.movementState}"
            );
        }
    }
#endif
}
