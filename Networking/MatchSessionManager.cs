using PurrNet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MatchSessionManager : NetworkBehaviour
{
    public static MatchSessionManager Instance { get; private set; }

    private Dictionary<PlayerID, PlayerMatchData> _playerStats = new Dictionary<PlayerID, PlayerMatchData>();

    [Header("Ping Settings")]
    [SerializeField] private float pingUpdateInterval = 2f;
    private float _lastPingUpdate;

    // Events for UI updates
    public event Action<PlayerMatchData> OnPlayerStatsChanged;
    public event Action<PlayerID> OnPlayerJoined;
    public event Action<PlayerID> OnPlayerLeft;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        var networkManager = NetworkManager.main;
        if (networkManager != null && networkManager.playerModule != null)
        {
            networkManager.playerModule.onPlayerJoined += OnPlayerJoinedNetwork;
            networkManager.playerModule.onPlayerLeft += OnPlayerLeftNetwork;
        }
        else
        {
            Debug.LogError("[MatchSessionManager] NetworkManager.main or playerModule is null!");
        }
    }

    private void OnDestroy()
    {
        var networkManager = NetworkManager.main;
        if (networkManager != null && networkManager.playerModule != null)
        {
            networkManager.playerModule.onPlayerJoined -= OnPlayerJoinedNetwork;
            networkManager.playerModule.onPlayerLeft -= OnPlayerLeftNetwork;
        }
    }

    private void Update()
    {
        if (!isServer) return;

        // Update pings periodically
        if (Time.time - _lastPingUpdate >= pingUpdateInterval)
        {
            UpdateAllPlayerPings();
            _lastPingUpdate = Time.time;
        }
    }

    private void OnPlayerJoinedNetwork(PlayerID player, bool isReconnect, bool asServer)
    {
        if (!asServer) return;

        Debug.Log($"[MatchSessionManager] Player joined: {player}");

        // If reconnecting, mark as connected
        if (_playerStats.ContainsKey(player))
        {
            _playerStats[player].SetConnected(true);
            Debug.Log($"[MatchSessionManager] Player {player} reconnected - restoring stats");
        }
        else
        {
            // Create new player data
            // TODO: Get Steam name and ID from Steam API
            var playerData = new PlayerMatchData(player, 0, $"Player_{player}");
            _playerStats[player] = playerData;
            Debug.Log($"[MatchSessionManager] Created new player data for {player}");
        }

        OnPlayerJoined?.Invoke(player);
    }

    private void OnPlayerLeftNetwork(PlayerID player, bool asServer)
    {
        if (!asServer) return;

        Debug.Log($"[MatchSessionManager] Player left: {player}");

        // Mark as disconnected but keep data for persistence
        if (_playerStats.ContainsKey(player))
        {
            _playerStats[player].SetConnected(false);
        }

        OnPlayerLeft?.Invoke(player);
    }

    // Server-only stat modification methods
    [ServerRpc(requireOwnership: false)]
    public void ReportKill(PlayerID killer, PlayerID victim)
    {
        if (!isServer) return;

        if (_playerStats.TryGetValue(killer, out var killerData))
        {
            killerData.AddKill();
            OnPlayerStatsChanged?.Invoke(killerData);
            Debug.Log($"[MatchSessionManager] {killer} killed {victim}");
        }

        if (_playerStats.TryGetValue(victim, out var victimData))
        {
            victimData.AddDeath();
            OnPlayerStatsChanged?.Invoke(victimData);
        }
    }

    [ServerRpc(requireOwnership: false)]
    public void ReportAssist(PlayerID assister, PlayerID victim)
    {
        if (!isServer) return;

        if (_playerStats.TryGetValue(assister, out var assisterData))
        {
            assisterData.AddAssist();
            OnPlayerStatsChanged?.Invoke(assisterData);
            Debug.Log($"[MatchSessionManager] {assister} assisted on kill of {victim}");
        }
    }

    private void UpdateAllPlayerPings()
    {
        foreach (var kvp in _playerStats)
        {
            PlayerID playerId = kvp.Key;
            PlayerMatchData playerData = kvp.Value;

            if (!playerData.IsConnected) continue;

            // Get ping from PurrNet's connection
            // TODO: Implement actual ping measurement from transport layer
            float ping = GetPlayerPing(playerId);
            playerData.UpdatePing(ping);
        }
    }

    private float GetPlayerPing(PlayerID playerId)
    {
        // TODO: Get actual RTT from PurrNet transport
        // For now, return a placeholder
        return UnityEngine.Random.Range(20f, 100f);
    }

    // Public query methods
    public PlayerMatchData GetPlayerData(PlayerID playerId)
    {
        return _playerStats.TryGetValue(playerId, out var data) ? data : null;
    }

    public List<PlayerMatchData> GetAllPlayers()
    {
        return _playerStats.Values.ToList();
    }

    public List<PlayerMatchData> GetConnectedPlayers()
    {
        return _playerStats.Values.Where(p => p.IsConnected).ToList();
    }

    public List<PlayerMatchData> GetLeaderboard()
    {
        return _playerStats.Values
            .OrderByDescending(p => p.CalculateScore())
            .ToList();
    }

    // Reset all stats (for new match)
    [ServerRpc(requireOwnership: false)]
    public void ResetAllStats()
    {
        if (!isServer) return;

        _playerStats.Clear();
        Debug.Log("[MatchSessionManager] All stats reset");
    }
}
