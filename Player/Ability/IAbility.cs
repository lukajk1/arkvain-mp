public interface IAbility
{
    /// <summary>
    /// 0 = on cooldown, 1 = ready
    /// </summary>
    float CooldownNormalized { get; }
}
