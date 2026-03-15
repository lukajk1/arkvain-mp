using PurrNet.Prediction;
using System;
using UnityEngine;

/// <summary>
/// Manages weapons for a player with primary and secondary slots.
/// Reads loadout from PersistentClient at spawn and allows switching between the two equipped weapons.
/// Broadcasts weapon events globally for systems like hitmarkers.
/// </summary>
public class WeaponManager : PredictedIdentity<WeaponManager.WeaponInput, WeaponManager.WeaponState>
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
    [SerializeField] private RevolverLogic _deagleLogic;
    [SerializeField] private RevolverVisual _deagleVisual;
    [SerializeField] private RailgunLogic _railgunLogic;
    [SerializeField] private RailgunVisual _railgunVisual;
    [SerializeField] private TrackingGunLogic _trackingGunLogic;
    [SerializeField] private TrackingGunVisual _trackingGunVisual;

    [Header("Other")]
    [SerializeField] private ViewmodelSway _viewmodelSway;
    [SerializeField] private float _weaponSwitchCooldown = 0.6f;

    private WeaponPair[] _weapons;
    private bool _isInitialized = false;

    /// <summary>
    /// Event broadcast when the active weapon is set or changed.
    /// </summary>
    public event Action<IWeaponLogic> OnWeaponEquipped;

    protected override WeaponState GetInitialState()
    {
        return new WeaponState
        {
            primaryWeaponIndex = Mathf.Clamp((int)PersistentClient.currentLoadout.Weapon1, 0, 3),
            secondaryWeaponIndex = Mathf.Clamp((int)PersistentClient.currentLoadout.Weapon2, 0, 3),
            isPrimaryActive = true,
            switchCooldown = 0f
        };
    }

    protected override void LateAwake()
    {
        base.LateAwake();

        // Build fixed weapon array
        _weapons = new WeaponPair[]
        {
            new WeaponPair(_crossbowLogic, _crossbowVisual),      // Index 0
            new WeaponPair(_trackingGunLogic, _trackingGunVisual), // Index 3
            new WeaponPair(_deagleLogic, _deagleVisual),          // Index 1
            new WeaponPair(_railgunLogic, _railgunVisual),        // Index 2
        };

        // Disable everything initially
        foreach (var weapon in _weapons)
        {
            SetWeaponActive(weapon, false);
        }

        // Initialize based on current state (important for late-joiners)
        ApplyActiveWeapon(GetActiveWeaponIndex());
        _isInitialized = true;
    }

    /// <summary>
    /// Sets the primary and secondary weapon indices.
    /// </summary>
    public void InitialEquip(int primaryIndex, int secondaryIndex)
    {
        primaryIndex = Mathf.Clamp(primaryIndex, 0, _weapons.Length - 1);
        secondaryIndex = Mathf.Clamp(secondaryIndex, 0, _weapons.Length - 1);

        // Update the predicted state
        var state = currentState;
        state.primaryWeaponIndex = primaryIndex;
        state.secondaryWeaponIndex = secondaryIndex;
        state.isPrimaryActive = true;
    }


    protected override void UpdateInput(ref WeaponInput input)
    {
        var playerInput = PersistentClient.Instance.inputManager.Player;

        // Accumulate button presses (like jump in PlayerMovement)
        input.switchToPrimary |= playerInput.PrimaryWeapon.WasPressedThisFrame();
        input.switchToSecondary |= playerInput.SecondaryWeapon.WasPressedThisFrame();
        input.quickSwitch |= playerInput.QuickSwitchWeapon.WasPressedThisFrame();

        // Mouse wheel scrolling (using new Input System)
        input.switchToPrimary |= playerInput.ScrollUp.WasPressedThisFrame();
        input.switchToSecondary |= playerInput.ScrollDown.WasPressedThisFrame();
    }

    protected override void ModifyExtrapolatedInput(ref WeaponInput input)
    {
        // Reset accumulated presses to prevent weapon switches from being extrapolated
        input.switchToPrimary = false;
        input.switchToSecondary = false;
        input.quickSwitch = false;
    }

    protected override void Simulate(WeaponInput input, ref WeaponState state, float delta)
    {
        // Count down switch cooldown
        if (state.switchCooldown > 0)
        {
            state.switchCooldown -= delta;
        }

        // Only allow weapon switch if cooldown has expired
        if (state.switchCooldown <= 0f)
        {
            bool switched = false;

            // Apply accumulated weapon switches
            if (input.switchToPrimary && !state.isPrimaryActive)
            {
                state.isPrimaryActive = true;
                switched = true;
            }
            else if (input.switchToSecondary && state.isPrimaryActive)
            {
                state.isPrimaryActive = false;
                switched = true;
            }
            else if (input.quickSwitch)
            {
                state.isPrimaryActive = !state.isPrimaryActive;
                switched = true;
            }

            // Reset cooldown if a switch occurred
            if (switched)
            {
                state.switchCooldown = _weaponSwitchCooldown;
            }
        }

        // If the state says we should be using a different weapon than what is visually active
        int activeIndex = state.isPrimaryActive ? state.primaryWeaponIndex : state.secondaryWeaponIndex;
        ApplyActiveWeapon(activeIndex);
    }

    private int GetActiveWeaponIndex()
    {
        var state = currentState;
        return state.isPrimaryActive ? state.primaryWeaponIndex : state.secondaryWeaponIndex;
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

    public struct WeaponInput : IPredictedData<WeaponInput>
    {
        public bool switchToPrimary;
        public bool switchToSecondary;
        public bool quickSwitch;
        public void Dispose() { }
    }

    public struct WeaponState : IPredictedData<WeaponState>
    {
        public int primaryWeaponIndex;
        public int secondaryWeaponIndex;
        public bool isPrimaryActive; // true = primary, false = secondary
        public float switchCooldown;
        public void Dispose() { }
    }
}
