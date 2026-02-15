using UnityEngine;
using PurrNet.Prediction;

public abstract class BaseAbilityLogic<TInput, TState> : PredictedIdentity<TInput, TState>, IAbility
    where TInput : struct, IPredictedData<TInput>
    where TState : struct, IPredictedData<TState>
{
    public abstract float CooldownNormalized { get; }
    public abstract float CooldownRemaining { get; }
}
