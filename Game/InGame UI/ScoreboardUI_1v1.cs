using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PurrNet;
using System.Linq;

/// <summary>
/// Displays scoreboard for 1v1 matches.
/// Shows kills/deaths for both players in a single text field.
/// </summary>
public class ScoreboardUI_1v1 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager1v1 _scoreManager;
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private GameObject _matchResultObject;
    [SerializeField] private TextMeshProUGUI _matchResultText;
    [SerializeField] private Image _glowBehindText;

    [Header("Format")]
    private string _scoreFormat = "{0}: {1} / {2}: {3}";
    private string _victoryMessage = "VICTORY";
    private string _defeatMessage = "DEFEAT";

    [Header("Settings")]
    [SerializeField] private float _updateInterval = 0.5f; // Update UI every 0.5 seconds
    [SerializeField] private Color _defeatColor;

    private float _updateTimer;
    private PlayerInfo? _player1Info;
    private PlayerInfo? _player2Info;

    private void Start()
    {
        if (_scoreManager == null)
        {
            _scoreManager = FindFirstObjectByType<GameManager1v1>();
            if (_scoreManager == null)
            {
                Debug.LogError("[ScoreboardUI] ScoreManager not found in scene!");
                enabled = false;
                return;
            }
        }

        // Hide match result object initially
        if (_matchResultObject != null)
        {
            _matchResultObject.SetActive(false);
        }

        // Subscribe to score changes for immediate updates
        _scoreManager.kills.onChanged += OnScoresChanged;
        _scoreManager.deaths.onChanged += OnScoresChanged;

        // Subscribe to victory event
        GameManager1v1.OnPlayerVictory += OnPlayerVictory;

        UpdateUI();
    }

    private void OnDestroy()
    {
        if (_scoreManager != null)
        {
            _scoreManager.kills.onChanged -= OnScoresChanged;
            _scoreManager.deaths.onChanged -= OnScoresChanged;
        }

        // Unsubscribe from static event
        GameManager1v1.OnPlayerVictory -= OnPlayerVictory;
    }

    private void Update()
    {
        // Periodic update as fallback
        _updateTimer += Time.deltaTime;
        if (_updateTimer >= _updateInterval)
        {
            _updateTimer = 0f;
            UpdateUI();
        }
    }

    private void OnScoresChanged(System.Collections.Generic.Dictionary<PlayerID, int> scores)
    {
        UpdateUI();
    }

    /// <summary>
    /// Force an immediate UI update. Useful to call after players spawn.
    /// </summary>
    public void RefreshScoreboard()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_scoreManager == null || _scoreText == null) return;

        // Check if dictionaries are initialized (they might be null before server spawns the GameManager)
        if (_scoreManager.kills.value == null || _scoreManager.deaths.value == null)
        {
            _scoreText.text = "Waiting for match to start...";
            return;
        }

        // Get all players - first try from scores, then from PlayerInfoManager
        var players = _scoreManager.kills.value.Keys
            .Union(_scoreManager.deaths.value.Keys)
            .Distinct()
            .ToList();

        // If no players have scores yet, get all registered players from PlayerInfoManager
        if (players.Count == 0)
        {
            players = new System.Collections.Generic.List<PlayerID>(PlayerInfoManager.GetAll().Keys);
        }

        // Assign players to slots (first two players found)
        if (players.Count > 0 && !_player1Info.HasValue)
        {
            if (PlayerInfoManager.TryGet(players[0], out var info))
                _player1Info = info;
        }

        if (players.Count > 1 && !_player2Info.HasValue)
        {
            if (PlayerInfoManager.TryGet(players[1], out var info))
                _player2Info = info;
        }

        // Get scores using player names from PlayerInfoManager
        string player1Name = _player1Info.HasValue ? _player1Info.Value.name : "---";
        int player1Score = _player1Info.HasValue ? _scoreManager.GetKills(_player1Info.Value) : 0;

        string player2Name = _player2Info.HasValue ? _player2Info.Value.name : "---";
        int player2Score = _player2Info.HasValue ? _scoreManager.GetKills(_player2Info.Value) : 0;

        // Update single text field
        _scoreText.text = string.Format(_scoreFormat, player1Name, player1Score, player2Name, player2Score);
    }

    /// <summary>
    /// Called when a player wins the match.
    /// Shows victory or defeat message based on whether the local player won.
    /// </summary>
    private void OnPlayerVictory(PlayerInfo winner)
    {
        if (_matchResultObject == null || _matchResultText == null) return;

        // Check if the local player is the winner
        PlayerID localPlayerID = NetworkManager.main.localPlayer;

        Debug.Log($"[ScoreboardUI_1v1] OnPlayerVictory called. Winner ID: {winner.playerID.id}, Local PlayerID: {localPlayerID.id}");

        bool isLocalPlayerWinner = localPlayerID.id == winner.playerID.id;

        // Set appropriate message
        _matchResultText.text = isLocalPlayerWinner ? _victoryMessage : _defeatMessage;
        if (!isLocalPlayerWinner)
        {
            _matchResultText.color = _defeatColor;
            _glowBehindText.color = _defeatColor;
        }

        // Show the match result object
        _matchResultObject.SetActive(true);

        Debug.Log($"[ScoreboardUI_1v1] Match ended. Winner: {winner.playerID.id}, Local player: {localPlayerID.id}, Victory: {isLocalPlayerWinner}");
    }
}
