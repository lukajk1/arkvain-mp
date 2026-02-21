using PurrNet;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Server-authoritative 1v1 game mode manager.
/// Tracks kills/deaths and checks win conditions.
/// Uses standard PurrNet (non-predicted) networking for reliable, synchronized game stats.
/// </summary>
public class GameManager1v1 : NetworkBehaviour
{
    [Header("1v1 Mode Settings")]
    [SerializeField] private int _scoreLimit = 10;

    // Server-authoritative score dictionary
    // Automatically syncs to all clients when changed on server
    public SyncVar<Dictionary<PlayerID, int>> kills = new SyncVar<Dictionary<PlayerID, int>>();
    public SyncVar<Dictionary<PlayerID, int>> deaths = new SyncVar<Dictionary<PlayerID, int>>();

    // Event fired when a player wins (includes winner's PlayerInfo)
    public static event System.Action<PlayerInfo> OnPlayerVictory;

    private bool _matchEnded = false;

    protected override void OnSpawned(bool isRetroactive)
    {
        base.OnSpawned(isRetroactive);

        // Initialize dictionaries on server
        if (isServer)
        {
            kills.value = new Dictionary<PlayerID, int>();
            deaths.value = new Dictionary<PlayerID, int>();
            _matchEnded = false;
        }
    }

    /// <summary>
    /// Called by server to record a kill.
    /// Checks win condition after updating scores.
    /// </summary>
    [ServerRpc]
    public void RecordKill(PlayerInfo killer, PlayerInfo victim)
    {
        if (_matchEnded) return; // Don't record kills after match ends

        // Initialize if needed
        if (!kills.value.ContainsKey(killer.playerID))
            kills.value[killer.playerID] = 0;
        if (!deaths.value.ContainsKey(victim.playerID))
            deaths.value[victim.playerID] = 0;

        // Update scores
        kills.value[killer.playerID]++;
        deaths.value[victim.playerID]++;

        // Force sync to clients (required when modifying dictionary contents)
        kills.SetDirty();
        deaths.SetDirty();

        // Check win condition
        CheckWinCondition();
    }

    /// <summary>
    /// Checks if any player has reached the score limit.
    /// Server-only.
    /// </summary>
    private void CheckWinCondition()
    {
        if (!isServer || _matchEnded) return;

        foreach (var kvp in kills.value)
        {
            if (kvp.Value >= _scoreLimit)
            {
                // Winner found!
                _matchEnded = true;
                TriggerVictory(kvp.Key);
                break;
            }
        }
    }

    /// <summary>
    /// Triggers victory event on all clients.
    /// Server-only.
    /// </summary>
    [ObserversRpc]
    private void TriggerVictory(PlayerID winner)
    {
        PlayerInfo winnerInfo = new PlayerInfo(winner);
        Debug.Log($"[ScoreManager] Player {winnerInfo} wins with {GetKills(winner)} kills!");
        OnPlayerVictory?.Invoke(winnerInfo);
    }

    /// <summary>
    /// Get kills for a specific player.
    /// </summary>
    public int GetKills(PlayerID player)
    {
        return kills.value.TryGetValue(player, out int count) ? count : 0;
    }

    public int GetKills(PlayerInfo player)
    {
        return GetKills(player.playerID);
    }

    /// <summary>
    /// Get deaths for a specific player.
    /// </summary>
    public int GetDeaths(PlayerID player)
    {
        return deaths.value.TryGetValue(player, out int count) ? count : 0;
    }

    public int GetDeaths(PlayerInfo player)
    {
        return GetDeaths(player.playerID);
    }

    /// <summary>
    /// Get K/D ratio for a specific player.
    /// </summary>
    public float GetKDRatio(PlayerID player)
    {
        int killCount = GetKills(player);
        int deathCount = GetDeaths(player);

        if (deathCount == 0)
            return killCount;

        return (float)killCount / deathCount;
    }

    public float GetKDRatio(PlayerInfo player)
    {
        return GetKDRatio(player.playerID);
    }

    /// <summary>
    /// Reset all scores (server only).
    /// </summary>
    [ServerRpc]
    public void ResetScores()
    {
        kills.value.Clear();
        deaths.value.Clear();
        kills.SetDirty();
        deaths.SetDirty();
        _matchEnded = false;
    }

    /// <summary>
    /// Get the current score limit for this 1v1 match.
    /// </summary>
    public int GetScoreLimit()
    {
        return _scoreLimit;
    }

    /// <summary>
    /// Check if the match has ended.
    /// </summary>
    public bool IsMatchEnded()
    {
        return _matchEnded;
    }
}
