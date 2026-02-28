using UnityEngine;

public class CanvasLookAtCamera : MonoBehaviour
{
    private Transform _cachedCameraTransform;
    private void LateUpdate()
    {
        // Cache the camera transform reference
        if (_cachedCameraTransform == null)
        {
            // Check if camera exists and is not destroyed
            if (ClientsideGameManager._mainCamera == null || !ClientsideGameManager._mainCamera)
            {
                // Try to find and register a camera in the scene
                Camera foundCamera = Camera.main;
                if (foundCamera == null)
                {
                    // Fallback: find any enabled camera
                    foundCamera = FindFirstObjectByType<Camera>();
                }

                if (foundCamera != null && ClientsideGameManager.Instance != null)
                {
                    ClientsideGameManager.Instance.RegisterMainCamera(foundCamera);
                    //Debug.Log($"[CanvasLookAtCamera] Found and registered camera: {foundCamera.name}");
                }
                else
                {
                    return;
                }
            }
            _cachedCameraTransform = ClientsideGameManager._mainCamera.transform;
        }

        Vector3 dir = _cachedCameraTransform.position - transform.position;
        dir.y = 0;

        if (dir.sqrMagnitude > 0.0001f) // Only rotate if there's meaningful distance
        {
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }

}
