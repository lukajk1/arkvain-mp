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

    private void OnEnable()
    {
        PlayerHealth.OnPlayerDeath += OnPlayerDied;    
    }
    private void OnDisable()
    {
        PlayerHealth.OnPlayerDeath -= OnPlayerDied;
    }

    public void OnPlayerSpawned(PlayerID player, PredictedObjectID obj)
    {
        currentState.playersAlive[player] = obj;
    }

    private void OnPlayerDied(PlayerInfo? player)
    {
        if (!player.HasValue) return;
        if (machine.currentStateNode is not GameRunningState) return;

        currentState.playersAlive.Remove(player.Value.playerID);
        
        // Note: We no longer check for playersAlive.Count <= 1 here.
        // Win conditions are now handled by BaseGameModeLogic.
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
