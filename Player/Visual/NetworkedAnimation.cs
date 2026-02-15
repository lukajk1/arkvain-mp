using Animancer;
using Animancer.TransitionLibraries;
using PurrDiction;
using PurrNet.Prediction;
using System;
using UnityEngine;

/// <summary>
/// Server-synced animation playback system using Animancer 2D directional mixer and PurrNet prediction.
/// Drives locomotion animations based on movement input, ensuring precise synchronization across clients.
/// Uses TransitionLibrary for consistent animation identification across network.
/// </summary>
public class NetworkedAnimation : PredictedIdentity<NetworkedAnimation.AnimInput, NetworkedAnimation.AnimState>
{
    [Header("Animation Setup")]
    [SerializeField] private AnimancerComponent _animancer;
    [SerializeField] private PlayerManualMovement _playerMovement;

    // Transition indices in the library
    [Header("Transition Indices")]
    [SerializeField] private byte _locomotionMixerIndex = 0;
    [SerializeField] private byte _jumpStartIndex = 1;
    [SerializeField] private byte _airborneIndex = 2;
    [SerializeField] private byte _landingIndex = 3;

    public enum JumpPhase { None, JumpStart, Airborne, Landing }

    [Header("Parameter Smoothing")]
    [SerializeField] private StringAsset _parameterX;
    [SerializeField] private StringAsset _parameterY;
    [SerializeField] private float _parameterSmoothTime = 0.25f;

    private SmoothedVector2Parameter _smoothedParameter;
    private Vector2MixerState _mixerState;
    private JumpPhase _pendingPhase = JumpPhase.None;

    protected override void LateAwake()
    {
        base.LateAwake();

        if (_playerMovement != null)
        {
            _playerMovement._onJump.AddListener(OnJump);
            _playerMovement._onLand.AddListener(OnLand);
            Debug.Log("[NetworkedAnimation] Subscribed to _onJump and _onLand");
        }
        else
        {
            Debug.LogError("[NetworkedAnimation] _playerMovement is null — jump/land events will not fire!");
        }

        Debug.Log($"[NetworkedAnimation] LateAwake started on {gameObject.name}");

        if (_animancer == null)
        {
            _animancer = GetComponent<AnimancerComponent>();
            Debug.Log($"[NetworkedAnimation] AnimancerComponent: {(_animancer != null ? "Found" : "NULL!")}");
        }

        // Get the transition library from the AnimancerComponent
        if (_animancer.Graph.Transitions == null)
        {
            Debug.LogError($"[NetworkedAnimation] AnimancerComponent has no TransitionLibrary set! Please assign it in the AnimancerComponent inspector.");
            return;
        }

        //Debug.Log($"[NetworkedAnimation] TransitionLibrary found with {_animancer.Graph.Transitions.Count} transitions");

        if (_animancer.Graph.Transitions.TryGetTransition(_locomotionMixerIndex, out TransitionModifierGroup transitionGroup))
        {
            var transition = transitionGroup.Transition;

            //Debug.Log($"[NetworkedAnimation] Transition at index {_locomotionMixerIndex}: Type={transition.GetType().Name}");

            // If it's a MixerTransition2D, log the mixer type
            if (transition is MixerTransition2D mixerTransition2D)
            {
                //Debug.Log($"[NetworkedAnimation] MixerTransition2D Type: {mixerTransition2D.Type}");
            }

            _mixerState = _animancer.Play(transition) as Vector2MixerState;

            if (_mixerState != null)
            {
                // Ensure layer 0 has weight (might be 0 on remote clients)
                if (_animancer.Layers[0].Weight < 0.01f)
                {
                    //Debug.LogWarning($"[NetworkedAnimation] Layer 0 weight was {_animancer.Layers[0].Weight}, setting to 1");
                    _animancer.Layers[0].Weight = 1f;
                }

                // Ensure the mixer has full weight (critical for remote clients)
                _mixerState.Weight = 1f;

                //Debug.Log($"[NetworkedAnimation] Successfully created {_mixerState.GetType().Name}, Weight: {_mixerState.Weight}");
                //Debug.Log($"[NetworkedAnimation] Mixer has {_mixerState.ChildCount} child animations");

                // Log each child animation
                for (int i = 0; i < _mixerState.ChildCount; i++)
                {
                    var child = _mixerState.GetChild(i);
                    if (child != null)
                    {
                        //Debug.Log($"[NetworkedAnimation] Child {i}: {child.GetType().Name}, Clip={child.Clip?.name ?? "NULL"}, Weight={child.Weight:F2}");
                    }
                    else
                    {
                        //Debug.LogWarning($"[NetworkedAnimation] Child {i} is NULL!");
                    }
                }

                // Initialize smoothed 2D parameter for directional mixer
                _smoothedParameter = new SmoothedVector2Parameter(
                    _animancer,
                    _parameterX,
                    _parameterY,
                    _parameterSmoothTime);

                //Debug.Log($"[NetworkedAnimation] SmoothedVector2Parameter initialized with X={_parameterX?.name}, Y={_parameterY?.name}");
            }
            else
            {
                Debug.LogError($"[NetworkedAnimation] Transition at index {_locomotionMixerIndex} is not a Vector2MixerState! Type: {transition.GetType().Name}");
            }
        }
        else
        {
            Debug.LogError($"[NetworkedAnimation] Locomotion mixer index {_locomotionMixerIndex} not found in TransitionLibrary!");
        }
    }

    protected override void Simulate(AnimInput input, ref AnimState state, float delta)
    {
        if (input.hasPhaseRequest && input.requestedPhase != state.jumpPhase)
        {
            Debug.Log($"[NetworkedAnimation] Simulate applying phase: {input.requestedPhase}");
            state.jumpPhase = input.requestedPhase;
            PlayJumpPhase(state.jumpPhase);
        }

        if (state.jumpPhase != JumpPhase.None)
            return;

        // Set the target value for smoothed parameters from input
        if (_smoothedParameter != null)
        {
            _smoothedParameter.TargetValue = new Vector2(input.moveDirection.x, input.moveDirection.y);
        }

        // Read back the ACTUAL mixer state (after smoothing)
        // This is critical for network sync - we need the actual values, not just the target
        if (_mixerState != null)
        {
            state.moveDirectionX = _mixerState.Parameter.x;
            state.moveDirectionY = _mixerState.Parameter.y;
            state.mixerTime = _mixerState.Time;
            state.mixerSpeed = _mixerState.Speed;
        }
        else
        {
            // Fallback if mixer isn't available
            state.moveDirectionX = input.moveDirection.x;
            state.moveDirectionY = input.moveDirection.y;
        }
    }

    private void PlayJumpPhase(JumpPhase phase)
    {
        if (_animancer == null) return;

        switch (phase)
        {
            case JumpPhase.JumpStart:
                if (!_animancer.Graph.Transitions.TryGetTransition(_jumpStartIndex, out var jumpGroup))
                { Debug.LogError($"[NetworkedAnimation] JumpStart transition not found at index {_jumpStartIndex}"); return; }
                var jumpState = _animancer.Play(jumpGroup.Transition);
                Debug.Log("[NetworkedAnimation] Playing JumpStart");
                jumpState.Events(this, out AnimancerEvent.Sequence jumpEvents);
                jumpEvents.OnEnd = OnJumpStartEnd;
                break;
            case JumpPhase.Airborne:
                if (!_animancer.Graph.Transitions.TryGetTransition(_airborneIndex, out var airGroup))
                { Debug.LogError($"[NetworkedAnimation] Airborne transition not found at index {_airborneIndex}"); return; }
                _animancer.Play(airGroup.Transition);
                Debug.Log("[NetworkedAnimation] Playing Airborne");
                break;
            case JumpPhase.Landing:
                if (!_animancer.Graph.Transitions.TryGetTransition(_landingIndex, out var landGroup))
                { Debug.LogError($"[NetworkedAnimation] Landing transition not found at index {_landingIndex}"); return; }
                var landState = _animancer.Play(landGroup.Transition);
                Debug.Log("[NetworkedAnimation] Playing Landing");
                landState.Events(this, out AnimancerEvent.Sequence landEvents);
                landEvents.OnEnd = OnLandingEnd;
                break;
            case JumpPhase.None:
                if (_mixerState != null) _animancer.Play(_mixerState);
                Debug.Log("[NetworkedAnimation] Returning to locomotion mixer");
                break;
        }
    }

    private bool _hasPendingPhase = false;

    protected override void UpdateInput(ref AnimInput input)
    {
        if (_hasPendingPhase)
        {
            Debug.Log($"[NetworkedAnimation] UpdateInput consuming pendingPhase: {_pendingPhase}");
            input.requestedPhase = _pendingPhase;
            input.hasPhaseRequest = true;
            _pendingPhase = JumpPhase.None;
            _hasPendingPhase = false;
        }
    }

    protected override void GetFinalInput(ref AnimInput input)
    {
        // Get movement input at tick boundary (same as PlayerMovement)
        input.moveDirection = InputManager.Instance.Player.Move.ReadValue<Vector2>();

        if (input.moveDirection.sqrMagnitude > 0.01f)
        {
            //Debug.Log($"[NetworkedAnimation] GetFinalInput - Raw input: ({input.moveDirection.x:F2}, {input.moveDirection.y:F2})");
        }
    }

    protected override void SanitizeInput(ref AnimInput input)
    {
        // Normalize input to prevent values > 1 (same as PlayerMovement)
        if (input.moveDirection.magnitude > 1)
        {
            input.moveDirection.Normalize();
        }
    }

    protected override void ModifyExtrapolatedInput(ref AnimInput input)
    {
        input.requestedPhase = JumpPhase.None;
        input.hasPhaseRequest = false;
    }

    /// <summary>
    /// Called at initialization to capture the current mixer state.
    /// </summary>
    protected override void GetUnityState(ref AnimState state)
    {
        if (_mixerState == null)
            return;

        // Capture current mixer state
        state.moveDirectionX = _mixerState.Parameter.x;
        state.moveDirectionY = _mixerState.Parameter.y;
        state.mixerTime = _mixerState.Time;
        state.mixerSpeed = _mixerState.Speed;
        state.weight = _mixerState.Weight;
        state.locomotionMixerIndex = _locomotionMixerIndex;
    }

    /// <summary>
    /// Called after rollback to restore mixer to the rolled-back state.
    /// Critical for maintaining synchronized animation blending after prediction corrections.
    /// </summary>
    protected override void SetUnityState(AnimState state)
    {
        // 1. Ensure the mixer exists (in case SetUnityState runs before LateAwake or if it was destroyed)
        if (_mixerState == null)
        {
            // Re-run your initialization logic here if needed, or simply return if you trust LateAwake
            // Ideally, extract the creation logic from LateAwake into a method like InitializeMixer()
            return;
        }

        // 2. Restore jump phase animation if mid-air
        if (state.jumpPhase != JumpPhase.None)
        {
            PlayJumpPhase(state.jumpPhase);
            return;
        }

        // 3. Apply locomotion mixer parameters
        _mixerState.Parameter = new Vector2(state.moveDirectionX, state.moveDirectionY);
        _mixerState.Time = state.mixerTime;
        _mixerState.Speed = state.mixerSpeed;
        _mixerState.Weight = state.weight;

        // 4. Update smoothed target to match visual snap
        if (_smoothedParameter != null)
        {
            _smoothedParameter.TargetValue = new Vector2(state.moveDirectionX, state.moveDirectionY);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_playerMovement != null)
        {
            _playerMovement._onJump.RemoveListener(OnJump);
            _playerMovement._onLand.RemoveListener(OnLand);
        }
    }

    private void SetPendingPhase(JumpPhase phase)
    {
        _pendingPhase = phase;
        _hasPendingPhase = true;
    }

    private void OnJump()
    {
        Debug.Log("[NetworkedAnimation] OnJump fired, setting pending JumpStart");
        SetPendingPhase(JumpPhase.JumpStart);
    }

    private void OnJumpStartEnd()
    {
        Debug.Log("[NetworkedAnimation] OnJumpStartEnd fired, setting pending Airborne");
        SetPendingPhase(JumpPhase.Airborne);
    }

    private void OnLand()
    {
        Debug.Log("[NetworkedAnimation] OnLand fired, setting pending Landing");
        SetPendingPhase(JumpPhase.Landing);
    }

    private void OnLandingEnd()
    {
        Debug.Log("[NetworkedAnimation] OnLandingEnd fired, returning to None");
        SetPendingPhase(JumpPhase.None);
    }

    #region Data Structures

    public struct AnimInput : IPredictedData<AnimInput>
    {
        public Vector2 moveDirection;
        public JumpPhase requestedPhase;
        public bool hasPhaseRequest;

        public void Dispose()
        {
        }
    }

    public struct AnimState : IPredictedData<AnimState>
    {
        public float moveDirectionX;
        public float moveDirectionY;
        public float mixerTime;  // Sync the mixer's playback time for synchronized footsteps
        public float mixerSpeed; // Sync the mixer's speed
        public float weight;
        public byte locomotionMixerIndex;
        public JumpPhase jumpPhase;

        public void Dispose()
        {
        }
    }

    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Debug visualization
        if (_animancer != null && Application.isPlaying)
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2.5f,
                $"Move: ({currentState.moveDirectionX:F2}, {currentState.moveDirectionY:F2})"
            );

            // Draw direction arrow
            Vector3 direction = new Vector3(currentState.moveDirectionX, 0, currentState.moveDirectionY);
            if (direction.sqrMagnitude > 0.01f)
            {
                UnityEditor.Handles.color = Color.cyan;
                UnityEditor.Handles.DrawLine(
                    transform.position + Vector3.up,
                    transform.position + Vector3.up + direction
                );
                UnityEditor.Handles.DrawWireDisc(
                    transform.position + Vector3.up + direction,
                    Vector3.up,
                    0.1f
                );
            }
        }
    }
#endif
}
