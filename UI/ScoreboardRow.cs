using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Heathen.SteamworksIntegration;

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

    public PlayerMatchData PlayerData { get; private set; }
    private ulong _loadedSteamId;

    public void UpdateData(PlayerMatchData playerData)
    {
        if (playerData == null)
            return;

        PlayerData = playerData;

        // Update text fields
        if (playerNameText != null)
            playerNameText.text = playerData.PlayerName;

        if (killsText != null)
            killsText.text = playerData.Kills.ToString();

        if (deathsText != null)
            deathsText.text = playerData.Deaths.ToString();

        if (assistsText != null)
            assistsText.text = playerData.Assists.ToString();

        if (kdaText != null)
            kdaText.text = playerData.GetKDA().ToString("F2");

        if (scoreText != null)
            scoreText.text = playerData.CalculateScore().ToString("F0");

        if (pingText != null)
            pingText.text = $"{playerData.AveragePing:F0}ms";

        // Load avatar if Steam ID changed and is valid
        if (avatar != null && playerData.SteamId != 0 && playerData.SteamId != _loadedSteamId)
        {
            _loadedSteamId = playerData.SteamId;
            UserData.Get(playerData.SteamId).LoadAvatar(OnAvatarLoaded);
        }

        // Update background color
        UpdateBackgroundColor(playerData);
    }

    private void OnAvatarLoaded(Texture2D texture)
    {
        if (avatar != null && texture != null)
        {
            avatar.texture = texture;
        }
    }

    private void UpdateBackgroundColor(PlayerMatchData playerData)
    {
        if (backgroundImage == null)
            return;

        // Check if disconnected
        if (!playerData.IsConnected)
        {
            backgroundImage.color = disconnectedColor;
            return;
        }

        // Check if local player
        // TODO: Compare with local player's PlayerID when available
        // For now, use normal color
        backgroundImage.color = normalPlayerColor;
    }
}
