using Animancer;
using Animancer.TransitionLibraries;
using UnityEngine;

/// <summary>
/// Visual-only armature controller that adds additional smoothing on top of the
/// authoritative GameplayArmatureController for enhanced visual quality.
///
/// NOT networked - reads state directly from GameplayArmatureController for display only.
/// Does NOT drive colliders - purely cosmetic skinned mesh rendering.
///
/// This is optional - if visual quality of gameplay armature is sufficient, you don't need this.
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

    [Header("Parameter Smoothing")]
    [SerializeField] private StringAsset _parameterX;
    [SerializeField] private StringAsset _parameterY;
    [SerializeField] private float _parameterSmoothTime = 0.2f;

    private SmoothedVector2Parameter _smoothedParameter;
    private Vector2MixerState _mixerState;

    private PlayerManualMovement.MovementState _lastPlayedMovementState = PlayerManualMovement.MovementState.Grounded;

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
            _parameterSmoothTime);
    }

    // -------------------------------------------------------------------------
    // Update - reads from gameplay armature and applies extra smoothing
    // -------------------------------------------------------------------------

    private void LateUpdate()
    {
        if (_gameplayArmature == null || _mixerState == null) return;

        // Read authoritative state from gameplay armature
        var gameplayState = _gameplayArmature.currentState;

        // Handle movement state transitions
        if (gameplayState.movementState != _lastPlayedMovementState)
        {
            _lastPlayedMovementState = gameplayState.movementState;
            PlayMovementStateAnimation(gameplayState.movementState);
        }

        // Apply extra smoothing to movement parameters
        if (gameplayState.movementState == PlayerManualMovement.MovementState.Grounded && _smoothedParameter != null)
        {
            _smoothedParameter.TargetValue = new Vector2(
                gameplayState.moveDirectionX,
                gameplayState.moveDirectionY
            );
        }
    }

    // -------------------------------------------------------------------------
    // Animancer playback
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
    // Cleanup
    // -------------------------------------------------------------------------

    private void OnDestroy()
    {
        _smoothedParameter?.Dispose();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_animancer != null && Application.isPlaying && _gameplayArmature != null)
        {
            var state = _gameplayArmature.currentState;
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 3.5f,
                $"Visual Armature (Extra Smooth)\nMove: ({state.moveDirectionX:F2}, {state.moveDirectionY:F2})\nState: {state.movementState}"
            );
        }
    }
#endif
}
