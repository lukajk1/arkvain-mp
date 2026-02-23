using UnityEngine;
using PurrNet.Prediction;

public class NewLookTargetSync : PredictedIdentity<NewLookTargetSync.LookInput, NewLookTargetSync.LookState>
{
    [SerializeField] private FirstPersonCamera _camera;
    [SerializeField] private Transform _gunTransform;
    [SerializeField] private Transform _debugObj;

    private Transform _cameraTransform;
    private Quaternion _initialGunRotation;
    private Quaternion _initialDebugRotation;

    protected override void Simulate(LookInput input, ref LookState state, float delta)
    {
        // Cache camera transform and initial rotations
        if (_cameraTransform == null && _camera != null)
        {
            _cameraTransform = _camera.transform;
        }

        if (_initialGunRotation == Quaternion.identity && _gunTransform != null)
        {
            _initialGunRotation = _gunTransform.localRotation;
        }

        if (_initialDebugRotation == Quaternion.identity && _debugObj != null)
        {
            _initialDebugRotation = _debugObj.localRotation;
        }
    }

    protected override void GetFinalInput(ref LookInput input)
    {
        // Not needed - we only care about rotation
    }

    protected override void UpdateView(LookState viewState, LookState? verified)
    {
        if (_cameraTransform == null) return;

        // Get camera's local X rotation (pitch)
        float cameraPitch = _cameraTransform.localEulerAngles.x;

        // Match gun's local X rotation to camera's pitch (up/down rotation)
        if (_gunTransform != null && _initialGunRotation != Quaternion.identity)
        {
            // Apply relative to initial rotation
            Vector3 initialEuler = _initialGunRotation.eulerAngles;
            Vector3 newRotation = initialEuler;
            newRotation.x = cameraPitch;
            _gunTransform.localRotation = Quaternion.Euler(newRotation);
        }

        // Apply same operations to debug object
        if (_debugObj != null && _initialDebugRotation != Quaternion.identity)
        {
            // Apply relative to initial rotation
            Vector3 initialEuler = _initialDebugRotation.eulerAngles;
            Vector3 newRotation = initialEuler;
            newRotation.x = cameraPitch;
            _debugObj.localRotation = Quaternion.Euler(newRotation);
        }
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
