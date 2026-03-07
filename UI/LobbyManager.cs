using Heathen.SteamworksIntegration;
using PurrLobby;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using API = Heathen.SteamworksIntegration.API;

[System.Serializable]
public class GameModeMapSettings
{
    public string gameModeName;
    public List<string> allowedMaps = new List<string>();
}

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

    [Header("Host Controls")]
    [SerializeField] private GameObject hostControlsContainer;
    [SerializeField] private TMP_Dropdown gameModeDropdown;
    [SerializeField] private TMP_Dropdown mapDropdown;

    [Header("Lobby Settings")]
    [SerializeField] private int maxMembers = 4;
    [SerializeField] private ELobbyType lobbyType = ELobbyType.k_ELobbyTypePublic;

    [Header("Game Mode/Map Configuration")]
    [SerializeField] private List<GameModeMapSettings> gameModeMapConfigs = new List<GameModeMapSettings>();

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

        if (gameModeDropdown != null)
            gameModeDropdown.onValueChanged.AddListener(OnGameModeChanged);

        if (mapDropdown != null)
            mapDropdown.onValueChanged.AddListener(OnMapChanged);

        // Subscribe to lobby events
        SteamTools.Events.OnLobbyChatUpdate += OnLobbyChatUpdate;
        SteamTools.Events.OnLobbyDataUpdate += OnLobbyDataUpdate;
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

        // Only host can see/modify game settings
        if (hostControlsContainer != null)
            hostControlsContainer.SetActive(isHost);

        // Disable dropdowns for non-hosts (defensive even though container is hidden)
        if (gameModeDropdown != null)
            gameModeDropdown.interactable = isHost;

        if (mapDropdown != null)
            mapDropdown.interactable = isHost;
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

                System.Action onConfirmDelegate = () => SetState(false);
                PersistentClient.Instance.CreateConfirmationDialog(
                    onConfirm: onConfirmDelegate,
                    message: "Host has closed the lobby.",
                    confirmText: "Main Menu");

                // Clear lobby and return to menu
                ArkvainLobbyData.Clear();
                _currentLobby = default;
            }
        }
    }

    private void OnLobbyDataUpdate(LobbyData lobby, LobbyMemberData? member)
    {
        if (!_currentLobby.IsValid) return;
        if (lobby != _currentLobby) return;

        // Only process lobby metadata updates (not member metadata)
        if (member == null)
        {
            // Sync dropdowns with updated lobby metadata
            SyncDropdownsFromLobby();

            // Check if host has started the game
            string gameStarted = lobby["game_started"];
            if (gameStarted == "true" && !_currentLobby.IsOwner)
            {
                Debug.Log("[LobbyManager] Host started the game - loading scene...");
                UpdateStatusText("Host started the game!");

                // Ensure ArkvainLobbyData is set
                ArkvainLobbyData.SetLobby(_currentLobby);

                // Load game scene
                UnityEngine.SceneManagement.SceneManager.LoadScene(gameScene.sceneName);
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

        if (gameModeDropdown != null)
            gameModeDropdown.onValueChanged.RemoveListener(OnGameModeChanged);

        if (mapDropdown != null)
            mapDropdown.onValueChanged.RemoveListener(OnMapChanged);

        // Unsubscribe from lobby events
        SteamTools.Events.OnLobbyChatUpdate -= OnLobbyChatUpdate;
        SteamTools.Events.OnLobbyDataUpdate -= OnLobbyDataUpdate;
    }

    private void OnBackButtonClicked()
    {
        if (_currentLobby.IsValid)
        {
            if (_currentLobby.IsOwner)
            {
                PersistentClient.Instance.CreateConfirmationDialog(
                    onConfirm: ConfirmExitLobby,
                    message: "Exit to Main Menu? This will close the Lobby.");
            }
            else
            {
                // Client leaves the lobby
                Debug.Log("[LobbyManager] Leaving lobby...");
                _currentLobby.Leave();
                ArkvainLobbyData.Clear();
                SetState(false);
                _currentLobby = default;
            }
        }

    }
    private void ConfirmExitLobby()
    {
        // Host closes the lobby
        Debug.Log("[LobbyManager] Host closing lobby...");
        // Closing the lobby will kick all players
        _currentLobby.Leave();
        ArkvainLobbyData.Clear();
        SetState(false);
        _currentLobby = default;
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

        // Set lobby metadata to signal clients to load scene
        _currentLobby["game_started"] = "true";

        // Store lobby data for persistence
        ArkvainLobbyData.SetLobby(_currentLobby);

        // Load game scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameScene.sceneName);
    }

    private void OnLobbyNameSubmitted(string newName)
    {
        if (!_currentLobby.IsValid)
        {
            Debug.LogWarning("[LobbyManager] Not in a lobby. Cannot change name.");
            return;
        }

        if (string.IsNullOrEmpty(newName))
        {
            Debug.LogWarning("[LobbyManager] Lobby name cannot be empty.");
            return;
        }

        _currentLobby.Name = newName;
        Debug.Log($"[LobbyManager] Lobby name changed to: {newName}");
    }

    private void OnGameModeChanged(int index)
    {
        if (!_currentLobby.IsValid)
        {
            Debug.LogWarning("[LobbyManager] Not in a lobby. Cannot change game mode.");
            return;
        }

        if (!_currentLobby.IsOwner)
        {
            Debug.LogWarning("[LobbyManager] Only the host can change game mode.");
            return;
        }

        if (gameModeDropdown != null)
        {
            string selectedMode = gameModeDropdown.options[index].text;
            _currentLobby["game_mode"] = selectedMode;
            Debug.Log($"[LobbyManager] Game mode changed to: {selectedMode}");

            // Repopulate map dropdown based on selected game mode
            RepopulateMapDropdown(selectedMode);
        }
    }

    private void OnMapChanged(int index)
    {
        if (!_currentLobby.IsValid)
        {
            Debug.LogWarning("[LobbyManager] Not in a lobby. Cannot change map.");
            return;
        }

        if (!_currentLobby.IsOwner)
        {
            Debug.LogWarning("[LobbyManager] Only the host can change map.");
            return;
        }

        if (mapDropdown != null)
        {
            string selectedMap = mapDropdown.options[index].text;
            _currentLobby["map"] = selectedMap;
            Debug.Log($"[LobbyManager] Map changed to: {selectedMap}");
        }
    }

    private void SetState(bool state)
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

        SetState(true);
    }

    public void JoinLobby(LobbyData lobby)
    {
        Debug.Log($"[LobbyManager] Attempting to join lobby: {lobby.Name}");

        lobby.Join((enterData, ioError) =>
        {
            if (ioError)
            {
                Debug.LogError($"[LobbyManager] Failed to join lobby: IO Error");
                UpdateStatusText("Failed to join lobby: IO Error");
                return;
            }

            if (enterData.Response == Steamworks.EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
                Debug.Log($"[LobbyManager] Successfully joined lobby!");

                // Store the lobby reference
                _currentLobby = lobby;

                // Set ArkvainLobbyData for scene persistence
                ArkvainLobbyData.SetLobby(lobby);

                // Update UI with lobby info
                UpdateStatusText($"Joined lobby: {lobby.Name}");

                // Populate lobby code field
                if (lobbyCodeInputField != null)
                {
                    lobbyCodeInputField.text = lobby.HexId;
                }

                // Populate lobby name field
                if (lobbyNameInputField != null)
                {
                    lobbyNameInputField.text = lobby.Name;
                }

                // Sync dropdown values from lobby metadata
                SyncDropdownsFromLobby();
            }
            else
            {
                Debug.LogWarning($"[LobbyManager] Failed to join lobby: {enterData.Response}");
                UpdateStatusText($"Failed to join lobby: {enterData.Response}");
            }
        });

        SetState(true);
    }

    private void HandleLobbyCreated(EResult result, LobbyData lobby, bool ioError)
    {
        if (ioError || result != EResult.k_EResultOK)
        {
            UpdateStatusText($"Failed to create lobby!\nResult: {result}\nIO Error: {ioError}");
            return;
        }

        _currentLobby = lobby;

        // Set ArkvainLobbyData for scene persistence
        ArkvainLobbyData.SetLobby(lobby);

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

        // Set initial game settings from dropdowns
        if (gameModeDropdown != null && gameModeDropdown.options.Count > 0)
        {
            string initialMode = gameModeDropdown.options[gameModeDropdown.value].text;
            lobby["game_mode"] = initialMode;

            // Repopulate map dropdown for the initial mode
            RepopulateMapDropdown(initialMode);
        }
        else
        {
            lobby["game_mode"] = "Test Mode";
        }

        // Map is set by RepopulateMapDropdown, but fall back if no config
        if (string.IsNullOrEmpty(lobby["map"]))
        {
            if (mapDropdown != null && mapDropdown.options.Count > 0)
            {
                string initialMap = mapDropdown.options[mapDropdown.value].text;
                lobby["map"] = initialMap;
            }
            else
            {
                lobby["map"] = "Test Map";
            }
        }

        // Display lobby code in the input field for easy copying
        string lobbyCode = lobby.HexId;
        if (lobbyCodeInputField != null)
        {
            lobbyCodeInputField.text = lobbyCode;
        }

        // Sync dropdowns to reflect current lobby settings
        SyncDropdownsFromLobby();

        UpdateStatusText($"Lobby Created Successfully!\n" +
                         $"Lobby ID: {lobby.SteamId}\n" +
                         $"Lobby Code: {lobbyCode}\n" +
                         $"Type: {lobbyType}\n" +
                         $"Max Members: {maxMembers}\n" +
                         $"Owner: {lobby.Owner.user.Name}");

        Debug.Log($"[LobbyManager] Created lobby with ID: {lobby.SteamId}, Code: {lobbyCode}");
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

    private void SyncDropdownsFromLobby()
    {
        if (!_currentLobby.IsValid)
            return;

        string gameMode = _currentLobby["game_mode"];
        string map = _currentLobby["map"];

        // Sync game mode dropdown
        if (gameModeDropdown != null && !string.IsNullOrEmpty(gameMode))
        {
            for (int i = 0; i < gameModeDropdown.options.Count; i++)
            {
                if (gameModeDropdown.options[i].text == gameMode)
                {
                    gameModeDropdown.SetValueWithoutNotify(i);
                    break;
                }
            }

            // Repopulate map dropdown for the current game mode (without triggering events)
            RepopulateMapDropdownSilent(gameMode);
        }

        // Sync map dropdown
        if (mapDropdown != null && !string.IsNullOrEmpty(map))
        {
            for (int i = 0; i < mapDropdown.options.Count; i++)
            {
                if (mapDropdown.options[i].text == map)
                {
                    mapDropdown.SetValueWithoutNotify(i);
                    break;
                }
            }
        }
    }

    private void RepopulateMapDropdown(string gameMode)
    {
        if (mapDropdown == null)
            return;

        // Find the configuration for this game mode
        GameModeMapSettings config = gameModeMapConfigs.FirstOrDefault(c => c.gameModeName == gameMode);

        if (config == null || config.allowedMaps.Count == 0)
        {
            Debug.LogWarning($"[LobbyManager] No map configuration found for game mode: {gameMode}");
            return;
        }

        // Clear existing options
        mapDropdown.ClearOptions();

        // Add new options from configuration
        List<TMP_Dropdown.OptionData> newOptions = config.allowedMaps
            .Select(mapName => new TMP_Dropdown.OptionData(mapName))
            .ToList();

        mapDropdown.AddOptions(newOptions);

        // Set first map as default and update lobby metadata
        if (newOptions.Count > 0)
        {
            mapDropdown.SetValueWithoutNotify(0);

            if (_currentLobby.IsValid && _currentLobby.IsOwner)
            {
                _currentLobby["map"] = config.allowedMaps[0];
                Debug.Log($"[LobbyManager] Map automatically set to: {config.allowedMaps[0]}");
            }
        }
    }

    private void RepopulateMapDropdownSilent(string gameMode)
    {
        if (mapDropdown == null)
            return;

        // Find the configuration for this game mode
        GameModeMapSettings config = gameModeMapConfigs.FirstOrDefault(c => c.gameModeName == gameMode);

        if (config == null || config.allowedMaps.Count == 0)
        {
            return;
        }

        // Clear existing options
        mapDropdown.ClearOptions();

        // Add new options from configuration
        List<TMP_Dropdown.OptionData> newOptions = config.allowedMaps
            .Select(mapName => new TMP_Dropdown.OptionData(mapName))
            .ToList();

        mapDropdown.AddOptions(newOptions);

        // Don't set a value or update metadata - let SyncDropdownsFromLobby handle that
    }
}
