public interface IAbility
{
    /// <summary>
    /// 0 = on cooldown, 1 = ready
    /// </summary>
    float CooldownNormalized { get; }

    /// <summary>
    /// Remaining cooldown in seconds, 0 when ready
    /// </summary>
    float CooldownRemaining { get; }
}
