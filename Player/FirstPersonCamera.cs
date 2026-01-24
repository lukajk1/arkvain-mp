using PurrNet;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonCamera : MonoBehaviour
{
    [SerializeField] float _maxLookAngle = 80f;
    [SerializeField] Camera _camera;
    [SerializeField] InputActionReference _lookAction;

    [HideInInspector] public static Transform mainCameraTransform;
    public static event Action OnCameraInitialized;

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

        if (_lookAction != null)
        {
            _lookAction.action.Enable();
        }

        mainCameraTransform = transform;
        OnCameraInitialized?.Invoke();
    }

    void LateUpdate()
    {
        if (!_initialized) return;

        Vector2 lookDelta = Vector2.zero;
        if (_lookAction != null)
        {
            lookDelta = _lookAction.action.ReadValue<Vector2>();
        }

        // Normalize delta based on mouse DPI to get consistent physical movement
        // Input System gives delta in pixels, so divide by DPI to get physical distance in inches
        float physicalDeltaX = lookDelta.x / ClientGame.playerDPI;
        float physicalDeltaY = lookDelta.y / ClientGame.playerDPI;

        // Convert physical distance (inches) to degrees based on cm/360 setting
        // targetCm360 is how many cm for a 360° turn
        float inchesPerFullRotation = (ClientGame.targetCm360 / 2.54f);
        float degreesPerInch = 360f / inchesPerFullRotation;

        float mouseX = physicalDeltaX * degreesPerInch;
        float mouseY = physicalDeltaY * degreesPerInch;

        _currentRotation.x = Mathf.Clamp(_currentRotation.x - mouseY, -_maxLookAngle, _maxLookAngle);
        _currentRotation.y += mouseX;

        transform.localRotation = Quaternion.Euler(_currentRotation.x, 0f, 0f);
    }
}