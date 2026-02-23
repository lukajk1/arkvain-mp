using UnityEngine;
using PurrNet.Prediction;

public class NewLookTargetSync : PredictedIdentity<NewLookTargetSync.LookInput, NewLookTargetSync.LookState>
{
    [SerializeField] private FirstPersonCamera _camera;
    [SerializeField] private Transform _gunTransform;

    private Transform _cameraTransform;
    private Quaternion _rotationOffset; // Gun's rotation relative to camera at start
    private bool _offsetCaptured = false;

    protected override void LateAwake()
    {
        base.LateAwake();

        // Cache camera transform
        if (_camera != null)
        {
            _cameraTransform = _camera.transform;
            Debug.Log($"[NewLookTargetSync] Cached camera transform");
        }

        // Calculate initial offset: gun's world rotation relative to camera's world rotation
        if (_gunTransform != null && _cameraTransform != null)
        {
            _rotationOffset = Quaternion.Inverse(_cameraTransform.rotation) * _gunTransform.rotation;
            _offsetCaptured = true;
            Debug.Log($"[NewLookTargetSync] Captured offset: {_rotationOffset.eulerAngles}, Gun world: {_gunTransform.rotation.eulerAngles}, Camera world: {_cameraTransform.rotation.eulerAngles}");
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

            if (Time.frameCount % 120 == 0) // Log every 120 frames
            {
                Debug.Log($"[NewLookTargetSync] GetFinalInput - Camera rotation: {_cameraTransform.rotation.eulerAngles}");
            }
        }
    }

    protected override void UpdateView(LookState viewState, LookState? verified)
    {
        // Use networked camera rotation from state (world space)
        Quaternion cameraRotation = viewState.cameraRotation;

        // Apply camera rotation with offset to gun (world space)
        if (_gunTransform != null && _offsetCaptured)
        {
            // Gun's world rotation = Camera's world rotation * offset
            _gunTransform.rotation = cameraRotation * _rotationOffset;
        }
        else if (Time.frameCount % 60 == 0) // Log every 60 frames
        {
            Debug.Log($"[NewLookTargetSync] UpdateView - gunTransform null: {_gunTransform == null}, offset captured: {_offsetCaptured}, cameraRot: {cameraRotation.eulerAngles}");
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
