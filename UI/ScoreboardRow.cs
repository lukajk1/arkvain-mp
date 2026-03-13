using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Heathen.SteamworksIntegration;
using PurrNet;

public class ScoreboardRow : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RawImage avatar;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text killsText;
    [SerializeField] private TMP_Text deathsText;
    [SerializeField] private TMP_Text assistsText;
    [SerializeField] private TMP_Text kdaText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text pingText;
    [SerializeField] private Image backgroundImage;

    [Header("Display Settings")]
    [SerializeField] private Color localPlayerColor = new Color(0.2f, 0.5f, 0.8f, 0.3f);
    [SerializeField] private Color normalPlayerColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
    [SerializeField] private Color disconnectedColor = new Color(0.3f, 0.1f, 0.1f, 0.5f);

    public MatchSessionManager.PlayerMatchState PlayerData { get; private set; }
    private ulong _loadedSteamId;

    public void UpdateData(MatchSessionManager.PlayerMatchState playerData)
    {
        PlayerData = playerData;

        // Update text fields
        if (playerNameText != null)
            playerNameText.text = MatchSessionManager.Instance.GetPlayerName(playerData.playerId);

        if (killsText != null)
            killsText.text = playerData.kills.ToString();

        if (deathsText != null)
            deathsText.text = playerData.deaths.ToString();

        if (assistsText != null)
            assistsText.text = playerData.assists.ToString();

        if (kdaText != null)
            kdaText.text = MatchSessionManager.GetKDA(playerData).ToString("F2");

        if (scoreText != null)
            scoreText.text = MatchSessionManager.CalculateScore(playerData).ToString("F0");

        // Update background color
        UpdateBackgroundColor(playerData);
    }

    private void UpdateBackgroundColor(MatchSessionManager.PlayerMatchState playerData)
    {
        if (backgroundImage == null)
            return;

        // Check if local player
        if (NetworkManager.main != null && playerData.playerId == NetworkManager.main.localPlayer)
        {
            backgroundImage.color = localPlayerColor;
        }
        else
        {
            backgroundImage.color = normalPlayerColor;
        }
    }
}
