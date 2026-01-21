using PurrNet;
using PurrNet.Pooling;
using PurrNet.Prediction;
using PurrNet.Prediction.StateMachine;
using UnityEngine;

public class RoundEndedState : PredictedStateNode<RoundEndedState.RoundEndState>
{
    public struct RoundEndState : IPredictedData<RoundEndState>
    {

        public void Dispose() { }
    }
}
