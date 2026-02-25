using Animancer;
using Animancer.TransitionLibraries;
using UnityEngine;

/// <summary>
/// Visual-only armature controller that mirrors GameplayArmatureController with additional smoothing.
///
/// Reads the interpolated view state from GameplayArmatureController (already smooth at 60+ FPS from PurrNet)
/// and applies EXTRA smoothing on top to eliminate any rollback jitter.
///
/// NOT networked - purely cosmetic for what the player sees.
/// Does NOT drive colliders - GameplayArmature handles all collision/hurtboxes.
///
/// Two layers of smoothing:
/// 1. PurrNet's interpolation between ticks (60 FPS updates)
/// 2. Our SmoothedParameter with longer smooth time (configurable, default 0.4s vs gameplay's 0.2s)
/// </summary>
public class VisualArmatureController : MonoBehaviour
{
    [Header("Animation Setup")]
    [SerializeField] private AnimancerComponent _animancer;
    [SerializeField] private GameplayArmatureController _gameplayArmature;
    [SerializeField] private AvatarMask _layerMask;

    [Header("Transition Indices")]
    [SerializeField] private byte _locomotionMixerIndex = 0;
    [SerializeField] private byte _jumpStartIndex = 1;
    [SerializeField] private byte _airborneIndex = 2;
    [SerializeField] private byte _landingIndex = 3;

    [Header("Blending")]
    [SerializeField] private float _landToLocomotionFade = 0.15f;

    [Header("Visual Smoothing")]
    [SerializeField, Tooltip("General visual smoothing factor - higher = smoother but more laggy. 0 = no extra smoothing beyond PurrNet interpolation.")]
    private float _visualSmoothTime = 0.1f;

    [Header("Parameter Smoothing")]
    [SerializeField] private StringAsset _parameterX;
    [SerializeField] private StringAsset _parameterY;
    [Tooltip("Smoothing for directional input to animation mixer")]
    private float _directionalParameterSmoothTime = 0.2f;

    private SmoothedVector2Parameter _smoothedParameter;
    private Vector2MixerState _mixerState;

    private PlayerMovement.MovementState _lastPlayedMovementState = PlayerMovement.MovementState.Grounded;

    // -------------------------------------------------------------------------
    // Initialization
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (_gameplayArmature == null)
        {
            Debug.LogError("[VisualArmatureController] _gameplayArmature is null. This component requires a reference to GameplayArmatureController.");
            enabled = false;
            return;
        }

        if (_animancer == null)
            _animancer = GetComponent<AnimancerComponent>();

        if (_animancer == null || _animancer.Graph.Transitions == null)
        {
            Debug.LogError("[VisualArmatureController] AnimancerComponent or TransitionLibrary missing.");
            enabled = false;
            return;
        }

        if (!_animancer.Graph.Transitions.TryGetTransition(_locomotionMixerIndex, out TransitionModifierGroup transitionGroup))
        {
            Debug.LogError($"[VisualArmatureController] Locomotion mixer index {_locomotionMixerIndex} not found in TransitionLibrary.");
            enabled = false;
            return;
        }

        _mixerState = _animancer.Play(transitionGroup.Transition) as Vector2MixerState;

        if (_mixerState == null)
        {
            Debug.LogError($"[VisualArmatureController] Transition at index {_locomotionMixerIndex} did not produce a Vector2MixerState.");
            enabled = false;
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
            _directionalParameterSmoothTime);
    }

    // -------------------------------------------------------------------------
    // Update - reads interpolated state from gameplay armature and applies extra smoothing
    // -------------------------------------------------------------------------

    private void Update()
    {
        if (_gameplayArmature == null || _mixerState == null) return;

        // Read the raw tick state from gameplay armature
        // (GameplayArmature suppresses interpolation, so we get 30Hz tick updates)
        // Our SmoothedParameter will smooth this out to 60+ FPS
        var state = _gameplayArmature.currentState;

        // Handle movement state transitions
        if (state.movementState != _lastPlayedMovementState)
        {
            _lastPlayedMovementState = state.movementState;
            PlayMovementStateAnimation(state.movementState);
        }

        // Apply smoothing to movement parameters
        // This smooths the 30Hz tick updates into 60+ FPS smooth motion
        if (state.movementState == PlayerMovement.MovementState.Grounded && _smoothedParameter != null)
        {
            _smoothedParameter.TargetValue = new Vector2(
                state.moveDirectionX,
                state.moveDirectionY
            );
        }
    }

    // -------------------------------------------------------------------------
    // Animancer playback
    // -------------------------------------------------------------------------

    private void PlayMovementStateAnimation(PlayerMovement.MovementState state)
    {
        if (_animancer == null) return;

        switch (state)
        {
            case PlayerMovement.MovementState.Jumping:
                if (_animancer.Graph.Transitions.TryGetTransition(_jumpStartIndex, out var jumpGroup))
                    _animancer.Play(jumpGroup.Transition);
                break;

            case PlayerMovement.MovementState.Airborne:
                if (_animancer.Graph.Transitions.TryGetTransition(_airborneIndex, out var airGroup))
                    _animancer.Play(airGroup.Transition);
                break;

            case PlayerMovement.MovementState.Grounded:
                if (_mixerState != null)
                {
                    var fadeState = _animancer.Play(_mixerState, _landToLocomotionFade);
                    fadeState.FadeGroup?.SetEasing(Easing.Sine.Out);
                }
                break;
        }
    }

    // -------------------------------------------------------------------------
    // Cleanup
    // -------------------------------------------------------------------------

    private void OnDestroy()
    {
        _smoothedParameter?.Dispose();
    }

}
