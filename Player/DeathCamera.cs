using UnityEngine;
using UnityEngine.InputSystem;

public class DeathCamera : MonoBehaviour
{
    private float _yRotation;
    private InputAction _cachedLookAction;

    public void Start()
    {
        _yRotation = transform.eulerAngles.y;
        _cachedLookAction = InputManager.Instance.Player.Look;
    }

    private void LateUpdate()
    {
        Vector2 lookDelta = _cachedLookAction.ReadValue<Vector2>();

        float physicalDeltaX = lookDelta.x / PersistentClient.playerDPI;
        float inchesPerFullRotation = PersistentClient.cm360 / 2.54f;
        float degreesPerInch = 360f / inchesPerFullRotation;

        _yRotation += physicalDeltaX * degreesPerInch;

        transform.localRotation = Quaternion.Euler(0f, _yRotation, 0f);
    }
}
