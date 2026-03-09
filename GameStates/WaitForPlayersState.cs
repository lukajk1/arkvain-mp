using PurrNet;
using PurrNet.Prediction;
using PurrNet.Prediction.StateMachine;
using System.Collections.Generic;
using UnityEngine;

public class WaitForPlayersState : PredictedStateNode<WaitForPlayersState.WaitState>
{
    [SerializeField] private int _expectedPlayers;
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private RoundRunningState _roundRunningState;

    private HashSet<PlayerID> _spawnedPlayers = new HashSet<PlayerID>();

    public override void Enter()
    {
        Debug.Log($"[WaitForPlayersState] Entered waiting for players state.");
        
        if (GameStateUI.Instance != null)
        {
            GameStateUI.Instance.UpdateStatus("Waiting for players...");
        }
    }

    protected override void StateSimulate(ref WaitState state, float delta)
    {
        bool isMapLoaded = MapLoader.Instance != null && MapLoader.Instance.CurrentMapData != null && !MapLoader.Instance.IsLoading;

        if (isMapLoaded)
        {
            // Immediate spawn logic: check for players who haven't spawned yet
            for (var i = 0; i < predictionManager.players.currentState.players.Count; i++)
            {
                PlayerID player = predictionManager.players.currentState.players[i];
                if (!_spawnedPlayers.Contains(player))
                {
                    SpawnPlayer(player, i);
                    _spawnedPlayers.Add(player);
                }
            }
        }

        if (predictionManager.players.currentState.players.Count >= _expectedPlayers && isMapLoaded)
        {
            Debug.Log("[WaitForPlayersState] All players joined and map is loaded - proceeding to next state.");
            machine.Next();
        }
    }

    private void SpawnPlayer(PlayerID player, int index)
    {
        MapData mapData = MapLoader.Instance.CurrentMapData;
        int teamIndex = index % 2;
        Transform spawnPoint = mapData.GetSpawnPointSequential(index, teamIndex);

        Debug.Log($"[WaitForPlayersState] Immediate spawn for player {player} at {spawnPoint.position}");

        var newPlayer = hierarchy.Create(_playerPrefab, spawnPoint.position, spawnPoint.rotation, player);
        if (newPlayer.HasValue)
        {
            predictionManager.SetOwnership(newPlayer, player);
            PlayerInfoManager.Register(player);
            _roundRunningState.OnPlayerSpawned(player, newPlayer.Value);
        }
    }

    public override void Exit()
    {
        if (GameStateUI.Instance != null)
        {
            GameStateUI.Instance.UpdateStatus("", false);
        }
    }
    public struct WaitState : IPredictedData<WaitState>
    {
        public void Dispose() { }
    }
}
