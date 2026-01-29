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

    // Transition indices in the library
    [Header("Transition Indices")]
    [SerializeField] private byte _locomotionMixerIndex = 0;
    [SerializeField] private byte _jumpIndex = 1;

    [Header("Parameter Smoothing")]
    [SerializeField] private StringAsset _parameterX;
    [SerializeField] private StringAsset _parameterY;
    [SerializeField] private float _parameterSmoothTime = 0.25f;

    private SmoothedVector2Parameter _smoothedParameter;
    private Vector2MixerState _mixerState;

    protected override void LateAwake()
    {
        base.LateAwake();

        Debug.Log($"[NetworkedAnimation] LateAwake started on {gameObject.name}");

        if (_animancer == null)
        {
            _animancer = GetComponent<AnimancerComponent>();
            Debug.Log($"[NetworkedAnimation] AnimancerComponent: {(_animancer != null ? "Found" : "NULL!")}");
        }

        if (_animancer == null)
        {
            Debug.LogError($"[NetworkedAnimation] No AnimancerComponent found on {gameObject.name}!");
            return;
        }

        // Get the transition library from the AnimancerComponent
        if (_animancer.Graph.Transitions == null)
        {
            Debug.LogError($"[NetworkedAnimation] AnimancerComponent has no TransitionLibrary set! Please assign it in the AnimancerComponent inspector.");
            return;
        }

        Debug.Log($"[NetworkedAnimation] TransitionLibrary found with {_animancer.Graph.Transitions.Count} transitions");

        if (_animancer.Graph.Transitions.TryGetTransition(_locomotionMixerIndex, out TransitionModifierGroup transitionGroup))
        {
            var transition = transitionGroup.Transition;

            Debug.Log($"[NetworkedAnimation] Transition at index {_locomotionMixerIndex}: Type={transition.GetType().Name}");

            // If it's a MixerTransition2D, log the mixer type
            if (transition is MixerTransition2D mixerTransition2D)
            {
                Debug.Log($"[NetworkedAnimation] MixerTransition2D Type: {mixerTransition2D.Type}");
            }

            _mixerState = _animancer.Play(transition) as Vector2MixerState;

            if (_mixerState != null)
            {
                Debug.Log($"[NetworkedAnimation] Successfully created {_mixerState.GetType().Name}");
                Debug.Log($"[NetworkedAnimation] Mixer has {_mixerState.ChildCount} child animations");

                // Log each child animation
                for (int i = 0; i < _mixerState.ChildCount; i++)
                {
                    var child = _mixerState.GetChild(i);
                    if (child != null)
                    {
                        Debug.Log($"[NetworkedAnimation] Child {i}: {child.GetType().Name}, Clip={child.Clip?.name ?? "NULL"}, Weight={child.Weight:F2}");
                    }
                    else
                    {
                        Debug.LogWarning($"[NetworkedAnimation] Child {i} is NULL!");
                    }
                }

                // Initialize smoothed 2D parameter for directional mixer
                _smoothedParameter = new SmoothedVector2Parameter(
                    _animancer,
                    _parameterX,
                    _parameterY,
                    _parameterSmoothTime);

                Debug.Log($"[NetworkedAnimation] SmoothedVector2Parameter initialized with X={_parameterX?.name}, Y={_parameterY?.name}");
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
        // Update smoothed parameter target from input
        state.moveDirectionX = input.moveDirection.x;
        state.moveDirectionY = input.moveDirection.y;

        // Debug log when there's input
        if (input.moveDirection.sqrMagnitude > 0.01f)
        {
            Debug.Log($"[NetworkedAnimation] Simulate - Input: ({input.moveDirection.x:F2}, {input.moveDirection.y:F2})");
        }

        // Apply to smoothed parameters (will interpolate towards target)
        if (_smoothedParameter != null)
        {
            _smoothedParameter.TargetValue = new Vector2(state.moveDirectionX, state.moveDirectionY);

            if (input.moveDirection.sqrMagnitude > 0.01f)
            {
                Debug.Log($"[NetworkedAnimation] Set TargetValue to ({state.moveDirectionX:F2}, {state.moveDirectionY:F2})");
            }
        }
        else if (input.moveDirection.sqrMagnitude > 0.01f)
        {
            Debug.LogWarning($"[NetworkedAnimation] _smoothedParameter is NULL! Cannot set target value.");
        }

        // Log mixer state if it exists
        if (_mixerState != null && input.moveDirection.sqrMagnitude > 0.01f)
        {
            Debug.Log($"[NetworkedAnimation] Mixer Parameter: ({_mixerState.Parameter.x:F2}, {_mixerState.Parameter.y:F2}), IsPlaying: {_mixerState.IsPlaying}, Weight: {_mixerState.Weight:F2}");
        }
    }

    protected override void UpdateInput(ref AnimInput input)
    {
        // Poll movement input every frame (accumulates until GetFinalInput)
        // This matches PlayerMovement's pattern
    }

    protected override void GetFinalInput(ref AnimInput input)
    {
        // Get movement input at tick boundary (same as PlayerMovement)
        input.moveDirection = InputManager.Instance.Player.Move.ReadValue<Vector2>();

        if (input.moveDirection.sqrMagnitude > 0.01f)
        {
            Debug.Log($"[NetworkedAnimation] GetFinalInput - Raw input: ({input.moveDirection.x:F2}, {input.moveDirection.y:F2})");
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
        // Allow extrapolation of movement for smooth animation
        // Unlike jump/shoot which should not be extrapolated
    }

    /// <summary>
    /// Called at initialization to capture the current mixer parameter values.
    /// </summary>
    protected override void GetUnityState(ref AnimState state)
    {
        if (_mixerState == null)
            return;

        // Capture current mixer parameter values
        state.moveDirectionX = _mixerState.Parameter.x;
        state.moveDirectionY = _mixerState.Parameter.y;
        state.locomotionMixerIndex = _locomotionMixerIndex;
    }

    /// <summary>
    /// Called after rollback to restore mixer parameters to the rolled-back state.
    /// Critical for maintaining synchronized animation blending after prediction corrections.
    /// </summary>
    protected override void SetUnityState(AnimState state)
    {
        if (_mixerState == null)
            return;

        // Snap mixer parameters to rolled-back state (immediate, no smoothing)
        _mixerState.Parameter = new Vector2(state.moveDirectionX, state.moveDirectionY);

        // Also update the smoothed parameter target to match
        if (_smoothedParameter != null)
        {
            _smoothedParameter.TargetValue = new Vector2(state.moveDirectionX, state.moveDirectionY);
        }
    }

    #region Data Structures

    public struct AnimInput : IPredictedData<AnimInput>
    {
        public Vector2 moveDirection;

        public void Dispose()
        {
        }
    }

    public struct AnimState : IPredictedData<AnimState>
    {
        public float moveDirectionX;
        public float moveDirectionY;
        public byte locomotionMixerIndex; // Track which mixer is playing for network sync

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
