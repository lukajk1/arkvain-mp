using PurrNet;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ScoreboardManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private Transform rowContainer;
    [SerializeField] private GameObject playerRowPrefab;

    [Header("Update Settings")]
    [SerializeField] private float updateInterval = 0.5f;

    private List<ScoreboardRow> _activeRows = new List<ScoreboardRow>();
    private Dictionary<PlayerID, ScoreboardRow> _playerRowMap = new Dictionary<PlayerID, ScoreboardRow>();
    private bool _isVisible;
    private float _lastUpdateTime;

    private void Start()
    {
        // Subscribe to MatchSessionManager events
        if (MatchSessionManager.Instance != null)
        {
            MatchSessionManager.Instance.OnPlayerJoined += OnPlayerJoined;
            MatchSessionManager.Instance.OnPlayerLeft += OnPlayerLeft;
        }
        else
        {
            Debug.LogError("[ScoreboardManager] MatchSessionManager.Instance is null!");
        }

        // Start hidden
        if (canvas != null)
            canvas.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (MatchSessionManager.Instance != null)
        {
            MatchSessionManager.Instance.OnPlayerJoined -= OnPlayerJoined;
            MatchSessionManager.Instance.OnPlayerLeft -= OnPlayerLeft;
        }
    }

    private void Update()
    {
        // Toggle scoreboard with Tab
        if (PersistentClient.Instance != null && PersistentClient.Instance.inputManager != null)
        {
            if (PersistentClient.Instance.inputManager.UI.Tab.WasPressedThisFrame())
            {
                Show();
            }
            else if (PersistentClient.Instance.inputManager.UI.Tab.WasReleasedThisFrame())
            {
                Hide();
            }
        }

        // Update rows periodically while visible
        if (_isVisible && Time.time - _lastUpdateTime >= updateInterval)
        {
            UpdateRows();
            _lastUpdateTime = Time.time;
        }
    }

    private void Show()
    {
        if (canvas != null)
        {
            canvas.gameObject.SetActive(true);
            _isVisible = true;

            // Rebuild rows with fresh data
            RebuildRows();
        }
    }

    private void Hide()
    {
        if (canvas != null)
        {
            canvas.gameObject.SetActive(false);
            _isVisible = false;
        }
    }

    private void RebuildRows()
    {
        // Clear existing rows
        ClearRows();

        if (MatchSessionManager.Instance == null)
        {
            Debug.LogWarning("[ScoreboardManager] MatchSessionManager.Instance is null!");
            return;
        }

        // Get leaderboard sorted by score
        List<PlayerMatchData> players = MatchSessionManager.Instance.GetLeaderboard();

        // Create a row for each player
        foreach (var playerData in players)
        {
            CreateRowForPlayer(playerData);
        }
    }

    private void CreateRowForPlayer(PlayerMatchData playerData)
    {
        if (playerRowPrefab == null || rowContainer == null)
        {
            Debug.LogError("[ScoreboardManager] playerRowPrefab or rowContainer is null!");
            return;
        }

        // Instantiate row
        GameObject rowObj = Instantiate(playerRowPrefab, rowContainer);
        ScoreboardRow row = rowObj.GetComponent<ScoreboardRow>();

        if (row != null)
        {
            row.UpdateData(playerData);
            _activeRows.Add(row);
            _playerRowMap[playerData.PlayerId] = row;
        }
        else
        {
            Debug.LogError("[ScoreboardManager] playerRowPrefab is missing ScoreboardRow component!");
        }
    }

    private void UpdateRows()
    {
        if (MatchSessionManager.Instance == null)
            return;

        // Update each existing row with fresh data
        foreach (var row in _activeRows)
        {
            if (row != null && row.PlayerData != null)
            {
                PlayerMatchData freshData = MatchSessionManager.Instance.GetPlayerData(row.PlayerData.PlayerId);
                if (freshData != null)
                {
                    row.UpdateData(freshData);
                }
            }
        }
    }

    private void ClearRows()
    {
        // Destroy all existing rows
        foreach (var row in _activeRows)
        {
            if (row != null)
                Destroy(row.gameObject);
        }

        _activeRows.Clear();
        _playerRowMap.Clear();
    }

    private void OnPlayerJoined(PlayerID playerId)
    {
        // If scoreboard is visible, add a row for the new player
        if (_isVisible && MatchSessionManager.Instance != null)
        {
            PlayerMatchData playerData = MatchSessionManager.Instance.GetPlayerData(playerId);
            if (playerData != null && !_playerRowMap.ContainsKey(playerId))
            {
                CreateRowForPlayer(playerData);
            }
        }
    }

    private void OnPlayerLeft(PlayerID playerId)
    {
        // Remove the player's row
        if (_playerRowMap.TryGetValue(playerId, out ScoreboardRow row))
        {
            _activeRows.Remove(row);
            _playerRowMap.Remove(playerId);

            if (row != null)
                Destroy(row.gameObject);
        }
    }
}
