/// <summary>
/// Interface for weapon logic components.
/// Allows WeaponManager to treat all weapon types uniformly without knowing their specific PredictedIdentity types.
/// Note: Implementations must be MonoBehaviour subclasses to access the .enabled property.
/// </summary>
public interface IWeaponLogic
{
    /// <summary>
    /// Called when this weapon becomes the active weapon.
    /// Triggers visual effects like equip animations.
    /// </summary>
    void SwitchToActive();
}
