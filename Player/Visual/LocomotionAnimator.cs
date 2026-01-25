using PurrNet.Prediction;
using UnityEngine;

public class LocomotionAnimator : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private PlayerMovement _movement;
    [SerializeField] private PredictedRigidbody _rigidbody;

    private void Update()
    {
        if (_movement.isOwner)
        {
            // Local player: Use predicted input for instant response (already normalized -1 to 1)
            Vector2 moveInput = _movement.currentInput.moveDirection;
            float speed = moveInput.magnitude;

            //Debug.Log($"[OWNER] Setting animator - input_x: {moveInput.x}, input_y: {moveInput.y}, speed: {speed}");

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

            //Debug.Log($"[REMOTE] Setting animator - input_x: {normalizedX}, input_y: {normalizedZ}, speed: {speed}");

            _animator.SetFloat("input_x", normalizedX);
            _animator.SetFloat("input_y", normalizedZ);
            //_animator.SetFloat("speed", speed);
        }
    }
}
