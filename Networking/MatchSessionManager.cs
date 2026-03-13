using PurrNet;
using PurrNet.Prediction;
using PurrNet.Pooling;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PlayerStatus
{
    Alive,
    Dead,
    Spectating
}

public struct KillInfo
{
    public PlayerID killer;
    public PlayerID victim;
}

/// <summary>
/// A tick-aligned, predicted manager for match session state.
/// Tracks scores, player status, and broadcasts match-critical events.
/// </summary>
public class MatchSessionManager : PredictedIdentity<MatchSessionManager.MatchState>
{
    public static MatchSessionManager Instance { get; private set; }

    // Visual-only lookup for strings (not networked in state)
    private readonly Dictionary<PlayerID, string> _playerNames = new();

    // Events for UI updates (Local only)
    public event Action<PlayerMatchState> OnPlayerStatsChanged;
    public event Action<PlayerID> OnPlayerJoined;
    public event Action<PlayerID> OnPlayerLeft;

    // Tick-aligned Death Event
    [HideInInspector] public PredictedEvent<KillInfo> OnPlayerKilled;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    protected override void LateAwake()
    {
        base.LateAwake();


        OnPlayerKilled = new PredictedEvent<KillInfo>(predictionManager, this);
    }

    protected override void Simulate(ref MatchState state, float delta)
    {
        if (!predictionManager.isServer) return;

        // Sync local player list with PredictionManager's official list
        var currentPlayers = predictionManager.players.currentState.players;
        
        // Add new players to our tracking list
        for (int i = 0; i < currentPlayers.Count; i++)
        {
            PlayerID pid = currentPlayers[i];
            if (!HasPlayer(ref state, pid))
            {
                state.players.Add(new PlayerMatchState { playerId = pid, status = PlayerStatus.Spectating });
                // Note: We don't trigger OnPlayerJoined here because Simulate can run multiple times (rollbacks)
            }
        }
    }

    private bool HasPlayer(ref MatchState state, PlayerID pid)
    {
        for (int i = 0; i < state.players.Count; i++)
        {
            if (state.players[i].playerId == pid) return true;
        }
        return false;
    }

    // Authoritative Scoring (Called by PlayerHealth on Server)
    public void ReportKill(PlayerID killer, PlayerID victim)
    {
        if (!predictionManager.isServer) return;

        ref var state = ref currentState;
        
        for (int i = 0; i < state.players.Count; i++)
        {
            var p = state.players[i];
            if (p.playerId == killer)
            {
                p.kills++;
                state.players[i] = p;
            }
            if (p.playerId == victim)
            {
                p.deaths++;
                p.status = PlayerStatus.Dead;
                state.players[i] = p;
            }
        }

        // Broadcast tick-aligned event
        OnPlayerKilled.Invoke(new KillInfo { killer = killer, victim = victim });

        // Notify Game Mode
        if (BaseGameModeLogic.Instance != null)
        {
            BaseGameModeLogic.Instance.OnPlayerKilled(killer, victim);
        }
    }

    public void UpdatePlayerStatus(PlayerID playerId, PlayerStatus status)
    {
        if (!predictionManager.isServer) return;

        ref var state = ref currentState;
        for (int i = 0; i < state.players.Count; i++)
        {
            if (state.players[i].playerId == playerId)
            {
                var p = state.players[i];
                p.status = status;
                state.players[i] = p;
                break;
            }
        }
    }

    /// <summary>
    /// Update visual-only name mapping. This is not predicted but useful for UI.
    /// </summary>
    public void UpdatePlayerName(PlayerID id, string name)
    {
        _playerNames[id] = name;
    }

    public void UpdatePlayerSteamInfo(PlayerID playerId, ulong steamId, string steamName)
    {
        // For now, we just use the Steam name as the player name
        UpdatePlayerName(playerId, steamName);
        Debug.Log($"[MatchSessionManager] Updated Steam info for {playerId}: {steamName} ({steamId})");
    }

    public string GetPlayerName(PlayerID id)
    {
        if (_playerNames.TryGetValue(id, out var name)) return name;
        return $"Player {id}";
    }

    public List<PlayerMatchState> GetAllPlayers()
    {
        return currentState.players.list.ToList();
    }

    public PlayerMatchState? GetPlayerData(PlayerID id)
    {
        foreach (var p in currentState.players.list)
        {
            if (p.playerId == id) return p;
        }
        return null;
    }

    public List<PlayerMatchState> GetLeaderboard()
    {
        return currentState.players.list
            .OrderByDescending(p => CalculateScore(p))
            .ToList();
    }

    public static float CalculateScore(PlayerMatchState p)
    {
        return (p.kills * 10f) + (p.assists * 3f) - (p.deaths * 5f);
    }

    public static float GetKDA(PlayerMatchState p)
    {
        if (p.deaths == 0) return p.kills + p.assists;
        return (p.kills + p.assists) / (float)p.deaths;
    }

    public void RequestEndMatch()
    {
        if (!predictionManager.isServer) return;

        var sm = FindObjectOfType<PurrNet.Prediction.StateMachine.PredictedStateMachine>();
        if (sm != null)
        {
            var endState = sm.GetComponent<GameEndedState>();
            if (endState != null) sm.SetState(endState);
            else sm.Next();
        }
    }

    protected override MatchState GetInitialState()
    {
        return new MatchState
        {
            players = DisposableList<PlayerMatchState>.Create(16),
            matchTimer = 0f
        };
    }

    public struct PlayerMatchState : IPredictedData<PlayerMatchState>
    {
        public PlayerID playerId;
        public int kills;
        public int deaths;
        public int assists;
        public PlayerStatus status;

        public void Dispose() { }
    }

    public struct MatchState : IPredictedData<MatchState>
    {
        public DisposableList<PlayerMatchState> players;
        public float matchTimer;

        public void Dispose()
        {
            if (players.list != null) players.Dispose();
        }
    }
}
