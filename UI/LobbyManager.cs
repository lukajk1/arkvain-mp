using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Heathen.SteamworksIntegration;
using Steamworks;
using API = Heathen.SteamworksIntegration.API;

public class LobbyManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SceneNameHolder gameScene;
    
    [Header("UI References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private TMP_InputField lobbyCodeInputField;
    [SerializeField] private TMP_InputField lobbyNameInputField;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text memberListText;
    [SerializeField] private Button backButton;
    [SerializeField] private Button startButton;

    [Header("Lobby Settings")]
    [SerializeField] private int maxMembers = 4;
    [SerializeField] private ELobbyType lobbyType = ELobbyType.k_ELobbyTypePublic;

    private LobbyData _currentLobby;

    private void Awake()
    {
        SetState(false);
    }

    void Start()
    {
        UpdateStatusText("Ready to create lobby.");

        if (lobbyNameInputField != null)
            lobbyNameInputField.onSubmit.AddListener(OnLobbyNameSubmitted);

        // Subscribe to lobby chat updates
        SteamTools.Events.OnLobbyChatUpdate += OnLobbyChatUpdate;
    }

    void Update()
    {
        // Update member list if in a lobby
        if (_currentLobby.IsValid)
        {
            UpdateMemberList();
            UpdateLobbyUI();
        }
    }

    private void UpdateLobbyUI()
    {
        bool isHost = _currentLobby.IsOwner;

        // Only host can see/click start button
        if (startButton != null)
            startButton.gameObject.SetActive(isHost);
    }

    private void OnLobbyChatUpdate(LobbyData lobby, UserData user, EChatMemberStateChange state)
    {
        if (!_currentLobby.IsValid) return;
        if (lobby != _currentLobby) return;

        // Check if someone left or disconnected
        if (state == EChatMemberStateChange.k_EChatMemberStateChangeLeft ||
            state == EChatMemberStateChange.k_EChatMemberStateChangeDisconnected)
        {
            // If the person who left is the owner, lobby is closing
            if (user.id == _currentLobby.Owner.user.id)
            {
                Debug.Log("[LobbyManager] Host has closed the lobby!");
                UpdateStatusText("Host closed the lobby.");

                // Clear lobby and return to menu
                _currentLobby = default;
                SetState(false);
            }
        }
    }
    void OnEnable()
    {
        if (backButton != null) backButton.onClick.AddListener(OnBackButtonClicked);
        if (startButton != null) startButton.onClick.AddListener(OnStartButtonClicked);
    }

    void OnDisable()
    {
        if (backButton != null) backButton.onClick.RemoveListener(OnBackButtonClicked);
        if (startButton != null) startButton.onClick.RemoveListener(OnStartButtonClicked);

        if (lobbyNameInputField != null)
            lobbyNameInputField.onSubmit.RemoveListener(OnLobbyNameSubmitted);

        // Unsubscribe from lobby events
        SteamTools.Events.OnLobbyChatUpdate -= OnLobbyChatUpdate;
    }

    private void OnBackButtonClicked()
    {
        if (_currentLobby.IsValid)
        {
            if (_currentLobby.IsOwner)
            {
                // Host closes the lobby
                Debug.Log("[LobbyManager] Host closing lobby...");
                // Closing the lobby will kick all players
                _currentLobby.Leave();
            }
            else
            {
                // Client leaves the lobby
                Debug.Log("[LobbyManager] Leaving lobby...");
                _currentLobby.Leave();
            }

            _currentLobby = default;
        }

        SetState(false);
    }

    private void OnStartButtonClicked()
    {
        if (!_currentLobby.IsValid)
        {
            Debug.LogWarning("[LobbyManager] Cannot start - not in a valid lobby!");
            return;
        }

        if (!_currentLobby.IsOwner)
        {
            Debug.LogWarning("[LobbyManager] Only the host can start the game!");
            return;
        }

        Debug.Log("[LobbyManager] Starting game...");
        ArkvainLobbyData.SetLobby(_currentLobby);

        UnityEngine.SceneManagement.SceneManager.LoadScene(gameScene.sceneName);
    }

    private void OnLobbyNameSubmitted(string newName)
    {
        if (!_currentLobby.IsValid)
        {
            Debug.LogWarning("[LobbyCreator] Not in a lobby. Cannot change name.");
            return;
        }

        if (string.IsNullOrEmpty(newName))
        {
            Debug.LogWarning("[LobbyCreator] Lobby name cannot be empty.");
            return;
        }

        _currentLobby.Name = newName;
        Debug.Log($"[LobbyCreator] Lobby name changed to: {newName}");
    }

    public void SetState(bool state)
    {
        if (canvas != null)
        {
            canvas.gameObject.SetActive(state);
        }
    }

    public void CreateLobby()
    {
        UpdateStatusText("Creating lobby...");

        API.Matchmaking.Client.CreateLobby(lobbyType, SteamLobbyModeType.Session, maxMembers, HandleLobbyCreated);
    }

    private void HandleLobbyCreated(EResult result, LobbyData lobby, bool ioError)
    {
        if (ioError || result != EResult.k_EResultOK)
        {
            UpdateStatusText($"Failed to create lobby!\nResult: {result}\nIO Error: {ioError}");
            return;
        }

        _currentLobby = lobby;

        // Set lobby name from input field, or use owner's Steam name as default
        string lobbyName = $"{lobby.Owner.user.Name}'s Lobby";
        if (lobbyNameInputField != null && !string.IsNullOrEmpty(lobbyNameInputField.text))
        {
            lobbyName = lobbyNameInputField.text;
        }
        lobby.Name = lobbyName;

        // Populate the input field with the current lobby name
        if (lobbyNameInputField != null)
        {
            lobbyNameInputField.text = lobbyName;
        }

        // Set some test metadata
        lobby["game_mode"] = "Test Mode";
        lobby["map"] = "Test Map";

        // Display lobby code in the input field for easy copying
        string lobbyCode = lobby.HexId;
        if (lobbyCodeInputField != null)
        {
            lobbyCodeInputField.text = lobbyCode;
        }

        UpdateStatusText($"Lobby Created Successfully!\n" +
                         $"Lobby ID: {lobby.SteamId}\n" +
                         $"Lobby Code: {lobbyCode}\n" +
                         $"Type: {lobbyType}\n" +
                         $"Max Members: {maxMembers}\n" +
                         $"Owner: {lobby.Owner.user.Name}");

        Debug.Log($"[LobbyCreator] Created lobby with ID: {lobby.SteamId}, Code: {lobbyCode}");
    }

    private void UpdateStatusText(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log($"[LobbyCreator] {message}");
    }

    private void UpdateMemberList()
    {
        if (memberListText == null || !_currentLobby.IsValid)
            return;

        string memberList = $"Members ({_currentLobby.MemberCount}/{_currentLobby.MaxMembers}):\n";

        foreach (var member in _currentLobby.Members)
        {
            string memberName = member.user.Name;
            bool isOwner = member.user == _currentLobby.Owner.user;

            if (isOwner)
            {
                memberList += $"[HOST] {memberName}\n";
            }
            else
            {
                memberList += $"{memberName}\n";
            }
        }

        memberListText.text = memberList;
    }
}
