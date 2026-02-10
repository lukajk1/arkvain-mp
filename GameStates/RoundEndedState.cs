using PurrNet;
using PurrNet.Pooling;
using PurrNet.Prediction;
using PurrNet.Prediction.StateMachine;
using UnityEngine;

public class RoundEndedState : PredictedStateNode<RoundEndedState.RoundEndState>
{
    [SerializeField] private float _timeToRestart = 3f;

    public override void ViewEnter(bool isVerified)
    {
        if (isVerified) Debug.Log("successfully entered round end state");
    }

    public override void Enter()
    {
        currentState.roundRestartTimer = _timeToRestart;
    }

    protected override void StateSimulate(ref RoundEndState state, float delta)
    {
        if (state.roundRestartTimer > 0)
        {
            state.roundRestartTimer -= delta;
            if (state.roundRestartTimer <= 0)
            {
                PlayerHealth.RespawnAllPlayers?.Invoke();
                machine.Next();
            }
        }
    }

    public struct RoundEndState : IPredictedData<RoundEndState>
    {
        public float roundRestartTimer;
        public void Dispose() { }
    }
}
