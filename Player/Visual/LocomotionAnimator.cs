using PurrNet.Prediction;
using UnityEngine;

public class LocomotionAnimator : StatelessPredictedIdentity
{
    [SerializeField] private Animator _animator;
    [SerializeField] private PlayerMovement _movement;
    [SerializeField] private PredictedRigidbody _rigidbody;

    protected override void LateAwake()
    {
        base.LateAwake();

        _movement._onJump.AddListener(OnJump);
        _movement._onLand.AddListener(OnLand);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_movement != null)
        {
            _movement._onJump.RemoveListener(OnJump);
            _movement._onLand.RemoveListener(OnLand);
        }
    }

    private void OnJump()
    {
        _animator.SetBool("isGrounded", false);
        //Debug.Log("jumped" + gameObject.GetInstanceID());

    }

    private void OnLand()
    {
        _animator.SetBool("isGrounded", true);
        //Debug.Log("landed" + gameObject.GetInstanceID());
    }

    private void Update()
    {
        //Debug.Log("isGrounded animator:" + _animator.GetBool("isGrounded"));
        if (_movement.isOwner)
        {
            // Local player: Use predicted input for instant response (already normalized -1 to 1)
            Vector2 moveInput = _movement.currentInput.moveDirection;
            float speed = moveInput.magnitude;

            _animator.SetFloat("input_x", moveInput.x);
            _animator.SetFloat("input_y", moveInput.y);
            //_animator.SetFloat("speed", speed);
        }
        else
        {
            // Remote players: Use actual velocity (interpolated by PurrNet)
            Vector3 velocity = _rigidbody.linearVelocity;
            Vector3 localVelocity = transform.InverseTransformDirection(velocity);

            // Normalize velocity to -1 to 1 range based on max speed
            float normalizedX = Mathf.Clamp(localVelocity.x / _movement._moveSpeed, -1f, 1f);
            float normalizedZ = Mathf.Clamp(localVelocity.z / _movement._moveSpeed, -1f, 1f);
            float speed = new Vector2(localVelocity.x, localVelocity.z).magnitude / _movement._moveSpeed;

            _animator.SetFloat("input_x", normalizedX);
            _animator.SetFloat("input_y", normalizedZ);
            //_animator.SetFloat("speed", speed);
        }
    }
}
