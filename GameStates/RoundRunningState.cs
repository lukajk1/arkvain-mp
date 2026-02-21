using PurrNet;
using PurrNet.Pooling;
using PurrNet.Prediction;
using PurrNet.Prediction.StateMachine;
using UnityEngine;

public class RoundRunningState : PredictedStateNode<RoundRunningState.RoundState>
{
    protected override RoundState GetInitialState()
    {
        return new RoundState()
        {
            playersAlive = DisposableDictionary<PlayerID, PredictedObjectID>.Create()
        };
    }

    public override void Exit()
    {
        currentState.playersAlive.Clear();
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
        if (machine.currentStateNode is not RoundRunningState) return;

        currentState.playersAlive.Remove(player.Value.playerID);

        if (currentState.playersAlive.Count <= 1)
        {
            machine.Next();
        }
    }

    public struct RoundState : IPredictedData<RoundState>
    {
        public DisposableDictionary<PlayerID, PredictedObjectID> playersAlive;


        public void Dispose() 
        {
            playersAlive.Dispose();
        }

        public override string ToString()
        {
            if (playersAlive.isDisposed) return "Game not running";

            string log = $"players alive: {playersAlive.Count}";
            foreach (var player in playersAlive)
            {
                log += $"\n   {player.Key}";
            }

            return log;
        }
    }
}
