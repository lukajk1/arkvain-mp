using UnityEngine;

public class CanvasLookAtCamera : MonoBehaviour
{
    private Transform _cachedCameraTransform;
    private void LateUpdate()
    {
        // Cache the camera transform reference
        if (_cachedCameraTransform == null)
        {
            if (ClientGame._mainCamera == null) return;
            _cachedCameraTransform = ClientGame._mainCamera.transform;
        }

        Vector3 dir = _cachedCameraTransform.position - transform.position;
        dir.y = 0;

        if (dir.sqrMagnitude > 0.0001f) // Only rotate if there's meaningful distance
        {
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }

}
