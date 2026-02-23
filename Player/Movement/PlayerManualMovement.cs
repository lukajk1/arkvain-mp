using UnityEngine;
using PurrNet.Prediction;

public class PlayerManualMovement : PredictedIdentity<PlayerManualMovement.MoveInput, PlayerManualMovement.State>
{
    public enum MovementState
    {
        Grounded,
        Airborne,
        Jumping
    }

    public MovementState CurrentMovementState { get; private set; } = MovementState.Grounded;

    [Header("Values")]
    [HideInInspector] public float _moveSpeed = 4.2f;
    [SerializeField] private float _acceleration = 20f;
    [SerializeField] private float _airAccelerationMultiplier = 0.5f;
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _planarDamping = 10f;
    [Tooltip("Acceleration multiplier applied whenever there is existing horizontal velocity. 1 = no penalty, 0 = no acceleration while moving. Applied uniformly regardless of direction change angle.")]
    [SerializeField] private float _oppositeAccelMultiplier = 0.25f;

    [Header("Groud")]
    [SerializeField] private float _groundDrag = 8f;
    [SerializeField] private float _groundCheckRadius = 0.2f;
    [SerializeField] private float _jumpCooldown = 0.2f;
    [SerializeField] private float _landCooldown = 0.15f;

    [Header("Slope Handling")]
    [SerializeField] private float _maxSlopeAngle = 40f;
    [SerializeField] private float _slopeStickForce = 80f;
    [SerializeField] private float _slopeStickDuration = 0.15f;
    private RaycastHit _slopeHit;


    [Header("External References")]
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private FirstPersonCamera _camera;
    [SerializeField] public PredictedRigidbody _rigidbody;
    [SerializeField] private CapsuleCollider _capsule;
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

        // SNAP LOGIC: If we were grounded and aren't jumping, check for a slope transition
        if (!isGrounded && state.wasGrounded && state.jumpCooldown <= 0)
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit snapHit, _groundCheckRadius * 2f, _groundMask))
            {
                isGrounded = true;
                // Manually push the position down or apply a heavy burst of downward force
                _rigidbody.AddForce(Vector3.down * _slopeStickForce * 2f, ForceMode.Acceleration);
            }
        }

        Vector3 moveDir = transform.forward * input.moveDirection.y + transform.right * input.moveDirection.x;

        bool onSlope = OnSlope();

        if (isGrounded && onSlope)
        {
            // Use the slope's normal to find the correct "forward" and "right" on the incline
            Vector3 slopeRight = Vector3.Cross(_slopeHit.normal, Vector3.up);
            Vector3 slopeForward = Vector3.Cross(slopeRight, _slopeHit.normal);

            // Reverse logic if moving backward/left
            // Or more simply, project your existing moveDir correctly:
            if (!input.jump)
                moveDir = GetSlopeMoveDirection(moveDir);

            // don't apply gravity cancellation while on slop if jumping
            if (!input.jump)
                _rigidbody.AddForce(-Physics.gravity * _rigidbody.rb.mass);

            if (input.moveDirection.sqrMagnitude > 0 && !input.jump)
                state.slopeStickCooldown = _slopeStickDuration;

            if (state.slopeStickCooldown > 0 && !input.jump)
                _rigidbody.AddForce(-_slopeHit.normal * _slopeStickForce); // Stick TO the normal, not just down

        }

        if (isGrounded && input.moveDirection.sqrMagnitude == 0 && !input.jump)
            _rigidbody.velocity = new Vector3(_rigidbody.velocity.x, 0f, _rigidbody.velocity.z);

        Vector3 targetVel = moveDir * _moveSpeed;
        float accel = isGrounded ? _acceleration : _acceleration * _airAccelerationMultiplier;

        if (moveDir.sqrMagnitude > 0.001f)
        {
            var curHorizontal = new Vector3(_rigidbody.linearVelocity.x, 0, _rigidbody.linearVelocity.z);
            if (curHorizontal.sqrMagnitude > 0.001f)
            {
                accel *= _oppositeAccelMultiplier;
            }
        }

        _rigidbody.AddForce(targetVel * accel);

        var horizontal = new Vector3(_rigidbody.linearVelocity.x, 0, _rigidbody.linearVelocity.z);
        _rigidbody.AddForce(-horizontal * _planarDamping);
        if (isGrounded)
            _rigidbody.AddForce(-horizontal * _groundDrag);
        if (horizontal.magnitude > _moveSpeed)
        {
            _rigidbody.velocity = new Vector3(targetVel.x, _rigidbody.velocity.y, targetVel.z);
        }

        // Detect landing: was airborne last tick, grounded now
        if (!state.wasGrounded && isGrounded)
        {
            Debug.Log($"[PlayerManualMovement] Landing detected! Invoking _onLand event. wasGrounded: {state.wasGrounded}, isGrounded: {isGrounded}");
            state.movementState = MovementState.Grounded;
            _onLand.Invoke();
        }

        if (input.jump && isGrounded && state.jumpCooldown <= 0)
        {
            state.jumpCooldown = _jumpCooldown;
            // Don't set landCooldown here - let it be set only when actually landing
            state.movementState = MovementState.Jumping;
            _rigidbody.velocity = new Vector3(_rigidbody.velocity.x, 0f, _rigidbody.velocity.z);
            _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
            _onJump.Invoke();
            //Debug.Log("jump called in movement");
        }

        // Transition Jumping -> Airborne once we leave the ground
        if (state.movementState == MovementState.Jumping && !isGrounded)
            state.movementState = MovementState.Airborne;

        // If grounded and not mid-jump, ensure state reflects that
        if (isGrounded && state.movementState != MovementState.Jumping)
            state.movementState = MovementState.Grounded;
        else if (!isGrounded && state.movementState == MovementState.Grounded)
            state.movementState = MovementState.Airborne;

        CurrentMovementState = state.movementState;

        if (input.blinkDirection != Vector3.zero && !state.blinkConsumed && _capsule != null)
        {
            state.blinkConsumed = true;

            float skinWidth = 0.1f;
            Vector3 capsuleCenter = _rigidbody.position + _capsule.center;
            float halfHeight = Mathf.Max(0f, _capsule.height * 0.5f - _capsule.radius);
            Vector3 castOriginOffset = -input.blinkDirection * skinWidth;
            Vector3 point1 = capsuleCenter + Vector3.up * halfHeight + castOriginOffset;
            Vector3 point2 = capsuleCenter - Vector3.up * halfHeight + castOriginOffset;

            float travelDistance = Physics.CapsuleCast(point1, point2, _capsule.radius, input.blinkDirection, out RaycastHit blinkHit, input.blinkDistance + skinWidth, _groundMask, QueryTriggerInteraction.Ignore)
                ? Mathf.Max(0f, blinkHit.distance - skinWidth)
                : input.blinkDistance;

            _rigidbody.position = _rigidbody.position + input.blinkDirection * travelDistance;
            _rigidbody.velocity = Vector3.zero;
        }
        else if (input.blinkDirection == Vector3.zero)
        {
            state.blinkConsumed = false;
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
        if (Physics.Raycast(transform.position, Vector3.down, out _slopeHit, _groundCheckRadius, _groundMask))
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

    private (Vector3 direction, float distance)? _pendingBlink;

    public void QueueBlink(Vector3 direction, float distance)
    {
        _pendingBlink = (direction, distance);
    }

    // this will call every frame
    protected override void UpdateInput(ref MoveInput input)
    {
        // if a or/(|=) b, a = true
        input.jump |= InputManager.Instance.Player.Jump.IsPressed();

        if (_pendingBlink.HasValue)
        {
            input.blinkDirection = _pendingBlink.Value.direction;
            input.blinkDistance = _pendingBlink.Value.distance;
            _pendingBlink = null;
        }
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
            input.cameraForward = input.cameraForward.Value.normalized;
        }
    }

    // prevent csp from operating on things like jumping and shooting, to prevent noticeable rubberbanding
    protected override void ModifyExtrapolatedInput(ref MoveInput input)
    {
        input.jump = false;
        input.blinkDirection = Vector3.zero;
        input.blinkDistance = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, _groundCheckRadius);

        // slope raycast
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, Vector3.down * _groundCheckRadius);
    }

    public struct State : IPredictedData<State>
    {
        public float jumpCooldown;
        public bool wasGrounded;
        public float landCooldown;
        public float slopeStickCooldown;
        public MovementState movementState;
        public bool blinkConsumed;

        public void Dispose() { }
    }

    public struct MoveInput : IPredictedData
    {
        public Vector2 moveDirection;
        public Vector3? cameraForward;
        public bool jump;
        public Vector3 blinkDirection;
        public float blinkDistance;

        public void Dispose() { }
    }

}
