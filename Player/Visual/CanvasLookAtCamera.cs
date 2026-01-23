using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class CanvasLookAtCamera : MonoBehaviour
{
    private Transform _cam;

    private void Awake()
    {
        _cam = Camera.main?.transform;
    }

    private void LateUpdate()
    {
        if (!_cam) {
            _cam = Camera.main?.transform;
            if (!_cam) return;
        }

        Vector3 dir = _cam.position - transform.position;
        dir.y = 0;
        transform.rotation = Quaternion.LookRotation(dir);
    }
}
