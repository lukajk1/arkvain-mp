using System;
using UnityEngine;

/// <summary>
/// Shared hit information structure for all weapon types.
/// </summary>
public struct HitInfo
{
    public Vector3 position;
    public bool hitPlayer;
    public bool isHeadshot;
}

/// <summary>
/// Interface for weapon logic components.
/// Allows WeaponManager to treat all weapon types uniformly without knowing their specific PredictedIdentity types.
/// Note: Implementations must be MonoBehaviour subclasses to access the .enabled property.
/// </summary>
public interface IWeaponLogic
{
    /// <summary>
    /// Called when this weapon is equipped (becomes active).
    /// Implementations should invoke their OnEquipped event.
    /// </summary>
    void TriggerEquipped();

    /// <summary>
    /// Called when this weapon is holstered (deactivated).
    /// Implementations should invoke their OnHolstered event.
    /// </summary>
    void TriggerHolstered();

    /// <summary>
    /// Event fired when this weapon successfully hits a target.
    /// Subscribe to this for hitmarkers, blood effects, etc.
    /// </summary>
    event Action<HitInfo> OnHit;

    /// <summary>
    /// Event fired when this weapon is fired/shot.
    /// Subscribe to this for muzzle flash, recoil animations, etc.
    /// </summary>
    event Action OnShoot;

    /// <summary>
    /// Event fired when this weapon is equipped (becomes active).
    /// Subscribe to this for equip sounds, animations, or starting effects.
    /// </summary>
    event Action OnEquipped;

    /// <summary>
    /// Event fired when this weapon is holstered (deactivated).
    /// Subscribe to this for cleanup, stopping effects, or holster sounds.
    /// </summary>
    event Action OnHolstered;

    /// <summary>
    /// Gets the current ammo count in the weapon's magazine/clip.
    /// </summary>
    int CurrentAmmo { get; }

    /// <summary>
    /// Gets the maximum ammo capacity of the weapon's magazine/clip.
    /// </summary>
    int MaxAmmo { get; }

    /// <summary>
    /// Gets whether this weapon belongs to the local player.
    /// Used to determine if UI feedback (hitmarkers, sounds) should be shown.
    /// </summary>
    bool isOwner { get; }
}
