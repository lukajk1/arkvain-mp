using PurrNet.Prediction;
using PurrNet.Prediction.StateMachine;
using UnityEngine;

public class WaitForPlayersState : PredictedStateNode<WaitForPlayersState.WaitState>
{
    [SerializeField] private int _expectedPlayers;

    public override void Enter()
    {
        Debug.Log($"entered waiting for players state");
        Debug.Log($"current player count: {predictionManager.players.currentState.players.Count}");
        Debug.Log($"need player count {_expectedPlayers}");
    }
    protected override void StateSimulate(ref WaitState state, float delta)
    {
        if (predictionManager.players.currentState.players.Count >= _expectedPlayers)
        {
            Debug.Log("moving to next state from waiting state");
            machine.Next();
        }
    }
    public struct WaitState : IPredictedData<WaitState>
    {
        public void Dispose() { }
    }
}
