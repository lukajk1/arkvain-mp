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
    [SerializeField] private Button joinButton;

    private LobbyData lobbyData;

    void Awake()
    {
        if (joinButton != null)
            joinButton.onClick.AddListener(OnJoinClicked);
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

        // Disable join button if full
        if (joinButton != null)
            joinButton.interactable = !lobby.Full;
    }

    private void OnJoinClicked()
    {
        if (!lobbyData.IsValid)
        {
            Debug.LogWarning("[ServerRow] Invalid lobby data!");
            return;
        }

        Debug.Log($"[ServerRow] Attempting to join lobby: {lobbyData.Name}");

        lobbyData.Join((enterData, ioError) =>
        {
            if (ioError)
            {
                Debug.LogError($"[ServerRow] Failed to join lobby: IO Error");
                return;
            }

            if (enterData.Response == Steamworks.EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
                Debug.Log($"[ServerRow] Successfully joined lobby!");
            }
            else
            {
                Debug.LogWarning($"[ServerRow] Failed to join lobby: {enterData.Response}");
            }
        });
    }

    void OnDestroy()
    {
        if (joinButton != null)
            joinButton.onClick.RemoveListener(OnJoinClicked);
    }
}
