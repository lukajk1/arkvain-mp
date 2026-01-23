using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class HealthCanvas : MonoBehaviour
{
    [SerializeField] private Transform _cam;

    private void LateUpdate()
    {
        if (!_cam) return;

        Vector3 dir = _cam.position - transform.position;
        dir.x = 0;
        transform.rotation = Quaternion.LookRotation(dir);
    }
}
