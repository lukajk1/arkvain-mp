using PurrNet.Prediction;
using PurrNet.Prediction.StateMachine;
using UnityEngine;

public class WaitForPlayersState : PredictedStateNode<WaitForPlayersState.WaitState>
{
    [SerializeField] private int _expectedPlayers;

    protected override void StateSimulate(ref WaitState state, float delta)
    {
        if (predictionManager.players.currentState.players.Count >= _expectedPlayers)
        {
            machine.Next();
        }
    }
    public struct WaitState : IPredictedData<WaitState>
    {
        public void Dispose() { }
    }
}
