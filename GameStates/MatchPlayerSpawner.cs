using PurrNet;
using PurrNet.Pooling;
using PurrNet.Prediction;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A standalone predicted spawner that ensures all connected players have a physical body.
/// Manages player lifecycle, including spawning on join and delayed destruction (2s) on disconnect.
/// </summary>
public class MatchPlayerSpawner : PredictedIdentity<MatchPlayerSpawner.SpawnState>
{
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private float _disconnectCleanupDelay = 2.0f;
    [SerializeField] private float _respawnDelay = 3.0f;

    protected override void LateAwake()
    {
        base.LateAwake();
        
        // Register this spawner with the global match manager
        MatchSessionManager.RegisterKilledListener(OnPlayerKilled);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        MatchSessionManager.UnregisterKilledListener(OnPlayerKilled);
    }

    private void OnPlayerKilled(KillInfo info)
    {
        if (!predictionManager.isServer) return;

        // Add to respawn queue
        currentState.pendingRespawns[info.victim] = _respawnDelay;
        Debug.Log($"[MatchPlayerSpawner] Player {info.victim} killed. Queuing respawn in {_respawnDelay}s");
    }

    protected override void Simulate(ref SpawnState state, float delta)
    {
        if (!state.isInitialized) return;

        // 1. Spawning Logic (Server Only)
        if (predictionManager.isServer)
        {
            var currentPlayers = predictionManager.players.currentState.players;
            
            // Handle Initial Spawning
            if (IsReadyForSpawning())
            {
                for (var i = 0; i < currentPlayers.Count; i++)
                {
                    PlayerID player = currentPlayers[i];
                    
                    // If we don't have a body for this player, AND they aren't waiting to respawn
                    if (!state.spawnedBodies.ContainsKey(player) && !state.pendingRespawns.ContainsKey(player))
                    {
                        SpawnPlayer(player, i, ref state);
                    }
                    
                    // If player was previously disconnecting but came back, stop the timer
                    if (state.cleanupTimers.ContainsKey(player))
                    {
                        state.cleanupTimers.Remove(player);
                        Debug.Log($"[MatchPlayerSpawner] Player {player} reconnected before cleanup. Timer cleared.");
                    }
                }
            }

            // 2. Process Respawn Timers
            var toRespawn = ListPool<PlayerID>.Instantiate();
            var respawnKeys = ListPool<PlayerID>.Instantiate();
            foreach(var kvp in state.pendingRespawns) respawnKeys.Add(kvp.Key);

            foreach (var id in respawnKeys)
            {
                float remaining = state.pendingRespawns[id] - delta;
                if (remaining <= 0)
                {
                    toRespawn.Add(id);
                }
                else
                {
                    state.pendingRespawns[id] = remaining;
                }
            }

            foreach (var id in toRespawn)
            {
                state.pendingRespawns.Remove(id);
                Debug.Log($"[MatchPlayerSpawner] Respawn timer expired for {id}. Spawning fresh body.");
                
                // Find index in player list for spawn point selection
                int idx = -1;
                for (int i = 0; i < currentPlayers.Count; i++)
                {
                    if (currentPlayers[i] == id) { idx = i; break; }
                }
                
                if (idx != -1) SpawnPlayer(id, idx, ref state);
            }
            ListPool<PlayerID>.Destroy(toRespawn);
            ListPool<PlayerID>.Destroy(respawnKeys);

            // 3. Disconnection Detection & Timing
            // ... (rest of disconnection logic)
            // Detect players who are in spawnedBodies but NO LONGER in the predictionManager's player list
            var toStartTimer = ListPool<PlayerID>.Instantiate();
            foreach (var kvp in state.spawnedBodies)
            {
                bool stillConnected = false;
                for (int i = 0; i < currentPlayers.Count; i++)
                {
                    if (currentPlayers[i] == kvp.Key)
                    {
                        stillConnected = true;
                        break;
                    }
                }

                if (!stillConnected && !state.cleanupTimers.ContainsKey(kvp.Key))
                {
                    toStartTimer.Add(kvp.Key);
                }
            }

            foreach (var id in toStartTimer)
            {
                state.cleanupTimers[id] = _disconnectCleanupDelay;
                Debug.Log($"[MatchPlayerSpawner] Player {id} disconnected. Starting {_disconnectCleanupDelay}s cleanup timer.");
            }
            ListPool<PlayerID>.Destroy(toStartTimer);

            // 3. Process Active Cleanup Timers
            var toDelete = ListPool<PlayerID>.Instantiate();
            var timerKeys = ListPool<PlayerID>.Instantiate();
            
            // Get keys first to avoid modification during iteration
            foreach (var kvp in state.cleanupTimers) timerKeys.Add(kvp.Key);

            foreach (var id in timerKeys)
            {
                float remaining = state.cleanupTimers[id] - delta;
                if (remaining <= 0)
                {
                    toDelete.Add(id);
                }
                else
                {
                    state.cleanupTimers[id] = remaining;
                }
            }

            // Execute physical deletion and state cleanup
            foreach (var id in toDelete)
            {
                if (state.spawnedBodies.TryGetValue(id, out var bodyId))
                {
                    Debug.Log($"[MatchPlayerSpawner] Cleanup timer expired for {id}. Deleting player body.");
                    hierarchy.Delete(bodyId);
                    state.spawnedBodies.Remove(id);
                }
                state.cleanupTimers.Remove(id);
            }

            ListPool<PlayerID>.Destroy(toDelete);
            ListPool<PlayerID>.Destroy(timerKeys);
        }
    }

    private bool IsReadyForSpawning()
    {
        return MapLoader.Instance != null && MapLoader.Instance.CurrentMapData != null && !MapLoader.Instance.IsLoading;
    }

    private void SpawnPlayer(PlayerID player, int listIndex, ref SpawnState state)
    {
        MapData mapData = MapLoader.Instance.CurrentMapData;
        
        // Use the persistent player ID value for the spawn index to ensure 
        // reconnecting players target the same spawn point.
        int spawnIndex = (int)(player.id.value % int.MaxValue);
        int teamIndex = spawnIndex % 2;
        
        Transform spawnPoint = mapData.GetSpawnPointSequential(spawnIndex, teamIndex);

        Debug.Log($"[MatchPlayerSpawner] Server spawning player {player} (SpawnIdx: {spawnIndex}) at {spawnPoint.position}");

        var newPlayer = hierarchy.Create(_playerPrefab, spawnPoint.position, spawnPoint.rotation, player);
        if (newPlayer.HasValue)
        {
            predictionManager.SetOwnership(newPlayer, player);
            PlayerInfoManager.Register(player);
            
            if (MatchSessionManager.Instance != null)
                MatchSessionManager.Instance.UpdatePlayerStatus(player, PlayerStatus.Alive);
                
            GameEvents.OnPlayerSpawned?.Invoke(player);
            state.spawnedBodies[player] = newPlayer.Value;
        }
    }

    protected override SpawnState GetInitialState()
    {
        return new SpawnState()
        {
            spawnedBodies = DisposableDictionary<PlayerID, PredictedObjectID>.Create(),
            cleanupTimers = DisposableDictionary<PlayerID, float>.Create(),
            pendingRespawns = DisposableDictionary<PlayerID, float>.Create(),
            isInitialized = true
        };
    }

    public struct SpawnState : IPredictedData<SpawnState>
    {
        public DisposableDictionary<PlayerID, PredictedObjectID> spawnedBodies;
        public DisposableDictionary<PlayerID, float> cleanupTimers;
        public DisposableDictionary<PlayerID, float> pendingRespawns;
        public bool isInitialized;

        public void Dispose()
        {
            if (spawnedBodies.dictionary != null) spawnedBodies.Dispose();
            if (cleanupTimers.dictionary != null) cleanupTimers.Dispose();
            if (pendingRespawns.dictionary != null) pendingRespawns.Dispose();
        }
    }
}
