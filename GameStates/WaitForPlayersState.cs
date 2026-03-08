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
        bool isMapLoaded = MapLoader.Instance != null && MapLoader.Instance.CurrentMapData != null && !MapLoader.Instance.IsLoading;

        if (predictionManager.players.currentState.players.Count >= _expectedPlayers && isMapLoaded)
        {
            Debug.Log("[WaitForPlayersState] All players joined and map is loaded - proceeding to next state.");
            machine.Next();
        }
    }
    public struct WaitState : IPredictedData<WaitState>
    {
        public void Dispose() { }
    }
}
