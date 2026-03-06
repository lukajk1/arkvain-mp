using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Heathen.SteamworksIntegration;
using Steamworks;
using API = Heathen.SteamworksIntegration.API;

public class LobbyCreator : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private TMP_InputField lobbyCodeInputField;
    [SerializeField] private TMP_InputField lobbyNameInputField;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text memberListText;
    [SerializeField] private Button backButton;

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
    }

    void Update()
    {
        // Update member list if in a lobby
        if (_currentLobby.IsValid)
        {
            UpdateMemberList();
        }
    }
    void OnEnable()
    {
        if (backButton != null) backButton.onClick.AddListener(OnBackButtonClicked);
    }

    void OnDisable()
    {
        if (backButton != null) backButton.onClick.RemoveListener(OnBackButtonClicked);

        if (lobbyNameInputField != null)
            lobbyNameInputField.onSubmit.RemoveListener(OnLobbyNameSubmitted);
    }

    private void OnBackButtonClicked()
    {
        SetState(false);
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
