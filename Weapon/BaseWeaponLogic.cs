using PurrNet.Prediction;
using System;
using UnityEngine;

/// <summary>
/// Base class for all weapon logic components.
/// Provides common events and functionality that all weapons share.
/// Implements IReloadableWeaponLogic interface and sits between PredictedIdentity and specific weapon implementations.
/// </summary>
public abstract class BaseWeaponLogic<TInput, TState> : PredictedIdentity<TInput, TState>, IReloadableWeaponLogic
    where TInput : struct, IPredictedData<TInput>
    where TState : struct, IPredictedData<TState>
{
    // IWeaponLogic interface events - concrete so they can be invoked
    public event Action<HitInfo> OnHit;
    public event Action<Vector3> OnShoot;
    public event Action OnEquipped;
    public event Action OnHolstered;

    // IReloadableWeaponLogic interface events
    public event Action onReload;
    public event Action onReloadComplete;

    // IWeaponLogic interface properties - must be overridden by derived classes
    public abstract int CurrentAmmo { get; }
    public abstract int MaxAmmo { get; }

    // IWeaponLogic interface methods
    public void TriggerEquipped()
    {
        OnEquipped?.Invoke();
    }

    public void TriggerHolstered()
    {
        OnHolstered?.Invoke();
    }

    // Protected helpers to invoke events from derived classes
    protected void InvokeOnHit(HitInfo hitInfo)
    {
        OnHit?.Invoke(hitInfo);
    }

    protected void InvokeOnShoot(Vector3 fireDirection)
    {
        OnShoot?.Invoke(fireDirection);
    }

    protected void InvokeReloadEvent()
    {
        onReload?.Invoke();
    }

    protected void InvokeReloadCompleteEvent()
    {
        onReloadComplete?.Invoke();
    }
}
