using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Heathen.SteamworksIntegration;

public class ServerRow : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text serverNameText;
    [SerializeField] private TMP_Text gameModeText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text mapText;
    [SerializeField] private TMP_Text pingText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button joinButton;

    [Header("Settings")]
    [SerializeField] private float refreshInterval = 5f;

    private LobbyData lobbyData;
    private float _nextRefreshTime;

    void Awake()
    {
        if (joinButton != null)
            joinButton.onClick.AddListener(OnJoinClicked);

        // Configure text overflow for text fields
        ConfigureTextOverflow();
    }

    private void ConfigureTextOverflow()
    {
        TextUtilities.ConfigureEllipsisOverflow(serverNameText, gameModeText, mapText);
    }

    public void Setup(LobbyData lobby)
    {
        lobbyData = lobby;

        // Set server name
        serverNameText.text = string.IsNullOrEmpty(lobby.Name) ? "Unnamed Server" : lobby.Name;

        // Set game mode from metadata
        string gameMode = lobby["game_mode"];
        gameModeText.text = string.IsNullOrEmpty(gameMode) ? "Unknown" : gameMode;

        // Set player count
        playerCountText.text = $"{lobby.MemberCount}/{lobby.MaxMembers}";

        // Set map from metadata
        string map = lobby["map"];
        mapText.text = string.IsNullOrEmpty(map) ? "Unknown" : map;

        // Ping (you'd need to implement actual ping logic)
        pingText.text = "? ms";

        // Initial status update
        RefreshStatusUI();

        // Disable join button if full
        if (joinButton != null)
            joinButton.interactable = !lobby.Full;

        _nextRefreshTime = Time.time + refreshInterval;
    }

    void Update()
    {
        if (!lobbyData.IsValid) return;

        if (Time.time >= _nextRefreshTime)
        {
            _nextRefreshTime = Time.time + refreshInterval;
            // Request latest data from Steam
            lobbyData.RequestData();
            RefreshStatusUI();
        }
    }

    private void RefreshStatusUI()
    {
        if (statusText == null) return;

        string gameStarted = lobbyData["game_started"];
        if (gameStarted == "true")
        {
            statusText.text = "In Game";
            statusText.color = Color.green;
        }
        else
        {
            statusText.text = "In Lobby";
            statusText.color = Color.white;
        }

        // Update member count while we're at it
        playerCountText.text = $"{lobbyData.MemberCount}/{lobbyData.MaxMembers}";
    }

    private void OnJoinClicked()
    {
        if (!lobbyData.IsValid)
        {
            Debug.LogWarning("[ServerRow] Invalid lobby data!");
            return;
        }

        MainMenu.Instance.JoinLobby(lobbyData);
    }

    void OnDestroy()
    {
        if (joinButton != null)
            joinButton.onClick.RemoveListener(OnJoinClicked);
    }
}
