using UnityEngine;
using UnityEngine.InputSystem;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private GameObject _menuObject;

    private void OnEscapePressed(InputAction.CallbackContext context)
    {
        if (_menuObject == null) return;

        bool stateToSetTo = !_menuObject.activeSelf;

        _menuObject.SetActive(stateToSetTo);
        LockActionMap.i.ModifyLockList(ActionMapType.PlayerControls, stateToSetTo, this);
        ClientGame.ModifyCursorUnlockList(stateToSetTo, this);
    }
}
