using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [SerializeField] private Transform _directionSource;
    [SerializeField] private float _force = 15f;

    private Vector3 LaunchImpulse => _directionSource.up * _force;

    private void OnTriggerEnter(Collider other)
    {
        //if (other.TryGetComponent(out PlayerManualMovement movement))
            // movement.QueueLaunchImpulse(LaunchImpulse); // TODO: update to new movement API
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_directionSource == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(_directionSource.position, _directionSource.up * 2f);
    }
#endif
}
