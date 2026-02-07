using UnityEngine;
using PurrNet.Prediction;

public class PlayerMovementPhysics : PredictedIdentity<PlayerMovementPhysics.MoveInput, PlayerMovementPhysics.State>
{
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 4.2f;
    [SerializeField] private float _groundDrag = 5f;
    [SerializeField] private float _airMultiplier = 0.4f;

    [Header("Jump")]
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _jumpCooldown = 0.2f;
    [SerializeField] private float _landCooldown = 0.15f;

    [Header("Ground Check")]
    [SerializeField] private float _playerHeight = 2f;
    [SerializeField] private float _groundCheckDistance = 0.3f;
    [SerializeField] private LayerMask _groundMask;

    [SerializeField] private FirstPersonCamera _camera;
    [SerializeField] private PredictedRigidbody _rigidbody;

    [HideInInspector] public PredictedEvent _onJump;
    [HideInInspector] public PredictedEvent _onLand;

    protected override void LateAwake()
    {
        base.LateAwake();

        _onJump = new PredictedEvent(predictionManager, this);
        _onLand = new PredictedEvent(predictionManager, this);

        if (isOwner)
        {
            _camera.Init();
            Debug.Log("initalize camera" + _camera.gameObject.GetInstanceID());
        }
        else
        {
            Destroy(_camera.gameObject);
            Debug.Log("spawned char is not locally controlled. destroyed camera");
        }
    }

    protected override void Simulate(PlayerMovementPhysics.MoveInput input, ref State state, float delta)
    {
        state.jumpCooldown -= delta;
        state.landCooldown -= delta;

        bool isGrounded = IsGrounded();

        Vector3 moveDirection = (transform.forward * input.moveDirection.y + transform.right * input.moveDirection.x);

        if (isGrounded)
        {
            _rigidbody.AddForce(moveDirection.normalized * _moveSpeed * 10f, ForceMode.Force);

            var horizontal = new Vector3(_rigidbody.linearVelocity.x, 0, _rigidbody.linearVelocity.z);
            _rigidbody.AddForce(-horizontal * _groundDrag);
        }
        else
        {
            _rigidbody.AddForce(moveDirection.normalized * _moveSpeed * 10f * _airMultiplier, ForceMode.Force);
        }

        SpeedControl();

        if (!state.wasGrounded && isGrounded && state.landCooldown <= 0)
        {
            _onLand.Invoke();
            state.landCooldown = _landCooldown;
        }

        if (input.jump && isGrounded && state.jumpCooldown <= 0)
        {
            state.jumpCooldown = _jumpCooldown;
            state.landCooldown = _landCooldown;

            Vector3 currentVelocity = _rigidbody.linearVelocity;
            _rigidbody.velocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

            _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
            _onJump.Invoke();
        }

        state.wasGrounded = isGrounded;

        if (input.cameraForward.HasValue)
        {
            var camForward = input.cameraForward.Value;
            camForward.y = 0;
            if (camForward.sqrMagnitude > 0.0001f)
                _rigidbody.MoveRotation(Quaternion.LookRotation(camForward.normalized));
        }
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);

        if (flatVel.magnitude > _moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * _moveSpeed;
            _rigidbody.velocity = new Vector3(limitedVel.x, _rigidbody.linearVelocity.y, limitedVel.z);
        }
    }

    public bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, _playerHeight * 0.5f + _groundCheckDistance, _groundMask);
    }

    protected override void UpdateInput(ref MoveInput input)
    {
        input.jump |= InputManager.Instance.Player.Jump.IsPressed();
    }

    protected override void GetFinalInput(ref MoveInput input)
    {
        input.moveDirection = InputManager.Instance.Player.Move.ReadValue<UnityEngine.Vector2>();
        input.cameraForward = _camera.forward;
    }

    protected override void SanitizeInput(ref MoveInput input)
    {
        if (input.moveDirection.magnitude > 1)
        {
            input.moveDirection.Normalize();
        }
        if (input.cameraForward.HasValue)
        {
            input.cameraForward.Value.Normalize();
        }
    }

    protected override void ModifyExtrapolatedInput(ref MoveInput input)
    {
        input.jump = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * (_playerHeight * 0.5f + _groundCheckDistance));
    }

    public struct State : IPredictedData<State>
    {
        public float jumpCooldown;
        public bool wasGrounded;
        public float landCooldown;

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
