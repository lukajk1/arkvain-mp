using UnityEngine;
using PurrNet.Prediction;

public class PlayerMovement : PredictedIdentity<PlayerMovement.MoveInput, PlayerMovement.State>
{
    [HideInInspector] public float _moveSpeed = 4.2f;
    [SerializeField] private float _acceleration = 20f;
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _planarDamping = 10f;
    [SerializeField] private float _groundCheckRadius = 0.2f;
    [SerializeField] private float _jumpCooldown = 0.2f;
    [SerializeField] private float _landCooldown = 0.15f;
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


    protected override void Simulate(PlayerMovement.MoveInput input, ref State state, float delta)
    {
        // simulate is the local simulation/interpolation that occurs between packets from the server ig
        // all operations pertaining to state/input should pass through the 'interfaces' referenced by state and input here i.e. don't call input.get... in here

        // delta = time in ms since last tick I guess
        state.jumpCooldown -= delta;
        state.landCooldown -= delta;

        Vector3 targetVel = (transform.forward * input.moveDirection.y + transform.right * input.moveDirection.x) * _moveSpeed;
        _rigidbody.AddForce(targetVel * _acceleration);

        var horizontal = new Vector3(_rigidbody.linearVelocity.x, 0, _rigidbody.linearVelocity.z);
        _rigidbody.AddForce(-horizontal * _planarDamping);
        if (horizontal.magnitude > _moveSpeed)
        {
            _rigidbody.velocity = new Vector3(targetVel.x, _rigidbody.velocity.y, targetVel.z);
        }

        bool isGrounded = IsGrounded();

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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _groundCheckRadius);  
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
