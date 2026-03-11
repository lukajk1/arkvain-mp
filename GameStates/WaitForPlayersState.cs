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
    private bool _wasMapReady = false;

    public override void Enter()
    {
        Debug.Log($"[WaitForPlayersState] Entered waiting for players state.");
        _lastPlayerCount = -1;
        _wasMapReady = false;
        
        if (predictionManager.players != null)
        {
            predictionManager.players.onPlayerAdded += OnPlayerAdded;
            predictionManager.players.onPlayerRemoved += OnPlayerRemoved;
        }

        // Initial UI update and check
        UpdateUI();
        PollForNewPlayers();
    }

    private void OnPlayerAdded(PlayerID player)
    {
        PollForNewPlayers();
        UpdateUI();
    }

    private void OnPlayerRemoved(PlayerID player)
    {
        if (!_spawnedPlayers.Contains(player))
        {
            _spawnedPlayers.Remove(player);
        }
    }

    private void PollForNewPlayers()
    {
        if (!predictionManager.isServer) return;

        bool isMapLoaded = MapLoader.Instance != null && MapLoader.Instance.CurrentMapData != null && !MapLoader.Instance.IsLoading;
        if (!isMapLoaded) return;

        var players = predictionManager.players.currentState.players;
        bool spawnedAny = false;
        for (var i = 0; i < players.Count; i++)
        {
            PlayerID player = players[i];
            if (!_spawnedPlayers.Contains(player))
            {
                SpawnPlayer(player, i);
                _spawnedPlayers.Add(player);
                spawnedAny = true;
            }
        }

        if (spawnedAny) UpdateUI();
    }

    protected override void StateSimulate(ref WaitState state, float delta)
    {
        bool isMapLoaded = MapLoader.Instance != null && MapLoader.Instance.CurrentMapData != null && !MapLoader.Instance.IsLoading;
        bool isLogicReady = BaseGameModeLogic.Instance != null;

        // If map just finished loading, catch up on spawns
        if (isMapLoaded && !_wasMapReady)
        {
            _wasMapReady = true;
            PollForNewPlayers();
            UpdateUI();
        }

        if (isMapLoaded && isLogicReady)
        {
            int required = BaseGameModeLogic.Instance.MinPlayersToStart;
            if (predictionManager.players.currentState.players.Count >= required)
            {
                Debug.Log("[WaitForPlayersState] wait condition met - proceeding to match.");
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
            
            // Try singleton first, then direct scene search (singleton might not be set yet during replication)
            var logic = BaseGameModeLogic.Instance;
            if (logic == null) logic = FindAnyObjectByType<BaseGameModeLogic>();

            if (logic != null)
            {
                int required = logic.MinPlayersToStart;
                GameStateUI.Instance.UpdateWaitingStatus(_lastPlayerCount, required);
            }
            else
            {
                // Fallback if logic hasn't spawned/replicated yet
                GameStateUI.Instance.UpdateStatus($"Waiting for players... ({_lastPlayerCount})");
            }
        }
    }

    private void SpawnPlayer(PlayerID player, int index)
    {
        Debug.Log("SPAWNING PLAYER");

        MapData mapData = MapLoader.Instance.CurrentMapData;
        int teamIndex = index % 2;
        Transform spawnPoint = mapData.GetSpawnPointSequential(index, teamIndex);

        Debug.Log($"[WaitForPlayersState] Immediate spawn for player {player} at {spawnPoint.position}");

        var newPlayer = hierarchy.Create(_playerPrefab, spawnPoint.position, spawnPoint.rotation, player);
        if (newPlayer.HasValue)
        {
            Debug.Log("SPAWNING PLAYER PLAYER HAS VALUE");

            predictionManager.SetOwnership(newPlayer, player);
            PlayerInfoManager.Register(player);
            _matchRunningState.OnPlayerSpawned(player, newPlayer.Value);
            GameEvents.OnPlayerSpawned?.Invoke(player);
        }
        else
        {
            Debug.Log("SPAWNING PLAYER PLAYER DOES NOT HAVE VALUE");

        }
    }

    public override void Exit()
    {
        if (predictionManager.players != null)
        {
            predictionManager.players.onPlayerAdded -= OnPlayerAdded;
            predictionManager.players.onPlayerRemoved -= OnPlayerRemoved;

        }

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
