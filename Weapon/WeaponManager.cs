using PurrNet.Prediction;
using System;
using UnityEngine;

/// <summary>
/// Manages the active weapon for a player. 
/// In this version, weapon switching is disabled; a weapon is assigned at spawn and remains active.
/// Broadcasts weapon events globally for systems like hitmarkers.
/// </summary>
public class WeaponManager : PredictedIdentity<WeaponManager.WeaponState>
{
    [System.Serializable]
    public class WeaponPair
    {
        public IWeaponLogic logic;
        public WeaponVisualBase visual;

        public WeaponPair(IWeaponLogic logic, WeaponVisualBase visual)
        {
            this.logic = logic;
            this.visual = visual;
        }
    }

    [Header("Weapon References")]
    [SerializeField] private CrossbowLogic _crossbowLogic;
    [SerializeField] private CrossbowVisual _crossbowVisual;
    [SerializeField] private DeagleLogic _deagleLogic;
    [SerializeField] private DeagleVisual _deagleVisual;
    [SerializeField] private RailgunLogic _railgunLogic;
    [SerializeField] private RailgunVisual _railgunVisual;
    [SerializeField] private TrackingGunLogic _trackingGunLogic;
    [SerializeField] private TrackingGunVisual _trackingGunVisual;

    [Header("Other")]
    [SerializeField] private ViewmodelSway _viewmodelSway;

    private WeaponPair[] _weapons;
    private bool _isInitialized = false;

    /// <summary>
    /// Static event broadcast when a local player's WeaponManager is initialized.
    /// </summary>
    public static event Action<WeaponManager> OnLocalWeaponManagerReady;

    /// <summary>
    /// Event broadcast when the active weapon is set or changed.
    /// </summary>
    public event Action<IWeaponLogic> OnWeaponEquipped;

    protected override void LateAwake()
    {
        base.LateAwake();

        // Build fixed weapon array
        _weapons = new WeaponPair[]
        {
            new WeaponPair(_crossbowLogic, _crossbowVisual),      // Index 0
            new WeaponPair(_deagleLogic, _deagleVisual),          // Index 1
            new WeaponPair(_railgunLogic, _railgunVisual),        // Index 2
            new WeaponPair(_trackingGunLogic, _trackingGunVisual) // Index 3
        };

        // Disable everything initially
        foreach (var weapon in _weapons)
        {
            SetWeaponActive(weapon, false);
        }

        // Pull from networked loadout manager if available
        if (owner.HasValue)
        {
            var loadout = PlayerLoadoutState.GetLoadout(owner.Value);
            if (loadout != null)
            {
                var state = currentState;
                state.activeWeaponIndex = loadout.weaponIndex;
                ApplyActiveWeapon(state.activeWeaponIndex);
            }
        }

        // Initialize based on current state (important for late-joiners)
        ApplyActiveWeapon(currentState.activeWeaponIndex);
        _isInitialized = true;

        if (isOwner)
        {
            OnLocalWeaponManagerReady?.Invoke(this);
        }
    }

    /// <summary>
    /// Sets the active weapon index. Should typically be called by the server or 
    /// owner during the initialization/spawning phase.
    /// </summary>
    public void InitialEquip(int index)
    {
        index = Mathf.Clamp(index, 0, _weapons.Length - 1);
        
        // Update the predicted state
        var state = currentState;
        state.activeWeaponIndex = index;
    }

    protected override void Simulate(ref WeaponState state, float delta)
    {
        // Safety: If we are the owner but the state doesn't match our loadout yet (due to timing)
        if (isOwner && owner.HasValue)
        {
            var loadout = PlayerLoadoutState.GetLoadout(owner.Value);
            if (loadout != null && state.activeWeaponIndex != loadout.weaponIndex)
            {
                state.activeWeaponIndex = loadout.weaponIndex;
            }
        }

        // If the state says we should be using a different weapon than what is visually active
        ApplyActiveWeapon(state.activeWeaponIndex);
    }

    private int _lastAppliedIndex = -1;

    private void ApplyActiveWeapon(int index)
    {
        if (index < 0 || index >= _weapons.Length) return;
        if (_lastAppliedIndex == index) return;

        // Holster old
        if (_lastAppliedIndex != -1)
        {
            var oldWeapon = _weapons[_lastAppliedIndex];
            oldWeapon.logic?.TriggerHolstered();
            
            if (oldWeapon.logic != null)
                oldWeapon.logic.OnHit -= ForwardHitToUI;

            SetWeaponActive(oldWeapon, false);
        }

        // Equip new
        var newWeapon = _weapons[index];
        SetWeaponActive(newWeapon, true);
        newWeapon.logic?.TriggerEquipped();

        if (newWeapon.logic != null)
            newWeapon.logic.OnHit += ForwardHitToUI;

        // Update viewmodel sway
        if (_viewmodelSway != null && newWeapon.visual != null)
        {
            _viewmodelSway.SetViewmodel(newWeapon.visual.transform);
        }

        if (isOwner)
        {
            OnWeaponEquipped?.Invoke(newWeapon.logic);
        }

        _lastAppliedIndex = index;
    }

    private void ForwardHitToUI(HitInfo hitInfo)
    {
        // Only show hitmarkers for the local player
        if (isOwner && HitmarkerManager.Instance != null)
        {
            HitmarkerManager.Instance.ReportHit(hitInfo);
        }
    }

    private void SetWeaponActive(WeaponPair weapon, bool active)
    {
        if (weapon.logic != null)
        {
            weapon.logic.IsCurrent = active;
        }

        if (weapon.visual != null)
        {
            weapon.visual.enabled = active;
            if (active) weapon.visual.Show();
            else weapon.visual.Hide();
        }
    }

    public IWeaponLogic GetActiveWeapon()
    {
        if (_lastAppliedIndex == -1) return null;
        return _weapons[_lastAppliedIndex].logic;
    }

    public struct WeaponState : IPredictedData<WeaponState>
    {
        public int activeWeaponIndex;
        public void Dispose() { }
    }
}
