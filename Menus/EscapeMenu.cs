using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EscapeMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private SceneNameHolder _lobbyScene;
    [SerializeField] private Canvas _menu;
    [SerializeField] private SettingsMenu _settingsMenu;

    [Header("Buttons")]
    [SerializeField] private Button _returnToGameButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _leaveMatchButton;
    [SerializeField] private Button _quitToDesktopButton;

    private void Start()
    {
        _menu.gameObject.SetActive(false);

        // Subscribe to escape key input (in Start to ensure InputManager is initialized)
        if (InputManager.Instance != null)
        {
            InputManager.Instance.UI.Escape.performed += OnEscapePressed;
        }

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

    public void SetState(bool value)
    {
        if (_menu != null)
        {
            _menu.gameObject.SetActive(value);
            InputManager.Instance.ModifyPlayerControlsLockList(value, this);
            PersistentClient.ModifyCursorUnlockList(value, this);
        }
    }

    private void OnEnable()
    {
        // Empty - subscriptions now in Start()
    }

    private void OnDisable()
    {
        // Unsubscribe from escape key input
        if (InputManager.Instance != null)
        {
            InputManager.Instance.UI.Escape.performed -= OnEscapePressed;
        }

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
        if (_menu == null) return;

        bool stateToSetTo = !_menu.gameObject.activeSelf;

        SetState(stateToSetTo);
    }

    private void OnReturnToGame()
    {
        SetState(false);
    }

    private void OnSettings()
    {
        if (_settingsMenu == null)
        {
            Debug.LogWarning("Settings menu not assigned!");
            return;
        }

        _settingsMenu.SetState(true);
    }

    private void OnLeaveMatch()
    {
        SetState(false); // necessary to clean up cursor lock modification
        SceneManager.LoadScene(_lobbyScene.sceneName);
    }

    private void OnQuitToDesktop()
    {
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
