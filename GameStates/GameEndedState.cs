using PurrNet.Prediction;
using PurrNet.Prediction.StateMachine;
using UnityEngine;

public class GameEndedState : PredictedStateNode<GameEndedState.EndState>
{
    [SerializeField] private float displayTime = 10f;
    private float _timer;

    public override void Enter()
    {
        Debug.Log("[GameEndedState] Match has ended!");
        _timer = 0;

        if (GameStateUI.Instance != null)
        {
            GameStateUI.Instance.UpdateStatus("MATCH OVER!");
            GameStateUI.Instance.ShowMatchResult(true); 
        }
    }

    protected override void StateSimulate(ref EndState state, float delta)
    {
        _timer += delta;

        if (_timer >= displayTime)
        {
            // Optional: Logic to return to lobby or restart
        }
    }

    public struct EndState : IPredictedData<EndState>
    {
        public void Dispose() { }
    }
}
