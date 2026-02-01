using UnityEngine;
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

    [Header("Format")]
    [SerializeField] private string _scoreFormat = "Player {0}: {1} / Player {2}: {3}";
    [SerializeField] private string _victoryMessage = "VICTORY!";
    [SerializeField] private string _defeatMessage = "DEFEAT";

    [Header("Settings")]
    [SerializeField] private float _updateInterval = 0.5f; // Update UI every 0.5 seconds

    private float _updateTimer;
    private PlayerID? _player1ID;
    private PlayerID? _player2ID;

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

    private void UpdateUI()
    {
        if (_scoreManager == null || _scoreText == null) return;

        // Get all players with scores
        var players = _scoreManager.kills.value.Keys
            .Union(_scoreManager.deaths.value.Keys)
            .Distinct()
            .ToList();

        // Assign players to slots (first two players found)
        if (players.Count > 0 && !_player1ID.HasValue)
            _player1ID = players[0];

        if (players.Count > 1 && !_player2ID.HasValue)
            _player2ID = players[1];

        // Get scores
        string player1IDStr = _player1ID.HasValue ? _player1ID.Value.ToString() : "---";
        int player1Score = _player1ID.HasValue ? _scoreManager.GetKills(_player1ID.Value) : 0;

        string player2IDStr = _player2ID.HasValue ? _player2ID.Value.ToString() : "---";
        int player2Score = _player2ID.HasValue ? _scoreManager.GetKills(_player2ID.Value) : 0;

        // Update single text field
        _scoreText.text = string.Format(_scoreFormat, player1IDStr, player1Score, player2IDStr, player2Score);
    }

    /// <summary>
    /// Called when a player wins the match.
    /// Shows victory or defeat message based on whether the local player won.
    /// </summary>
    private void OnPlayerVictory(PlayerID winner)
    {
        if (_matchResultObject == null || _matchResultText == null) return;

        // Check if the local player is the winner
        PlayerID localPlayerID = NetworkManager.main.localPlayer;

        Debug.Log($"[ScoreboardUI_1v1] OnPlayerVictory called. Winner ID: {winner.id}, Local PlayerID: {localPlayerID.id}");

        bool isLocalPlayerWinner = localPlayerID.id == winner.id;

        // Set appropriate message
        _matchResultText.text = isLocalPlayerWinner ? _victoryMessage : _defeatMessage;

        // Show the match result object
        _matchResultObject.SetActive(true);

        Debug.Log($"[ScoreboardUI_1v1] Match ended. Winner: {winner.id}, Local player: {localPlayerID.id}, Victory: {isLocalPlayerWinner}");
    }
}
