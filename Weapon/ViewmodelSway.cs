using UnityEngine;

/// <summary>
/// Handles viewmodel sway and bobbing for weapons.
/// Applied to weapon viewmodels to add natural movement feedback.
/// </summary>
public class ViewmodelSway : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement _playerMovement;

    [Header("Feature Toggles")]
    [Tooltip("Turn positional sway (on camera movement) on/off")]
    [SerializeField] private bool _enablePositionSway = true;
    [Tooltip("Turn rotational sway (on camera movement) on/off")]
    [SerializeField] private bool _enableRotationSway = true;
    [Tooltip("Turn positional bobbing (walking movement) on/off")]
    [SerializeField] private bool _enablePositionBob = true;
    [Tooltip("Turn rotational bobbing (walking movement) on/off")]
    [SerializeField] private bool _enableRotationBob = true;

    [Header("Sway")]
    [SerializeField] private float _swayStep = 0.01f;
    [SerializeField] private float _maxSwayDistance = 0.06f;
    private Vector3 _swayPos;

    [Header("Sway Rotation")]
    [SerializeField] private float _rotationStep = 4f;
    [SerializeField] private float _maxRotationStep = 5f;
    private Vector3 _swayEulerRot;

    [Header("Smoothing")]
    [SerializeField] private float _positionSmooth = 10f;
    [SerializeField] private float _rotationSmooth = 12f;

    [Header("Bobbing")]
    [Tooltip("Higher values make the bob motion snap faster at peaks (0 = smooth sine wave)")]
    [SerializeField] private float _bobSharpness = 2f;
    private float _speedCurve;
    private float CurveSin => Mathf.Pow(Mathf.Abs(Mathf.Sin(_speedCurve)), _bobSharpness) * Mathf.Sign(Mathf.Sin(_speedCurve));
    private float CurveCos => Mathf.Pow(Mathf.Abs(Mathf.Cos(_speedCurve)), _bobSharpness) * Mathf.Sign(Mathf.Cos(_speedCurve));

    [SerializeField] private Vector3 _travelLimit = Vector3.one * 0.025f;
    [SerializeField] private Vector3 _bobLimit = Vector3.one * 0.01f;
    private Vector3 _bobPosition;

    [SerializeField] private float _bobExaggeration = 1f;

    [Header("Bob Rotation")]
    [SerializeField] private Vector3 _bobRotationMultiplier = new Vector3(1f, 1f, 1f);
    private Vector3 _bobEulerRotation;

    // Current viewmodel being animated
    private Transform _currentViewmodel;
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;

    // Input tracking
    private Vector2 _walkInput;
    private Vector2 _lookInput;

    private void Update()
    {
        // Only update if we have a valid viewmodel and player movement reference
        if (_currentViewmodel == null || _playerMovement == null)
            return;

        GetInput();

        if (_enablePositionSway)
            CalculateSway();
        else
            _swayPos = Vector3.zero;

        if (_enableRotationSway)
            CalculateSwayRotation();
        else
            _swayEulerRot = Vector3.zero;

        if (_enablePositionBob)
            CalculateBobOffset();
        else
            _bobPosition = Vector3.zero;

        if (_enableRotationBob)
            CalculateBobRotation();
        else
            _bobEulerRotation = Vector3.zero;

        ApplyPositionAndRotation();
    }

    /// <summary>
    /// Sets the viewmodel transform to apply sway/bob to.
    /// Called by WeaponManager when switching weapons.
    /// </summary>
    public void SetViewmodel(Transform viewmodel)
    {
        // Reset previous viewmodel to its initial position before switching
        if (_currentViewmodel != null)
        {
            _currentViewmodel.localPosition = _initialPosition;
            _currentViewmodel.localRotation = _initialRotation;
        }

        _currentViewmodel = viewmodel;

        // Store initial position and rotation when switching viewmodels
        if (_currentViewmodel != null)
        {
            _initialPosition = _currentViewmodel.localPosition;
            _initialRotation = _currentViewmodel.localRotation;

            // Reset sway/bob state
            _swayPos = Vector3.zero;
            _bobPosition = Vector3.zero;
            _swayEulerRot = Vector3.zero;
            _bobEulerRotation = Vector3.zero;
        }
    }

    private void GetInput()
    {
        // Get movement input
        Vector2 moveInput = InputManager.Instance.Player.Move.ReadValue<Vector2>();
        _walkInput = moveInput.normalized;

        // Get look input (mouse delta)
        Vector2 lookDelta = InputManager.Instance.Player.Look.ReadValue<Vector2>();
        _lookInput = lookDelta;
    }

    private void CalculateSway()
    {
        Vector3 invertLook = _lookInput * -_swayStep;
        invertLook.x = Mathf.Clamp(invertLook.x, -_maxSwayDistance, _maxSwayDistance);
        invertLook.y = Mathf.Clamp(invertLook.y, -_maxSwayDistance, _maxSwayDistance);

        _swayPos = invertLook;
    }

    private void CalculateSwayRotation()
    {
        Vector2 invertLook = _lookInput * -_rotationStep;
        invertLook.x = Mathf.Clamp(invertLook.x, -_maxRotationStep, _maxRotationStep);
        invertLook.y = Mathf.Clamp(invertLook.y, -_maxRotationStep, _maxRotationStep);
        _swayEulerRot = new Vector3(invertLook.y, invertLook.x, invertLook.x);
    }

    private void ApplyPositionAndRotation()
    {
        Vector3 targetPosition = _initialPosition + (_swayPos + _bobPosition);
        _currentViewmodel.localPosition = Vector3.Lerp(
            _currentViewmodel.localPosition,
            targetPosition,
            Time.deltaTime * _positionSmooth
        );

        // Apply sway and bob rotation relative to initial rotation
        Quaternion targetRotation = _initialRotation * Quaternion.Euler(_swayEulerRot) * Quaternion.Euler(_bobEulerRotation);
        _currentViewmodel.localRotation = Quaternion.Slerp(
            _currentViewmodel.localRotation,
            targetRotation,
            Time.deltaTime * _rotationSmooth
        );
    }

    private void CalculateBobOffset()
    {
        // Read from PlayerMovement's predicted state (interpolated view state)
        bool isGrounded = _playerMovement.currentState.isGrounded;
        float movementMagnitude = Mathf.Abs(_walkInput.x) + Mathf.Abs(_walkInput.y);

        _speedCurve += Time.deltaTime * (isGrounded ? movementMagnitude * _bobExaggeration : 1f) + 0.01f;

        // Wrap speed curve to prevent indefinite growth (sin/cos repeat every 2π)
        if (_speedCurve > Mathf.PI * 2f)
        {
            _speedCurve -= Mathf.PI * 2f;
        }

        _bobPosition.x = (CurveCos * _bobLimit.x * (isGrounded ? 1 : 0)) - (_walkInput.x * _travelLimit.x);
        _bobPosition.y = (CurveSin * _bobLimit.y) - (_walkInput.y * _travelLimit.y);
        // Z-axis back and forth bobbing when moving in any direction
        _bobPosition.z = CurveSin * _bobLimit.z * (isGrounded && movementMagnitude > 0 ? 1 : 0);
    }

    private void CalculateBobRotation()
    {
        bool isMoving = _walkInput != Vector2.zero;

        _bobEulerRotation.x = isMoving
            ? _bobRotationMultiplier.x * Mathf.Sin(2 * _speedCurve)
            : _bobRotationMultiplier.x * Mathf.Sin(2 * _speedCurve) / 2;

        _bobEulerRotation.y = isMoving
            ? _bobRotationMultiplier.y * CurveCos
            : 0;

        _bobEulerRotation.z = isMoving
            ? _bobRotationMultiplier.z * CurveCos * _walkInput.x
            : 0;
    }
}
