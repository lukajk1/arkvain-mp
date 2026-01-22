using UnityEngine;

public class FirstPersonCamera : MonoBehaviour
{
    [SerializeField] float _lookSensitivity = 2f;
    [SerializeField] float _maxLookAngle = 80f;
    [SerializeField] Camera _camera;

    Vector2 _currentRotation;
    bool _initialized;

    public Vector3 forward => Quaternion.Euler(_currentRotation.x, _currentRotation.y, 0) * Vector3.forward;

    void Awake()
    {
        _camera.enabled = false;
    }

    public void Init()
    {
        _initialized = true;
        _camera.enabled = true;
        if (GetComponent<AudioListener>() == null)
            gameObject.AddComponent<AudioListener>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (!_initialized) return;

        float mouseX = Input.GetAxis("Mouse X") * _lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * _lookSensitivity;

        _currentRotation.x = Mathf.Clamp(_currentRotation.x - mouseY, -_maxLookAngle, _maxLookAngle);
        _currentRotation.y += mouseX;

        transform.localRotation = Quaternion.Euler(_currentRotation.x, 0f, 0f);
    }
}