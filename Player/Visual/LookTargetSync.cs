using UnityEngine;
using PurrNet.Prediction;

public class LookTargetSync : PredictedIdentity<LookTargetSync.LookInput, LookTargetSync.LookState>
{
    [SerializeField] private FirstPersonCamera _camera;
    [SerializeField] private Transform _lookTarget;
    [SerializeField] private float _lookDistance = 10f;

    protected override void Simulate(LookInput input, ref LookState state, float delta)
    {
        // Store the look direction in state
        if (input.lookDirection.HasValue)
        {
            state.lookDirection = input.lookDirection.Value;
        }

        // Update the look target position based on state
        UpdateLookTarget(state.lookDirection);
    }

    protected override void GetFinalInput(ref LookInput input)
    {
        // Only the owner sends their camera forward direction
        if (isOwner && _camera != null)
        {
            input.lookDirection = _camera.forward;
        }
    }

    private void UpdateLookTarget(Vector3 direction)
    {
        if (_lookTarget != null && direction.sqrMagnitude > 0.0001f)
        {
            // Position the look target at a fixed distance in the look direction
            _lookTarget.position = transform.position + direction.normalized * _lookDistance;
        }
    }

    protected override void UpdateView(LookState viewState, LookState? verified)
    {
        // Update visual every frame for smooth interpolation
        UpdateLookTarget(viewState.lookDirection);
    }

    public struct LookInput : IPredictedData<LookInput>
    {
        public Vector3? lookDirection;

        public void Dispose()
        {
        }
    }

    public struct LookState : IPredictedData<LookState>
    {
        public Vector3 lookDirection;

        public void Dispose()
        {
        }
    }
}
