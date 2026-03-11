using PurrNet;
using PurrNet.Pooling;
using PurrNet.Prediction;
using PurrNet.Prediction.StateMachine;
using UnityEngine;

public class GameRunningState : PredictedStateNode<GameRunningState.MatchState>
{
    public override void Enter()
    {
        Debug.Log("[MatchRunningState] Match is officially starting!");
        
        if (BaseGameModeLogic.Instance != null)
        {
            BaseGameModeLogic.Instance.OnMatchStarted();
        }
    }

    protected override MatchState GetInitialState()
    {
        return new MatchState()
        {
            playersAlive = DisposableDictionary<PlayerID, PredictedObjectID>.Create()
        };
    }

    public override void Exit()
    {
        currentState.playersAlive.Clear();
        
        if (BaseGameModeLogic.Instance != null)
        {
            BaseGameModeLogic.Instance.OnMatchEnded();
        }
    }

    protected override void StateSimulate(ref MatchState state, float delta)
    {
        // PurrDiction: Tick-aligned player death detection
        // If an object is deleted from the hierarchy, it will no longer be found here.
        // This dictionary will automatically rollback if the server disagrees with a deletion.
        
        var toRemove = ListPool<PlayerID>.Instantiate();
        
        foreach (var kvp in state.playersAlive)
        {
            if (!predictionManager.hierarchy.TryGetGameObject(kvp.Value, out var go) || go == null)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var id in toRemove)
        {
            state.playersAlive.Remove(id);
            Debug.Log($"[GameRunningState] Player {id} removed from playersAlive (object deleted)");
        }

        ListPool<PlayerID>.Destroy(toRemove);
    }

    public void OnPlayerSpawned(PlayerID player, PredictedObjectID obj)
    {
        currentState.playersAlive[player] = obj;
    }

    public struct MatchState : IPredictedData<MatchState>
    {
        public DisposableDictionary<PlayerID, PredictedObjectID> playersAlive;

        public void Dispose() 
        {
            playersAlive.Dispose();
        }

        public override string ToString()
        {
            if (playersAlive.isDisposed) return "Match not running";

            string log = $"players alive: {playersAlive.Count}";
            foreach (var player in playersAlive)
            {
                log += $"\n   {player.Key}";
            }

            return log;
        }
    }
}
