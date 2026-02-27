using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EscapeMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject _menuObject;

    [Header("Buttons")]
    [SerializeField] private Button _returnToGameButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _leaveMatchButton;
    [SerializeField] private Button _quitToDesktopButton;

    private void Start()
    {
        _menuObject.SetActive(false);
    }

    private void OnEnable()
    {
        // Subscribe to escape key input
        InputManager.Instance.UI.Escape.performed += OnEscapePressed;

        // Subscribe to button clicks
        if (_returnToGameButton != null)
            _returnToGameButton.onClick.AddListener(OnReturnToGame);

        if (_settingsButton != null)
            _settingsButton.onClick.AddListener(OnSettings);

        if (_leaveMatchButton != null)
            _leaveMatchButton.onClick.AddListener(OnLeaveMatch);

        if (_quitToDesktopButton != null)
            _quitToDesktopButton.onClick.AddListener(OnQuitToDesktop);
    }

    private void OnDisable()
    {
        // Unsubscribe from escape key input
        InputManager.Instance.UI.Escape.performed -= OnEscapePressed;

        // Unsubscribe from button clicks
        if (_returnToGameButton != null)
            _returnToGameButton.onClick.RemoveListener(OnReturnToGame);

        if (_settingsButton != null)
            _settingsButton.onClick.RemoveListener(OnSettings);

        if (_leaveMatchButton != null)
            _leaveMatchButton.onClick.RemoveListener(OnLeaveMatch);

        if (_quitToDesktopButton != null)
            _quitToDesktopButton.onClick.RemoveListener(OnQuitToDesktop);
    }

    private void OnEscapePressed(InputAction.CallbackContext context)
    {
        Debug.Log("escape called");

        if (_menuObject == null) return;

        bool stateToSetTo = !_menuObject.activeSelf;

        _menuObject.SetActive(stateToSetTo);
        LockActionMap.i.ModifyLockList(ActionMapType.PlayerControls, stateToSetTo, this);
        ClientGame.ModifyCursorUnlockList(stateToSetTo, this);
    }

    private void OnReturnToGame()
    {
        if (_menuObject != null)
        {
            _menuObject.SetActive(false);
            LockActionMap.i.ModifyLockList(ActionMapType.PlayerControls, false, this);
            ClientGame.ModifyCursorUnlockList(false, this);
        }
    }

    private void OnSettings()
    {
        // TODO: Open settings menu
        Debug.Log("Settings button clicked");
    }

    private void OnLeaveMatch()
    {
        // TODO: Implement leave match functionality
        Debug.Log("Leave match button clicked");
    }

    private void OnQuitToDesktop()
    {
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
