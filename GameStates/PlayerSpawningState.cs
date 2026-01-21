using NUnit.Framework;
using PurrNet.Prediction;
using PurrNet.Prediction.StateMachine;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawningState : PredictedStateNode<PlayerSpawningState.SpawnState>
{
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] RoundRunningState _roundRunningState;

    private int _currentSpawnPoint;

    public override void Enter()
    {

        for (var i = 0; i < predictionManager.players.currentState.players.Count; i++)
        {
            PredictedObjectID? newPlayer;

            var player = predictionManager.players.currentState.players[i];

            if (spawnPoints.Count > 0)
            {
                var spawnPoint = spawnPoints[_currentSpawnPoint];
                _currentSpawnPoint = (_currentSpawnPoint + 1) % spawnPoints.Count;
                newPlayer = hierarchy.Create(_playerPrefab, spawnPoint.position, spawnPoint.rotation, player);
            }
            else
            {
                newPlayer = hierarchy.Create(_playerPrefab, owner: player);
            }

            if (!newPlayer.HasValue)
                return;

            predictionManager.SetOwnership(newPlayer, player);
            _roundRunningState.OnPlayerSpawned(player, newPlayer.Value);
        }

        machine.Next();
    }

    public struct SpawnState : IPredictedData<SpawnState>
    {
        public void Dispose() { }
    }
}
