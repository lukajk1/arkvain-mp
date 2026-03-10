using PurrNet;
using PurrNet.Prediction;
using PurrNet.Prediction.StateMachine;
using System.Collections.Generic;
using UnityEngine;

public class WaitForPlayersState : PredictedStateNode<WaitForPlayersState.WaitState>
{
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private GameRunningState _matchRunningState;

    private HashSet<PlayerID> _spawnedPlayers = new HashSet<PlayerID>();
    private int _lastPlayerCount = -1;

    public override void Enter()
    {
        Debug.Log($"[WaitForPlayersState] Entered waiting for players state.");
        _lastPlayerCount = -1;
        
        // Initial UI update
        UpdateUI();
    }

    protected override void StateSimulate(ref WaitState state, float delta)
    {
        bool isMapLoaded = MapLoader.Instance != null && MapLoader.Instance.CurrentMapData != null && !MapLoader.Instance.IsLoading;
        bool isLogicReady = BaseGameModeLogic.Instance != null;

        // Always update UI while in this state
        UpdateUI();

        if (isMapLoaded)
        {
            // Immediate spawn logic: check for players who haven't spawned yet
            var players = predictionManager.players.currentState.players;
            for (var i = 0; i < players.Count; i++)
            {
                PlayerID player = players[i];
                if (!_spawnedPlayers.Contains(player))
                {
                    SpawnPlayer(player, i);
                    _spawnedPlayers.Add(player);
                }
            }
        }

        if (isMapLoaded && isLogicReady)
        {
            int required = BaseGameModeLogic.Instance.MinPlayersToStart;
            if (predictionManager.players.currentState.players.Count >= required)
            {
                Debug.Log("[WaitForPlayersState] Win condition met - proceeding to match.");
                machine.Next();
            }
        }
    }

    private void UpdateUI()
    {
        if (GameStateUI.Instance == null) return;

        var players = predictionManager.players.currentState.players;
        if (players.Count != _lastPlayerCount)
        {
            _lastPlayerCount = players.Count;
            
            // Try singleton first, then direct scene search
            var logic = BaseGameModeLogic.Instance;
            if (logic == null) logic = FindObjectOfType<BaseGameModeLogic>();

            if (logic != null)
            {
                int required = logic.MinPlayersToStart;
                GameStateUI.Instance.UpdateWaitingStatus(_lastPlayerCount, required);
            }
            else
            {
                // Fallback if logic hasn't spawned yet
                GameStateUI.Instance.UpdateStatus($"Waiting for players... ({_lastPlayerCount})");
            }
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
            _matchRunningState.OnPlayerSpawned(player, newPlayer.Value);
            GameEvents.OnPlayerSpawned?.Invoke(player);
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
