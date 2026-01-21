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
    public void OnPlayerSpawned(PlayerID player, PredictedObjectID obj)
    {
        currentState.playersAlive[player] = obj;
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
