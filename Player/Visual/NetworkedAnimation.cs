using Animancer;
using Animancer.TransitionLibraries;
using PurrDiction;
using PurrNet.Prediction;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Server-synced animation playback system using Animancer serialization and PurrNet prediction.
/// Ensures precise animation synchronization across clients for accurate hitbox positioning.
/// </summary>
public class NetworkedAnimation : PredictedIdentity<NetworkedAnimation.AnimInput, NetworkedAnimation.AnimState>
{
    [Header("Animation Setup")]
    [SerializeField] private AnimancerComponent _animancer;
    [SerializeField] private TransitionLibraryAsset _transitionLibrary;

    [Header("Test Input")]
    [SerializeField] private InputActionReference _testPlayAnimationAction;
    [SerializeField] private byte _testAnimationIndex = 0;

    private PredictedEvent<AnimationPlaybackData> _onPlayAnimation;

    protected override void LateAwake()
    {
        base.LateAwake();

        // Initialize predicted event for animation playback
        _onPlayAnimation = new PredictedEvent<AnimationPlaybackData>(predictionManager, this);
        _onPlayAnimation.AddListener(OnPlayAnimationEvent);

        // Ensure we have a valid animancer component
        if (_animancer == null)
        {
            _animancer = GetComponent<AnimancerComponent>();
        }

        // Set transition library if available
        if (_transitionLibrary != null && _animancer != null)
        {
            _animancer.Graph.Transitions = _transitionLibrary.Library;
        }

        // Enable test input action
        if (_testPlayAnimationAction != null)
        {
            _testPlayAnimationAction.action.Enable();
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _onPlayAnimation.RemoveListener(OnPlayAnimationEvent);

        if (_testPlayAnimationAction != null)
        {
            _testPlayAnimationAction.action.Disable();
        }
    }

    protected override void Simulate(AnimInput input, ref AnimState state, float delta)
    {
        // Update animation time tracking
        if (state.isPlaying)
        {
            state.currentTime += delta * state.currentSpeed;
        }

        // Handle test animation playback
        if (input.playAnimation)
        {
            PlayAnimation(input.animationIndex, ref state);
        }

        // Handle animation state updates from serialized data
        if (input.hasSerializedState)
        {
            ApplySerializedState(input.serializedState, ref state);
        }
    }

    private void PlayAnimation(byte animationIndex, ref AnimState state)
    {
        if (_animancer == null || _animancer.Graph.Transitions == null)
        {
            Debug.LogWarning("AnimancerComponent or TransitionLibrary not set up properly.");
            return;
        }

        if (!_animancer.Graph.Transitions.TryGetTransition(animationIndex, out TransitionModifierGroup transition))
        {
            Debug.LogError($"Animation index {animationIndex} not found in TransitionLibrary.");
            return;
        }

        // Update state
        state.currentAnimationIndex = animationIndex;
        state.currentTime = 0f;
        state.currentSpeed = 1f;
        state.isPlaying = true;

        // Invoke predicted event for visual playback
        _onPlayAnimation?.Invoke(new AnimationPlaybackData
        {
            animationIndex = animationIndex,
            startTime = 0f,
            speed = 1f
        });
    }

    private void ApplySerializedState(SerializedAnimationState serializedState, ref AnimState state)
    {
        // Apply the serialized animation state
        state.currentAnimationIndex = serializedState.animationIndex;
        state.currentTime = serializedState.time;
        state.currentSpeed = serializedState.speed;
        state.currentWeight = serializedState.weight;
        state.isPlaying = serializedState.isPlaying;

        // Invoke event to update visuals
        if (serializedState.isPlaying)
        {
            _onPlayAnimation?.Invoke(new AnimationPlaybackData
            {
                animationIndex = serializedState.animationIndex,
                startTime = serializedState.time,
                speed = serializedState.speed,
                weight = serializedState.weight
            });
        }
    }

    private void OnPlayAnimationEvent(AnimationPlaybackData data)
    {
        if (_animancer == null || _animancer.Graph.Transitions == null) return;

        if (!_animancer.Graph.Transitions.TryGetTransition(data.animationIndex, out TransitionModifierGroup transition))
        {
            Debug.LogError($"Animation index {data.animationIndex} not found in TransitionLibrary.");
            return;
        }

        AnimancerLayer layer = _animancer.Layers[0];

        // Play the animation without automatic fade to maintain precise timing
        AnimancerState animState = layer.GetOrCreateState(transition.Transition);
        animState.Time = data.startTime;
        animState.Speed = data.speed;

        // If weight is specified, set it directly; otherwise use full weight
        if (data.weight > 0)
        {
            animState.SetWeight(data.weight);
            layer.Play(animState, fadeDuration: 0f);
        }
        else
        {
            layer.Play(animState, fadeDuration: 0f);
        }
    }

    protected override void UpdateView(AnimState viewState, AnimState? verified)
    {
        base.UpdateView(viewState, verified);

        // Optionally sync view with current state for debugging
        // This ensures visual representation matches the predicted state
    }

    protected override void UpdateInput(ref AnimInput input)
    {
        // Poll for test animation playback button press
        if (_testPlayAnimationAction != null && _testPlayAnimationAction.action.WasPressedThisFrame())
        {
            input.playAnimation = true;
            input.animationIndex = _testAnimationIndex;
        }
    }

    protected override void ModifyExtrapolatedInput(ref AnimInput input)
    {
        // Don't extrapolate animation triggers
        input.playAnimation = false;
        input.hasSerializedState = false;
    }

    /// <summary>
    /// Gathers the current animation state for serialization and network transmission.
    /// Based on Animancer's SerializablePose.GatherFrom() method.
    /// </summary>
    public SerializedAnimationState GatherCurrentState()
    {
        if (_animancer == null) return default;

        var layer = _animancer.Layers[0];
        var activeStates = layer.ActiveStates;

        if (activeStates.Count == 0) return default;

        // Get the primary active state (highest weight or currently fading in)
        AnimancerState primaryState = activeStates[0];
        float remainingFadeDuration = 0f;

        for (int i = 0; i < activeStates.Count; i++)
        {
            AnimancerState state = activeStates[i];
            if (state.FadeGroup != null && state.TargetWeight == 1)
            {
                primaryState = state;
                remainingFadeDuration = state.FadeGroup.RemainingFadeDuration;
                break;
            }
        }

        return new SerializedAnimationState
        {
            animationIndex = (byte)_animancer.Graph.Transitions.IndexOf(primaryState.Key),
            time = primaryState.Time,
            speed = primaryState.Speed,
            weight = primaryState.Weight,
            fadeDuration = remainingFadeDuration,
            isPlaying = primaryState.IsPlaying
        };
    }

    /// <summary>
    /// Applies a serialized animation state to the Animancer component.
    /// Based on Animancer's SerializablePose.ApplyTo() method.
    /// </summary>
    public void ApplyGatheredState(SerializedAnimationState serializedState)
    {
        if (_animancer == null || !serializedState.isPlaying) return;

        if (!_animancer.Graph.Transitions.TryGetTransition(
            serializedState.animationIndex,
            out TransitionModifierGroup transition))
        {
            Debug.LogError($"Animation index {serializedState.animationIndex} not found in TransitionLibrary.");
            return;
        }

        float previousThreshold = AnimancerLayer.WeightlessThreshold;
        try
        {
            AnimancerLayer.WeightlessThreshold = 0;

            AnimancerLayer layer = _animancer.Layers[0];
            AnimancerState state = layer.GetOrCreateState(transition.Transition);

            if (state.Weight != 0)
                state = layer.GetOrCreateWeightlessState(state);

            state.IsPlaying = true;
            state.Time = serializedState.time;
            state.Speed = serializedState.speed;
            state.SetWeight(serializedState.weight);

            layer.Play(state, serializedState.fadeDuration);
        }
        finally
        {
            AnimancerLayer.WeightlessThreshold = previousThreshold;
        }
    }

    #region Data Structures

    public struct AnimInput : IPredictedData<AnimInput>
    {
        public bool playAnimation;
        public byte animationIndex;
        public bool hasSerializedState;
        public SerializedAnimationState serializedState;

        public void Dispose()
        {
        }
    }

    public struct AnimState : IPredictedData<AnimState>
    {
        public byte currentAnimationIndex;
        public float currentTime;
        public float currentSpeed;
        public float currentWeight;
        public bool isPlaying;

        public void Dispose()
        {
        }
    }

    [Serializable]
    public struct SerializedAnimationState
    {
        public byte animationIndex;
        public float time;
        public float speed;
        public float weight;
        public float fadeDuration;
        public bool isPlaying;
    }

    public struct AnimationPlaybackData
    {
        public byte animationIndex;
        public float startTime;
        public float speed;
        public float weight;
    }

    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Debug visualization if needed
        if (_animancer != null && Application.isPlaying)
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2f,
                $"Anim: {currentState.currentAnimationIndex} | Time: {currentState.currentTime:F2}s"
            );
        }
    }
#endif
}
