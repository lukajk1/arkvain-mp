using UnityEngine;
using PurrNet.Prediction;

public class NewLookTargetSync : PredictedIdentity<NewLookTargetSync.LookInput, NewLookTargetSync.LookState>
{
    [SerializeField] private FirstPersonCamera _camera;
    [SerializeField] private Transform _gunTransform;
    [SerializeField] private Transform _headTransform;

    private Transform _cameraTransform;
    private Quaternion _gunRotationOffset; // Gun's rotation relative to camera at start
    private Quaternion _headRotationOffset; // Head's rotation relative to camera at start
    private bool _offsetCaptured = false;

    protected override void LateAwake()
    {
        base.LateAwake();

        // Cache camera transform
        if (_camera != null)
        {
            _cameraTransform = _camera.transform;
        }

        if (_cameraTransform != null)
        {
            // Calculate initial offset: gun's world rotation relative to camera's world rotation
            if (_gunTransform != null)
            {
                _gunRotationOffset = Quaternion.Inverse(_cameraTransform.rotation) * _gunTransform.rotation;
            }

            // Calculate initial offset: head's world rotation relative to camera's world rotation
            if (_headTransform != null)
            {
                _headRotationOffset = Quaternion.Inverse(_cameraTransform.rotation) * _headTransform.rotation;
            }

            _offsetCaptured = true;
        }
    }

    protected override void Simulate(LookInput input, ref LookState state, float delta)
    {
        // Store camera rotation in state for networking
        if (input.cameraRotation.HasValue)
        {
            state.cameraRotation = input.cameraRotation.Value;
        }
    }

    protected override void GetFinalInput(ref LookInput input)
    {
        // Only the owner sends their camera rotation (world space)
        if (isOwner && _cameraTransform != null)
        {
            input.cameraRotation = _cameraTransform.rotation;
        }
    }

    protected override void UpdateView(LookState viewState, LookState? verified)
    {
        // Use networked camera rotation from state (world space)
        Quaternion cameraRotation = viewState.cameraRotation;

        if (!_offsetCaptured) return;

        // Apply camera rotation with offset to gun (world space)
        if (_gunTransform != null)
        {
            // Gun's world rotation = Camera's world rotation * offset
            _gunTransform.rotation = cameraRotation * _gunRotationOffset;
        }
    }

    private void LateUpdate()
    {
        // Apply head rotation in LateUpdate to run after animation systems
        if (_headTransform != null && _offsetCaptured)
        {
            // Head's world rotation = Camera's world rotation * offset
            Quaternion newRotation = currentState.cameraRotation * _headRotationOffset;
            _headTransform.rotation = newRotation;
        }
    }

    public struct LookInput : IPredictedData<LookInput>
    {
        public Quaternion? cameraRotation;

        public void Dispose()
        {
        }
    }

    public struct LookState : IPredictedData<LookState>
    {
        public Quaternion cameraRotation;

        public void Dispose()
        {
        }
    }
}
