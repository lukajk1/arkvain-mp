using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EscapeMenu : MonoBehaviour
{
    public static EscapeMenu Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private SceneNameHolder _lobbyScene;
    [SerializeField] private Canvas _menu;
    [SerializeField] private SettingsMenu _settingsMenu;

    [Header("Buttons")]
    [SerializeField] private Button _returnToGameButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _loadoutButton;
    [SerializeField] private Button _leaveMatchButton;
    [SerializeField] private Button _quitToDesktopButton;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        _menu.gameObject.SetActive(false);

        // Subscribe to escape key input (in Start to ensure InputManager is initialized)
        if (PersistentClient.Instance.inputManager != null)
        {
            PersistentClient.Instance.inputManager.UI.Escape.performed += OnEscapePressed;
        }

        // Subscribe to button clicks
        if (_returnToGameButton != null)
            _returnToGameButton.onClick.AddListener(OnReturnToGame);

        if (_settingsButton != null)
            _settingsButton.onClick.AddListener(OnSettings);

        if (_loadoutButton != null)
            _loadoutButton.onClick.AddListener(OnLoadoutPressed);

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
            PersistentClient.Instance.SetCursorToPlayMode(!value);
        }
    }

    private void OnEnable()
    {
        // Empty - subscriptions now in Start()
    }

    private void OnDisable()
    {
        // Unsubscribe from escape key input
        if (PersistentClient.Instance.inputManager != null)
        {
            PersistentClient.Instance.inputManager.UI.Escape.performed -= OnEscapePressed;
        }

        // Unsubscribe from button clicks
        if (_returnToGameButton != null)
            _returnToGameButton.onClick.RemoveListener(OnReturnToGame);

        if (_settingsButton != null)
            _settingsButton.onClick.RemoveListener(OnSettings);

        if (_loadoutButton != null)
            _loadoutButton.onClick.RemoveListener(OnLoadoutPressed);

        if (_leaveMatchButton != null)
            _leaveMatchButton.onClick.RemoveListener(OnLeaveMatch);

        if (_quitToDesktopButton != null)
            _quitToDesktopButton.onClick.RemoveListener(OnQuitToDesktop);
    }

    private void OnEscapePressed(InputAction.CallbackContext context)
    {
        if (_menu == null) return;

        bool isMenuCurrentlyActive = _menu.gameObject.activeSelf;

        // If the menu is already open, always close it on Escape regardless of context
        if (isMenuCurrentlyActive)
        {
            SetState(false);
            return;
        }

        // If the menu is NOT open, only allow opening if the context is Neutral
        if (PersistentClient.Instance.currentEscapeContext != EscapeContext.Neutral)
        {
            return;
        }

        SetState(true);
    }

    private void OnReturnToGame()
    {
        SetState(false);
    }

    private void OnLoadoutPressed()
    {
        if (LoadoutManager.Instance != null)
        {
            LoadoutManager.Instance.SetState(true);
        }
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
        SetState(false); // Clean up cursor lock modification

        // Leave Steam lobby if we are in one
        if (ArkvainLobbyData.HasValidLobby())
        {
            Debug.Log("[EscapeMenu] Leaving Steam lobby...");
            ArkvainLobbyData.CurrentLobby.Leave();
            ArkvainLobbyData.Clear();
        }

        // Return to the main menu/lobby scene
        if (_lobbyScene != null)
        {
            if (LoadingManager.Instance != null)
            {
                LoadingManager.Instance.LoadScene(_lobbyScene.sceneName);
            }
            else
            {
                SceneManager.LoadScene(_lobbyScene.sceneName);
            }
        }
        else
        {
            Debug.LogError("[EscapeMenu] _lobbyScene is not assigned!");
        }
    }

    private void OnQuitToDesktop()
    {
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
