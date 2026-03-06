using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Heathen.SteamworksIntegration;
using Steamworks;
using API = Heathen.SteamworksIntegration.API;

public class LobbyCreator : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private TMP_InputField lobbyCodeInputField;
    [SerializeField] private TMP_Text statusText;

    [Header("Lobby Settings")]
    [SerializeField] private int maxMembers = 4;
    [SerializeField] private ELobbyType lobbyType = ELobbyType.k_ELobbyTypePublic;

    private LobbyData _currentLobby;

    void Start()
    {
        if (createLobbyButton != null)
            createLobbyButton.onClick.AddListener(OnCreateLobbyClicked);

        UpdateStatusText("Ready to create lobby.");
    }

    private void OnCreateLobbyClicked()
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

    void OnDestroy()
    {
        if (createLobbyButton != null)
            createLobbyButton.onClick.RemoveListener(OnCreateLobbyClicked);
    }
}
