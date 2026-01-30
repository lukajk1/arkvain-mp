using PurrNet.Prediction;
using UnityEngine;

/// <summary>
/// Manages switching between different weapons with client-side prediction.
/// Enables/disables weapon Logic/Visual pairs and triggers activation events.
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

    [Header("Primary Weapon")]
    [SerializeField] private CrossbowLogic _crossbowLogic;
    [SerializeField] private CrossbowVisual _crossbowVisual;

    [Header("Secondary Weapon")]
    [SerializeField] private DeagleLogic _deagleLogic;
    [SerializeField] private DeagleVisual _deagleVisual;

    private WeaponPair[] _weapons;
    private const int PRIMARY_INDEX = 0;
    private const int SECONDARY_INDEX = 1;

    protected override void LateAwake()
    {
        base.LateAwake();

        Debug.Log("[WeaponManager] LateAwake started");

        // Build weapon array from references
        _weapons = new WeaponPair[]
        {
            new WeaponPair(_crossbowLogic, _crossbowVisual),
            new WeaponPair(_deagleLogic, _deagleVisual)
        };

        Debug.Log($"[WeaponManager] Weapon array built with {_weapons.Length} weapons");

        // Disable all weapons initially
        foreach (var weapon in _weapons)
        {
            SetWeaponActive(weapon, false);
        }

        Debug.Log("[WeaponManager] All weapons disabled");

        // Enable primary weapon
        SwitchToWeaponInternal(PRIMARY_INDEX);

        Debug.Log($"[WeaponManager] Primary weapon enabled (index {PRIMARY_INDEX})");
    }

    protected override void Simulate(SwitchInput input, ref SwitchState state, float delta)
    {
        bool weaponChanged = false;
        int targetIndex = state.currentWeaponIndex;

        // Direct selection: Primary weapon
        if (input.selectPrimary && state.currentWeaponIndex != PRIMARY_INDEX)
        {
            Debug.Log("[WeaponManager] Input: Select Primary");
            targetIndex = PRIMARY_INDEX;
            weaponChanged = true;
        }
        // Direct selection: Secondary weapon
        else if (input.selectSecondary && state.currentWeaponIndex != SECONDARY_INDEX)
        {
            Debug.Log("[WeaponManager] Input: Select Secondary");
            targetIndex = SECONDARY_INDEX;
            weaponChanged = true;
        }
        // Quick switch: Toggle between primary and secondary
        else if (input.quickSwitch)
        {
            Debug.Log("[WeaponManager] Input: Quick Switch");
            targetIndex = (state.currentWeaponIndex == PRIMARY_INDEX)
                ? SECONDARY_INDEX
                : PRIMARY_INDEX;
            weaponChanged = true;
        }

        if (weaponChanged)
        {
            state.currentWeaponIndex = targetIndex;
            SwitchToWeaponInternal(targetIndex);
        }
    }

    protected override void UpdateInput(ref SwitchInput input)
    {
        input.quickSwitch |= InputManager.Instance.Player.QuickSwitchWeapon.WasPressedThisFrame();
        input.selectPrimary |= InputManager.Instance.Player.PrimaryWeapon.WasPressedThisFrame();
        input.selectSecondary |= InputManager.Instance.Player.SecondaryWeapon.WasPressedThisFrame();
    }

    protected override void ModifyExtrapolatedInput(ref SwitchInput input)
    {
        // Don't extrapolate weapon switching
        input.quickSwitch = false;
        input.selectPrimary = false;
        input.selectSecondary = false;
    }

    protected override SwitchState GetInitialState()
    {
        return new SwitchState
        {
            currentWeaponIndex = PRIMARY_INDEX
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

        // Disable all weapons
        foreach (var weapon in _weapons)
        {
            SetWeaponActive(weapon, false);
        }

        // Enable target weapon
        var newWeapon = _weapons[index];
        SetWeaponActive(newWeapon, true);

        // Trigger switch event on the logic component
        TriggerSwitchToActive(newWeapon.logic);

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
    /// Calls SwitchToActive() on the weapon logic via the IWeaponLogic interface.
    /// </summary>
    private void TriggerSwitchToActive(IWeaponLogic logic)
    {
        logic?.SwitchToActive();
    }

    public struct SwitchInput : IPredictedData<SwitchInput>
    {
        public bool quickSwitch;      // Toggle between primary and secondary
        public bool selectPrimary;    // Direct select primary weapon
        public bool selectSecondary;  // Direct select secondary weapon

        public void Dispose()
        {
        }
    }

    public struct SwitchState : IPredictedData<SwitchState>
    {
        public int currentWeaponIndex;

        public void Dispose()
        {
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Validate primary weapon references
        if (_crossbowLogic == null)
        {
            Debug.LogWarning("[WeaponManager] Primary weapon (Crossbow) logic is missing!");
        }
        if (_crossbowVisual == null)
        {
            Debug.LogWarning("[WeaponManager] Primary weapon (Crossbow) visual is missing!");
        }

        // Validate secondary weapon references
        if (_deagleLogic == null)
        {
            Debug.LogWarning("[WeaponManager] Secondary weapon (Deagle) logic is missing!");
        }
        if (_deagleVisual == null)
        {
            Debug.LogWarning("[WeaponManager] Secondary weapon (Deagle) visual is missing!");
        }
    }
#endif
}
