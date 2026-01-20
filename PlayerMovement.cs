using UnityEngine;
using PurrNet.Prediction;

public class PlayerMovement : PredictedIdentity<PlayerMovement.MoveInput, PlayerMovement.State>
{
    [SerializeField] private float _moveSpeed = 7f;
    [SerializeField] private float _acceleration = 20f;
    [SerializeField] private float _jumpForce = 7f;
    [SerializeField] private float _planarDamping = 10f;

    [SerializeField] private FirstPersonCamera _camera;
    [SerializeField] private PredictedRigidbody _rigidbody;
    protected override void LateAwake()
    {
        if (isOwner) _camera.Init();
    }
    protected override void Simulate(PlayerMovement.MoveInput input, ref State state, float delta)
    {
        // simulate is the local simulation/interpolation that occurs between packets from the server ig
        // all operations pertaining to state/input should pass through the 'interfaces' referenced by state and input here i.e. don't call input.get... in here
        
        Vector3 targetVel = (transform.forward * input.moveDirection.y + transform.right * input.moveDirection.x) * _moveSpeed;
        _rigidbody.AddForce(targetVel * _acceleration);
        
        var horizontal = new Vector3(_rigidbody.linearVelocity.x, 0, _rigidbody.linearVelocity.z);
        _rigidbody.AddForce(-horizontal * _planarDamping);
        if (horizontal.magnitude > _moveSpeed)
        {
            _rigidbody.velocity = new Vector3(targetVel.x, _rigidbody.velocity.y, targetVel.z);
        }

        if (input.jump) _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);

        if (input.cameraForward.HasValue)
        {
            var camForward = input.cameraForward.Value;
            camForward.y = 0;
            if (camForward.sqrMagnitude > 0.0001f)
                _rigidbody.MoveRotation(Quaternion.LookRotation(camForward.normalized));
        }

    }

    // this will call every frame
    protected override void UpdateInput(ref MoveInput input)
    {
        // if a or/(|=) b, a = true
        input.jump |= Input.GetKeyDown(KeyCode.Space);

    }

    // this runs each tick (as opposed to each frame)
    protected override void GetFinalInput(ref MoveInput input)
    {
        input.moveDirection = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        input.cameraForward = _camera.forward;
    }

    protected override void SanitizeInput(ref MoveInput input)
    {
        if (input.moveDirection.magnitude > 1)
        {
            input.moveDirection.Normalize();
        }
    }

    public struct State : IPredictedData<State>
    {
        public void Dispose() { }
    }

    public struct MoveInput : IPredictedData
    {
        public Vector2 moveDirection;
        public Vector3? cameraForward;
        public bool jump;

        public void Dispose() { }
    }

}
