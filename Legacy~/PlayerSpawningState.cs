using NUnit.Framework;
using PurrNet;
using PurrNet.Prediction;
using PurrNet.Prediction.StateMachine;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawningState : PredictedStateNode<PlayerSpawningState.SpawnState>
{
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] MatchRunningState _matchRunningState;
    [SerializeField] private ScoreboardUI_1v1 _scoreboard;

    public override void ViewEnter(bool isVerified)
    {
        if (!isVerified) return;

        MapData mapData = MapLoader.Instance.CurrentMapData;
        if (mapData == null)
        {
            Debug.LogError("[PlayerSpawningState] MapData is null during spawn!");
            return;
        }

        for (var i = 0; i < predictionManager.players.currentState.players.Count; i++)
        {
            PlayerID player = predictionManager.players.currentState.players[i];
            
            // Alternating teams based on index
            int teamIndex = i % 2;
            
            // Use sequential, deterministic spawning for networking
            Transform spawnPoint = mapData.GetSpawnPointSequential(i, teamIndex);

            Debug.Log($"[PlayerSpawningState] Spawning player {player} at {spawnPoint.position} (Team {teamIndex}, MapData: {mapData.name})");

            PredictedObjectID? newPlayer;
            newPlayer = hierarchy.Create(_playerPrefab, spawnPoint.position, spawnPoint.rotation, player);

            if (!newPlayer.HasValue)
                continue;

            predictionManager.SetOwnership(newPlayer, player);

            // Register player info
            PlayerInfoManager.Register(player);

            _matchRunningState.OnPlayerSpawned(player, newPlayer.Value);
        }

        // Refresh scoreboard to show player names immediately
        if (_scoreboard != null)
        {
            _scoreboard.RefreshScoreboard();
        }

        machine.Next();
    }

    public struct SpawnState : IPredictedData<SpawnState>
    {
        public void Dispose() { }
    }
}
