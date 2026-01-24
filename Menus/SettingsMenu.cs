using UnityEngine;
using UnityEngine.InputSystem;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private InputActionReference _escapeAction;
    [SerializeField] private GameObject _menuObject;

    private void OnEnable()
    {
        if (_escapeAction != null)
        {
            _escapeAction.action.performed += OnEscapePressed;
            _escapeAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (_escapeAction != null)
        {
            _escapeAction.action.performed -= OnEscapePressed;
            _escapeAction.action.Disable();
        }
    }

    private void OnEscapePressed(InputAction.CallbackContext context)
    {
        if (_menuObject == null) return;

        bool stateToSetTo = !_menuObject.activeSelf;

        _menuObject.SetActive(stateToSetTo);
        LockActionMap.i.ModifyLockList(ActionMapType.PlayerControls, stateToSetTo, this);
        ClientGame.ModifyCursorUnlockList(stateToSetTo, this);
    }
}
