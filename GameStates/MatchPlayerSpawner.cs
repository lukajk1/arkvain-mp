using PurrNet;
using PurrNet.Pooling;
using PurrNet.Prediction;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A standalone predicted spawner that ensures all connected players have a physical body.
/// This runs independently of the match state machine, making it robust for late-joiners.
/// </summary>
public class MatchPlayerSpawner : PredictedIdentity<MatchPlayerSpawner.SpawnState>
{
    [SerializeField] private GameObject _playerPrefab;

    protected override void LateAwake()
    {
        base.LateAwake();
        
        if (predictionManager.players != null)
        {
            predictionManager.players.onPlayerAdded += OnPlayerAdded;
        }
    }

    private void OnPlayerAdded(PlayerID player)
    {
        // Spawning will be picked up in the next Simulate() tick
    }

    protected override void Simulate(ref SpawnState state, float delta)
    {
        // 0. Safety Check
        if (!state.isInitialized) return;

        // 1. Ready Checks: Return early if local environment isn't initialized
        if (!IsReadyForSpawning()) return;

        // 2. Spawning: Iterate through prediction manager player list (Server Only)
        if (predictionManager.isServer)
        {
            var players = predictionManager.players.currentState.players;
            for (var i = 0; i < players.Count; i++)
            {
                PlayerID player = players[i];
                if (!state.spawnedPlayers.Contains(player))
                {
                    SpawnPlayer(player, i, ref state);
                }
            }
        }
    }

    private bool IsReadyForSpawning()
    {
        bool isMapLoaded = MapLoader.Instance != null && MapLoader.Instance.CurrentMapData != null && !MapLoader.Instance.IsLoading;
        // GameModeLogic is not strictly required for spawning bodies, but good for team indices
        return isMapLoaded;
    }

    private void SpawnPlayer(PlayerID player, int index, ref SpawnState state)
    {
        MapData mapData = MapLoader.Instance.CurrentMapData;
        int teamIndex = index % 2;
        Transform spawnPoint = mapData.GetSpawnPointSequential(index, teamIndex);

        Debug.Log($"[MatchPlayerSpawner] Server spawning player {player} at {spawnPoint.position}");

        var newPlayer = hierarchy.Create(_playerPrefab, spawnPoint.position, spawnPoint.rotation, player);
        if (newPlayer.HasValue)
        {
            predictionManager.SetOwnership(newPlayer, player);
            PlayerInfoManager.Register(player);
            
            GameEvents.OnPlayerSpawned?.Invoke(player);
            state.spawnedPlayers.Add(player);
        }
    }

    protected override SpawnState GetInitialState()
    {
        return new SpawnState()
        {
            spawnedPlayers = DisposableList<PlayerID>.Create(),
            isInitialized = true
        };
    }

    public struct SpawnState : IPredictedData<SpawnState>
    {
        public DisposableList<PlayerID> spawnedPlayers;
        public bool isInitialized;

        public void Dispose()
        {
            if (spawnedPlayers.list != null)
                spawnedPlayers.Dispose();
        }
    }
}
