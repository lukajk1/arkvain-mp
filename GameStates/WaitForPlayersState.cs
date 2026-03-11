using PurrNet;
using PurrNet.Pooling;
using PurrNet.Prediction;
using PurrNet.Prediction.StateMachine;
using System.Collections.Generic;
using UnityEngine;

public class WaitForPlayersState : PredictedStateNode<WaitForPlayersState.WaitState>
{
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private GameRunningState _matchRunningState;

    private int _lastPlayerCount = -1;

    public override void Enter()
    {
        Debug.Log($"[WaitForPlayersState] Entered waiting for players state.");
        _lastPlayerCount = -1;
        
        if (predictionManager.players != null)
        {
            predictionManager.players.onPlayerAdded += OnPlayerAdded;
        }

        // Initial UI update
        UpdateUI();
    }

    private void OnPlayerAdded(PlayerID player)
    {
        // Spawning handled in StateSimulate once ready
        UpdateUI();
    }

    private void SpawnPlayer(PlayerID player, int index)
    {
        MapData mapData = MapLoader.Instance.CurrentMapData;
        int teamIndex = index % 2;
        Transform spawnPoint = mapData.GetSpawnPointSequential(index, teamIndex);

        Debug.Log($"[WaitForPlayersState] Server spawning player {player} at {spawnPoint.position}");

        var newPlayer = hierarchy.Create(_playerPrefab, spawnPoint.position, spawnPoint.rotation, player);
        if (newPlayer.HasValue)
        {
            predictionManager.SetOwnership(newPlayer, player);
            PlayerInfoManager.Register(player);
            _matchRunningState.OnPlayerSpawned(player, newPlayer.Value);
            GameEvents.OnPlayerSpawned?.Invoke(player);
        }
    }

    private bool IsReadyForSpawning()
    {
        bool isMapLoaded = MapLoader.Instance != null && MapLoader.Instance.CurrentMapData != null && !MapLoader.Instance.IsLoading;
        bool isLogicReady = BaseGameModeLogic.Instance != null || FindAnyObjectByType<BaseGameModeLogic>() != null;

        return (isMapLoaded && isLogicReady); 
    }

    protected override void StateSimulate(ref WaitState state, float delta)
    {
        // 0. Safety Check: Ensure state is fully initialized
        if (!state.isInitialized) return;

        // 1. Ready Checks: Return early if local environment isn't initialized
        if (!IsReadyForSpawning()) return;

        // 2. Logic Readiness: Ensure singleton is available (even via scene search)
        var logic = BaseGameModeLogic.Instance;
        // fallback
        if (logic == null) logic = FindAnyObjectByType<BaseGameModeLogic>();
        if (logic == null) return;

        // Always update UI while in this state
        UpdateUI();

        // 3. Spawning: Iterate through prediction manager player list
        if (predictionManager.isServer)
        {
            var players = predictionManager.players.currentState.players;
            for (var i = 0; i < players.Count; i++)
            {
                PlayerID player = players[i];
                if (!state.spawnedPlayers.Contains(player))
                {
                    SpawnPlayer(player, i);
                    state.spawnedPlayers.Add(player);
                }
            }
        }

        // 4. Transition: Check if match start conditions are met
        int required = logic.MinPlayersToStart;
        if (predictionManager.players.currentState.players.Count >= required)
        {
            Debug.Log("[WaitForPlayersState] Wait condition met - proceeding to match.");
            machine.Next();
        }
    }

    private void UpdateUI()
    {
        if (GameStateUI.Instance == null) return;

        var players = predictionManager.players.currentState.players;
        if (players.Count != _lastPlayerCount)
        {
            _lastPlayerCount = players.Count;
            
            var logic = BaseGameModeLogic.Instance;
            if (logic == null) logic = FindAnyObjectByType<BaseGameModeLogic>();

            if (logic != null)
            {
                int required = logic.MinPlayersToStart;
                GameStateUI.Instance.UpdateWaitingStatus(_lastPlayerCount, required);
            }
            else
            {
                GameStateUI.Instance.UpdateStatus($"Waiting for players... ({_lastPlayerCount})");
            }
        }
    }

    public override void Exit()
    {
        if (predictionManager.players != null)
        {
            predictionManager.players.onPlayerAdded -= OnPlayerAdded;
        }

        if (GameStateUI.Instance != null)
        {
            GameStateUI.Instance.UpdateStatus("", false);
        }
    }

    protected override WaitState GetInitialState()
    {
        return new WaitState()
        {
            spawnedPlayers = DisposableList<PlayerID>.Create(),
            isInitialized = true
        };
    }

    public struct WaitState : IPredictedData<WaitState>
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
