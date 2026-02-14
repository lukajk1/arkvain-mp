using UnityEngine;
using PurrNet.Prediction;

public class PlayerManualMovement : PredictedIdentity<PlayerManualMovement.MoveInput, PlayerManualMovement.State>
{
    [Header("Values")]
    [HideInInspector] public float _moveSpeed = 4.2f;
    [SerializeField] private float _acceleration = 20f;
    [SerializeField] private float _airAccelerationMultiplier = 0.5f;
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _planarDamping = 10f;
    [SerializeField] private float _groundDrag = 8f;
    [SerializeField] private float _groundCheckRadius = 0.2f;
    [SerializeField] private float _jumpCooldown = 0.2f;
    [SerializeField] private float _landCooldown = 0.15f;

    [Header("Slope Handling")]
    [SerializeField] private float _maxSlopeAngle = 40f;
    [SerializeField] private float _slopeRaycastDist = 0.5f;
    [SerializeField] private float _slopeStickForce = 80f;
    [SerializeField] private float _slopeStickDuration = 0.15f;
    private RaycastHit _slopeHit;


    [Header("Ext References")]
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private FirstPersonCamera _camera;
    [SerializeField] public PredictedRigidbody _rigidbody;
    //events
    [HideInInspector] public PredictedEvent _onJump;
    [HideInInspector] public PredictedEvent _onLand;


    protected override void LateAwake()
    {
        base.LateAwake();

        _onJump = new PredictedEvent(predictionManager, this);
        _onLand = new PredictedEvent(predictionManager, this);
    }


    protected override void Simulate(PlayerManualMovement.MoveInput input, ref State state, float delta)
    {
        state.jumpCooldown -= delta;
        state.landCooldown -= delta;
        state.slopeStickCooldown -= delta;

        bool isGrounded = IsGrounded();

        Vector3 moveDir = transform.forward * input.moveDirection.y + transform.right * input.moveDirection.x;

        bool onSlope = OnSlope();

        if (isGrounded && onSlope)
        {
            moveDir = GetSlopeMoveDirection(moveDir);

            _rigidbody.AddForce(-Physics.gravity * _rigidbody.rb.mass);

            if (input.moveDirection.sqrMagnitude > 0)
                state.slopeStickCooldown = _slopeStickDuration;

            if (state.slopeStickCooldown > 0)
                _rigidbody.AddForce(Vector3.down * _slopeStickForce);
        }

        Vector3 targetVel = moveDir * _moveSpeed;
        float accel = isGrounded ? _acceleration : _acceleration * _airAccelerationMultiplier;
        _rigidbody.AddForce(targetVel * accel);

        var horizontal = new Vector3(_rigidbody.linearVelocity.x, 0, _rigidbody.linearVelocity.z);
        _rigidbody.AddForce(-horizontal * _planarDamping);
        if (isGrounded)
            _rigidbody.AddForce(-horizontal * _groundDrag);
        if (horizontal.magnitude > _moveSpeed)
        {
            _rigidbody.velocity = new Vector3(targetVel.x, _rigidbody.velocity.y, targetVel.z);
        }

        // Detect landing: was not grounded last tick, but grounded now, and cooldown expired
        if (!state.wasGrounded && isGrounded && state.landCooldown <= 0)
        {
            _onLand.Invoke();
            state.landCooldown = _landCooldown;
        }

        if (input.jump && isGrounded && state.jumpCooldown <= 0)
        {
            state.jumpCooldown = _jumpCooldown;
            state.landCooldown = _landCooldown; // Prevent landing event right after jump
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

    private static Collider[] _groundColliders = new Collider[8];
    public bool IsGrounded()
    {
        var hit = Physics.OverlapSphereNonAlloc(transform.position, _groundCheckRadius, _groundColliders, _groundMask);
        return hit > 0;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out _slopeHit, _slopeRaycastDist, _groundMask))
        {
            float angle = Vector3.Angle(Vector3.up, _slopeHit.normal);
            return angle < _maxSlopeAngle && angle != 0;
        }

        return false;
    }

    private Vector3 GetSlopeMoveDirection(Vector3 moveDir)
    {
        return Vector3.ProjectOnPlane(moveDir, _slopeHit.normal).normalized;
    }

    // this will call every frame
    protected override void UpdateInput(ref MoveInput input)
    {
        // if a or/(|=) b, a = true
        input.jump |= InputManager.Instance.Player.Jump.IsPressed();
    }

    // this runs each tick (as opposed to each frame)
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

    // prevent csp from operating on things like jumping and shooting, to prevent noticeable rubberbanding
    protected override void ModifyExtrapolatedInput(ref MoveInput input)
    {
        input.jump = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, _groundCheckRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, Vector3.down * 0.4f);
    }

    public struct State : IPredictedData<State>
    {
        public float jumpCooldown;
        public bool wasGrounded;
        public float landCooldown;
        public float slopeStickCooldown;

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
