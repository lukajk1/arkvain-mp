using NUnit.Framework;
using PurrNet;
using PurrNet.Prediction;
using PurrNet.Prediction.StateMachine;
using System.Collections.Generic;
using UnityEngine;

public class OldSpawningState : PredictedStateNode<OldSpawningState.SpawnState>
{
    [SerializeField] private GameObject _playerPrefab;

    public override void ViewEnter(bool isVerified)
    {
        if (!isVerified) return;

        if (MapLoader.Instance == null || MapLoader.Instance.CurrentMapData == null)
        {
            Debug.LogError("[OldSpawningState] MapLoader or MapData is missing!");
            machine.Next();
            return;
        }

        MapData mapData = MapLoader.Instance.CurrentMapData;
        var players = predictionManager.players.currentState.players;

        for (var i = 0; i < players.Count; i++)
        {
            PlayerID player = players[i];
            
            int teamIndex = i % 2;
            Transform spawnPoint = mapData.GetSpawnPointSequential(i, teamIndex);

            Debug.Log($"[OldSpawningState] Spawning player {player} at {spawnPoint.position}");

            var newPlayer = hierarchy.Create(_playerPrefab, spawnPoint.position, spawnPoint.rotation, player);
            if (newPlayer.HasValue)
            {
                predictionManager.SetOwnership(newPlayer, player);
            }
        }
    }

    public struct SpawnState : IPredictedData<SpawnState>
    {
        public void Dispose() { }
    }
}