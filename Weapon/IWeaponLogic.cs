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

    /// <summary>
    /// Direction the shot was fired from (normalized).
    /// </summary>
    public Vector3 fireDirection;

    /// <summary>
    /// Surface normal at hit point (only valid when hitPlayer is false).
    /// Comes from RaycastHit.normal.
    /// </summary>
    public Vector3 surfaceNormal;
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
    event Action<Vector3> OnShoot;

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

/// <summary>
/// Extended weapon logic interface that includes reload events.
/// Not all weapons may support reloading, so this is kept as a separate interface.
/// </summary>
public interface IReloadableWeaponLogic : IWeaponLogic
{
    /// <summary>
    /// Event fired when this weapon starts reloading.
    /// </summary>
    event Action onReload;

    /// <summary>
    /// Event fired when this weapon finishes reloading.
    /// </summary>
    event Action onReloadComplete;
}
