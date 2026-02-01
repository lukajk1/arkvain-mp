using PurrNet.Prediction;
using System;
using UnityEngine;

/// <summary>
/// Manages switching between different weapons with client-side prediction.
/// Enables/disables weapon Logic/Visual pairs and triggers activation events.
/// Broadcasts weapon events globally for systems like hitmarkers.
/// </summary>
public class WeaponManager : PredictedIdentity<WeaponManager.SwitchInput, WeaponManager.SwitchState>
{
    [System.Serializable]
    public class WeaponPair
    {
        public IWeaponLogic logic;
        public WeaponVisual visual;

        public WeaponPair(IWeaponLogic logic, WeaponVisual visual)
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

    [Header("Weapon Selection")]
    [Tooltip("Index of weapon to use as primary (0=Crossbow, 1=Deagle, 2=Railgun, 3=TrackingGun)")]
    [SerializeField] private int _primaryWeaponIndex = 0;
    [Tooltip("Index of weapon to use as secondary (0=Crossbow, 1=Deagle, 2=Railgun, 3=TrackingGun)")]
    [SerializeField] private int _secondaryWeaponIndex = 1;
    [Tooltip("Cooldown in seconds between weapon switches")]
    [SerializeField] private float _weaponSwitchCooldown = 0.3f;

    private WeaponPair[] _weapons;
    private int _primaryIndex;
    private int _secondaryIndex;

    /// <summary>
    /// Static event broadcast when a local player's WeaponManager is initialized.
    /// Passes the WeaponManager instance so systems can subscribe to weapon events.
    /// </summary>
    public static event Action<WeaponManager> OnLocalWeaponManagerReady;

    /// <summary>
    /// Event broadcast when the active weapon changes.
    /// Passes the newly active weapon's IWeaponLogic interface.
    /// </summary>
    public event Action<IWeaponLogic> OnWeaponSwitched;

    protected override void LateAwake()
    {
        base.LateAwake();

        Debug.Log("[WeaponManager] LateAwake started");

        // Build weapon array from references (all 4 weapons)
        _weapons = new WeaponPair[]
        {
            new WeaponPair(_crossbowLogic, _crossbowVisual),      // Index 0
            new WeaponPair(_deagleLogic, _deagleVisual),          // Index 1
            new WeaponPair(_railgunLogic, _railgunVisual),        // Index 2
            new WeaponPair(_trackingGunLogic, _trackingGunVisual) // Index 3
        };

        // Validate and clamp weapon indices
        _primaryIndex = Mathf.Clamp(_primaryWeaponIndex, 0, _weapons.Length - 1);
        _secondaryIndex = Mathf.Clamp(_secondaryWeaponIndex, 0, _weapons.Length - 1);

        if (_primaryIndex == _secondaryIndex)
        {
            Debug.LogWarning($"[WeaponManager] Primary and secondary weapon indices are the same ({_primaryIndex}). Setting secondary to different weapon.");
            _secondaryIndex = (_primaryIndex + 1) % _weapons.Length;
        }

        Debug.Log($"[WeaponManager] Weapon array built with {_weapons.Length} weapons. Primary: {_primaryIndex}, Secondary: {_secondaryIndex}");

        // Disable all weapons initially
        foreach (var weapon in _weapons)
        {
            SetWeaponActive(weapon, false);
        }

        Debug.Log("[WeaponManager] All weapons disabled");

        // Enable primary weapon
        SwitchToWeaponInternal(_primaryIndex);

        Debug.Log($"[WeaponManager] Primary weapon enabled (index {_primaryIndex})");

        // Broadcast initialization for owner only
        if (isOwner)
        {
            OnLocalWeaponManagerReady?.Invoke(this);
            Debug.Log("[WeaponManager] Broadcast OnLocalWeaponManagerReady");
        }
    }

    protected override void Simulate(SwitchInput input, ref SwitchState state, float delta)
    {
        // Decrement cooldown
        state.switchCooldown -= delta;

        bool weaponChanged = false;
        int targetIndex = state.currentWeaponIndex;

        // Only process weapon switch inputs if cooldown has expired
        if (state.switchCooldown <= 0)
        {
            // Direct selection: Primary weapon
            if (input.selectPrimary && state.currentWeaponIndex != _primaryIndex)
            {
                Debug.Log("[WeaponManager] Input: Select Primary");
                targetIndex = _primaryIndex;
                weaponChanged = true;
            }
            // Direct selection: Secondary weapon
            else if (input.selectSecondary && state.currentWeaponIndex != _secondaryIndex)
            {
                Debug.Log("[WeaponManager] Input: Select Secondary");
                targetIndex = _secondaryIndex;
                weaponChanged = true;
            }
            // Quick switch: Toggle between primary and secondary
            else if (input.quickSwitch)
            {
                Debug.Log("[WeaponManager] Input: Quick Switch");
                targetIndex = (state.currentWeaponIndex == _primaryIndex)
                    ? _secondaryIndex
                    : _primaryIndex;
                weaponChanged = true;
            }
            // Scroll up: Previous weapon with wrap-around
            else if (input.scrollUp)
            {
                Debug.Log("[WeaponManager] Input: Scroll Up");
                targetIndex = state.currentWeaponIndex == _primaryIndex ? _secondaryIndex : _primaryIndex;
                weaponChanged = true;
            }
            // Scroll down: Next weapon with wrap-around
            else if (input.scrollDown)
            {
                Debug.Log("[WeaponManager] Input: Scroll Down");
                targetIndex = state.currentWeaponIndex == _primaryIndex ? _secondaryIndex : _primaryIndex;
                weaponChanged = true;
            }
        }

        if (weaponChanged)
        {
            state.currentWeaponIndex = targetIndex;
            state.switchCooldown = _weaponSwitchCooldown;
            SwitchToWeaponInternal(targetIndex);
        }
    }

    protected override void UpdateInput(ref SwitchInput input)
    {
        input.quickSwitch |= InputManager.Instance.Player.QuickSwitchWeapon.WasPressedThisFrame();
        input.selectPrimary |= InputManager.Instance.Player.PrimaryWeapon.WasPressedThisFrame();
        input.selectSecondary |= InputManager.Instance.Player.SecondaryWeapon.WasPressedThisFrame();
        input.scrollUp |= InputManager.Instance.Player.ScrollUp.WasPressedThisFrame();
        input.scrollDown |= InputManager.Instance.Player.ScrollDown.WasPressedThisFrame();
    }

    protected override void ModifyExtrapolatedInput(ref SwitchInput input)
    {
        // Don't extrapolate weapon switching
        input.quickSwitch = false;
        input.selectPrimary = false;
        input.selectSecondary = false;
        input.scrollUp = false;
        input.scrollDown = false;
    }

    protected override SwitchState GetInitialState()
    {
        return new SwitchState
        {
            currentWeaponIndex = _primaryIndex
        };
    }

    /// <summary>
    /// Internal method to switch weapons. Called from Simulate() for predicted switching.
    /// </summary>
    private void SwitchToWeaponInternal(int index)
    {
        if (index < 0 || index >= _weapons.Length)
        {
            Debug.LogError($"[WeaponManager] Invalid weapon index: {index}");
            return;
        }

        Debug.Log($"[WeaponManager] Switching to weapon index {index}");

        // Trigger holster on all weapons before disabling them
        foreach (var weapon in _weapons)
        {
            weapon.logic?.TriggerHolstered();
            SetWeaponActive(weapon, false);
        }

        // Enable target weapon
        var newWeapon = _weapons[index];
        SetWeaponActive(newWeapon, true);

        // Trigger equip
        newWeapon.logic?.TriggerEquipped();

        // Broadcast weapon switch event (only for owner)
        if (isOwner)
        {
            OnWeaponSwitched?.Invoke(newWeapon.logic);
        }

        Debug.Log($"[WeaponManager] Weapon switch complete. Active weapon index: {index}");
    }

    /// <summary>
    /// Enables or disables both logic and visual components of a weapon.
    /// </summary>
    private void SetWeaponActive(WeaponPair weapon, bool active)
    {
        // Enable/disable logic component
        var logicBehaviour = weapon.logic as MonoBehaviour;
        if (logicBehaviour != null)
        {
            logicBehaviour.enabled = active;
            Debug.Log($"[WeaponManager] {logicBehaviour.GetType().Name}.enabled = {active}");
        }

        // Enable/disable visual component and show/hide viewmodel
        if (weapon.visual != null)
        {
            weapon.visual.enabled = active;

            if (active)
            {
                weapon.visual.Show();
                Debug.Log($"[WeaponManager] {weapon.visual.GetType().Name} shown");
            }
            else
            {
                weapon.visual.Hide();
                Debug.Log($"[WeaponManager] {weapon.visual.GetType().Name} hidden");
            }
        }
    }

    /// <summary>
    /// Gets all weapon logic components. Use this to subscribe to weapon events.
    /// </summary>
    public IWeaponLogic[] GetAllWeapons()
    {
        if (_weapons == null) return null;

        IWeaponLogic[] weapons = new IWeaponLogic[_weapons.Length];
        for (int i = 0; i < _weapons.Length; i++)
        {
            weapons[i] = _weapons[i].logic;
        }
        return weapons;
    }

    public struct SwitchInput : IPredictedData<SwitchInput>
    {
        public bool quickSwitch;      // Toggle between primary and secondary
        public bool selectPrimary;    // Direct select primary weapon
        public bool selectSecondary;  // Direct select secondary weapon
        public bool scrollUp;         // Scroll to previous weapon (wraps around)
        public bool scrollDown;       // Scroll to next weapon (wraps around)

        public void Dispose()
        {
        }
    }

    public struct SwitchState : IPredictedData<SwitchState>
    {
        public int currentWeaponIndex;
        public float switchCooldown;

        public void Dispose()
        {
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Validate Crossbow references
        if (_crossbowLogic == null)
            Debug.LogWarning("[WeaponManager] Crossbow logic is missing!");
        if (_crossbowVisual == null)
            Debug.LogWarning("[WeaponManager] Crossbow visual is missing!");

        // Validate Deagle references
        if (_deagleLogic == null)
            Debug.LogWarning("[WeaponManager] Deagle logic is missing!");
        if (_deagleVisual == null)
            Debug.LogWarning("[WeaponManager] Deagle visual is missing!");

        // Validate Railgun references
        if (_railgunLogic == null)
            Debug.LogWarning("[WeaponManager] Railgun logic is missing!");
        if (_railgunVisual == null)
            Debug.LogWarning("[WeaponManager] Railgun visual is missing!");

        // Validate TrackingGun references
        if (_trackingGunLogic == null)
            Debug.LogWarning("[WeaponManager] TrackingGun logic is missing!");
        if (_trackingGunVisual == null)
            Debug.LogWarning("[WeaponManager] TrackingGun visual is missing!");

        // Validate weapon selection indices
        if (_primaryWeaponIndex < 0 || _primaryWeaponIndex > 3)
            Debug.LogWarning("[WeaponManager] Primary weapon index out of range! Must be 0-3.");
        if (_secondaryWeaponIndex < 0 || _secondaryWeaponIndex > 3)
            Debug.LogWarning("[WeaponManager] Secondary weapon index out of range! Must be 0-3.");
        if (_primaryWeaponIndex == _secondaryWeaponIndex)
            Debug.LogWarning("[WeaponManager] Primary and secondary weapon indices are the same!");
    }
#endif
}
