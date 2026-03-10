using PurrNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages the networked state of a match session, including player lifecycle (joining/leaving),
/// synchronized player statistics (kills, deaths, assists), and loadout configuration.
/// Acts as a central hub for reporting gameplay events to the server and broadcasting 
/// session-wide updates to all clients via SyncLists and events.
/// </summary>
public class MatchSessionManager : NetworkBehaviour
{
    public static MatchSessionManager Instance { get; private set; }

    private readonly SyncList<PlayerMatchData> _playerStats = new();

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
        _playerStats.onChanged += OnStatsListChanged;
    }

    private void Start()
    {
        StartCoroutine(WaitForPlayerModule());
    }

    private IEnumerator WaitForPlayerModule()
    {
        // Wait until NetworkManager and playerModule are initialized
        while (NetworkManager.main == null || NetworkManager.main.playerModule == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        if (isServer)
        {
            NetworkManager.main.playerModule.onPlayerJoined += OnPlayerJoinedNetwork;
            NetworkManager.main.playerModule.onPlayerLeft += OnPlayerLeftNetwork;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_playerStats != null)
            _playerStats.onChanged -= OnStatsListChanged;

        var networkManager = NetworkManager.main;
        if (isServer && networkManager != null && networkManager.playerModule != null)
        {
            networkManager.playerModule.onPlayerJoined -= OnPlayerJoinedNetwork;
            networkManager.playerModule.onPlayerLeft -= OnPlayerLeftNetwork;
        }
    }

    private void OnStatsListChanged(SyncListChange<PlayerMatchData> change)
    {
        if (change.operation == SyncListOperation.Set || change.operation == SyncListOperation.Added)
        {
            OnPlayerStatsChanged?.Invoke(change.value);
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

        int index = FindPlayerIndex(player);
        if (index != -1)
        {
            _playerStats[index].SetConnected(true);
            _playerStats.SetDirty(index);
        }
        else
        {
            var playerData = new PlayerMatchData(player, 0, $"Player_{player}");
            _playerStats.Add(playerData);
        }

        OnPlayerJoined?.Invoke(player);
    }

    private void OnPlayerLeftNetwork(PlayerID player, bool asServer)
    {
        if (!asServer) return;

        int index = FindPlayerIndex(player);
        if (index != -1)
        {
            _playerStats[index].SetConnected(false);
            _playerStats.SetDirty(index);
        }

        OnPlayerLeft?.Invoke(player);
    }

    private int FindPlayerIndex(PlayerID playerId)
    {
        for (int i = 0; i < _playerStats.Count; i++)
        {
            if (_playerStats[i].PlayerId == playerId)
                return i;
        }
        return -1;
    }

    // Server-only stat modification methods
    [ServerRpc(requireOwnership: false)]
    public void ReportKill(PlayerID killer, PlayerID victim)
    {
        if (!isServer) return;

        int killerIdx = FindPlayerIndex(killer);
        if (killerIdx != -1)
        {
            _playerStats[killerIdx].AddKill();
            _playerStats.SetDirty(killerIdx);
        }

        int victimIdx = FindPlayerIndex(victim);
        if (victimIdx != -1)
        {
            _playerStats[victimIdx].AddDeath();
            _playerStats.SetDirty(victimIdx);
        }

        // Notify the current game mode logic
        if (BaseGameModeLogic.Instance != null)
        {
            BaseGameModeLogic.Instance.OnPlayerKilled(killer, victim);
        }
    }

    /// <summary>
    /// Called by GameModeLogic to request the match to end.
    /// </summary>
    public void RequestEndMatch()
    {
        if (!isServer) return;

        var sm = FindObjectOfType<PurrNet.Prediction.StateMachine.PredictedStateMachine>();
        if (sm != null)
        {
            var endState = sm.GetComponent<GameEndedState>();
            if (endState != null)
            {
                Debug.Log("[MatchSessionManager] Explicitly setting state to GameEndedState.");
                sm.SetState(endState);
            }
            else
            {
                Debug.LogWarning("[MatchSessionManager] GameEndedState component not found on StateMachine! Falling back to Next().");
                sm.Next();
            }
        }
    }

    [ServerRpc(requireOwnership: false)]
    public void ReportAssist(PlayerID assister, PlayerID victim)
    {
        if (!isServer) return;

        int assisterIdx = FindPlayerIndex(assister);
        if (assisterIdx != -1)
        {
            _playerStats[assisterIdx].AddAssist();
            _playerStats.SetDirty(assisterIdx);
        }
    }

    private void UpdateAllPlayerPings()
    {
        for (int i = 0; i < _playerStats.Count; i++)
        {
            PlayerMatchData playerData = _playerStats[i];
            if (!playerData.IsConnected) continue;

            float ping = GetPlayerPing(playerData.PlayerId);
            playerData.UpdatePing(ping);
            _playerStats.SetDirty(i);
        }
    }

    private float GetPlayerPing(PlayerID playerId)
    {
        return UnityEngine.Random.Range(20f, 100f);
    }

    // Public query methods
    public PlayerMatchData GetPlayerData(PlayerID playerId)
    {
        return _playerStats.FirstOrDefault(p => p.PlayerId == playerId);
    }

    public List<PlayerMatchData> GetAllPlayers()
    {
        return _playerStats.ToList();
    }

    public List<PlayerMatchData> GetConnectedPlayers()
    {
        return _playerStats.Where(p => p.IsConnected).ToList();
    }

    public List<PlayerMatchData> GetLeaderboard()
    {
        return _playerStats
            .OrderByDescending(p => p.CalculateScore())
            .ToList();
    }

    public void UpdatePlayerSteamInfo(PlayerID playerId, ulong steamId, string steamName)
    {
        if (!isServer) return;

        int index = FindPlayerIndex(playerId);
        if (index != -1)
        {
            _playerStats[index].UpdateSteamInfo(steamId, steamName);
            _playerStats.SetDirty(index);
            Debug.Log($"[MatchSessionManager] Updated Steam info for {playerId}: {steamName} ({steamId})");
        }
    }

    [ServerRpc(requireOwnership: false)]
    public void UpdatePlayerLoadout(PlayerID playerId, LoadoutSelection selection)
    {
        if (!isServer) return;

        // Basic validation
        if (!System.Enum.IsDefined(typeof(HeroType), selection.Hero) || 
            !System.Enum.IsDefined(typeof(WeaponType), selection.Weapon))
        {
            Debug.LogWarning($"[MatchSessionManager] Received invalid loadout from {playerId}");
            return;
        }

        int index = FindPlayerIndex(playerId);
        if (index != -1)
        {
            _playerStats[index].UpdateLoadout(selection);
            _playerStats.SetDirty(index);
            Debug.Log($"[MatchSessionManager] Updated loadout for {playerId}: Hero={selection.Hero}, Weapon={selection.Weapon}");
        }
    }

    [ServerRpc(requireOwnership: false)]
    public void ResetAllStats()
    {
        if (!isServer) return;
        _playerStats.Clear();
    }
}
