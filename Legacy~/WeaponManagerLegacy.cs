using PurrNet.Prediction;
using System;
using UnityEngine;

/// <summary>
/// Legacy version of WeaponManager.
/// </summary>
public class WeaponManagerLegacy : PredictedIdentity<WeaponManagerLegacy.SwitchInput, WeaponManagerLegacy.SwitchState>
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

    public bool canSwitch = true;

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

    [Header("Audio")]
    [Tooltip("Sound to play when switching weapons")]
    [SerializeField] private AudioClip _weaponSwitchSound;

    [Header("Viewmodel Sway")]
    [SerializeField] private ViewmodelSway _viewmodelSway;

    private WeaponPair[] _weapons;
    private int _primaryIndex;
    private int _secondaryIndex;
    private bool _isInitialized = false;


    /// <summary>
    /// Static event broadcast when a local player's WeaponManager is initialized.
    /// Passes the WeaponManager instance so systems can subscribe to weapon events.
    /// </summary>
    public static event Action<WeaponManagerLegacy> OnLocalWeaponManagerReady;

    /// <summary>
    /// Event broadcast when the active weapon changes.
    /// Passes the newly active weapon's IWeaponLogic interface.
    /// </summary>
    public event Action<IWeaponLogic> OnWeaponSwitched;

    protected override void LateAwake()
    {
        base.LateAwake();

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
            Debug.LogWarning($"[WeaponManagerLegacy] Primary and secondary weapon indices are the same ({_primaryIndex}). Setting secondary to different weapon.");
            _secondaryIndex = (_primaryIndex + 1) % _weapons.Length;
        }

        // Disable all weapons initially
        foreach (var weapon in _weapons)
        {
            SetWeaponActive(weapon, false);
        }

        // Enable primary weapon (without sound)
        SwitchToWeaponInternal(_primaryIndex);
        _isInitialized = true; // Mark as initialized after first weapon is equipped

        // Broadcast initialization for owner only
        if (isOwner)
        {
            OnLocalWeaponManagerReady?.Invoke(this);
        }
    }

    protected override void Simulate(SwitchInput input, ref SwitchState state, float delta)
    {
        // Decrement cooldown
        state.switchCooldown -= delta;

        bool weaponChanged = false;
        int targetIndex = state.currentWeaponIndex;

        // Only process weapon switch inputs if cooldown has expired
        if (state.switchCooldown <= 0 && canSwitch)
        {
            // Direct selection: Primary weapon
            if (input.selectPrimary && state.currentWeaponIndex != _primaryIndex)
            {
                targetIndex = _primaryIndex;
                weaponChanged = true;
            }
            // Direct selection: Secondary weapon
            else if (input.selectSecondary && state.currentWeaponIndex != _secondaryIndex)
            {
                targetIndex = _secondaryIndex;
                weaponChanged = true;
            }
            // Quick switch: Toggle between primary and secondary
            else if (input.quickSwitch)
            {
                targetIndex = (state.currentWeaponIndex == _primaryIndex)
                    ? _secondaryIndex
                    : _primaryIndex;
                weaponChanged = true;
            }
            // Scroll up: Previous weapon with wrap-around
            else if (input.scrollUp)
            {
                targetIndex = state.currentWeaponIndex == _primaryIndex ? _secondaryIndex : _primaryIndex;
                weaponChanged = true;
            }
            // Scroll down: Next weapon with wrap-around
            else if (input.scrollDown)
            {
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
        input.quickSwitch |= PersistentClient.Instance.inputManager.Player.QuickSwitchWeapon.WasPressedThisFrame();
        input.selectPrimary |= PersistentClient.Instance.inputManager.Player.PrimaryWeapon.WasPressedThisFrame();
        input.selectSecondary |= PersistentClient.Instance.inputManager.Player.SecondaryWeapon.WasPressedThisFrame();
        input.scrollUp |= PersistentClient.Instance.inputManager.Player.ScrollUp.WasPressedThisFrame();
        input.scrollDown |= PersistentClient.Instance.inputManager.Player.ScrollDown.WasPressedThisFrame();
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

    private void SwitchToWeaponInternal(int index)
    {
        if (index < 0 || index >= _weapons.Length)
        {
            Debug.LogError($"[WeaponManagerLegacy] Invalid weapon index: {index}");
            return;
        }

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

        // Update viewmodel sway to use the new weapon's viewmodel
        if (_viewmodelSway != null && newWeapon.visual != null)
        {
            Transform viewmodelTransform = newWeapon.visual.transform;
            _viewmodelSway.SetViewmodel(viewmodelTransform);
        }

        // Broadcast weapon switch event (only for owner)
        if (isOwner)
        {
            OnWeaponSwitched?.Invoke(newWeapon.logic);
        }
    }

    private void SetWeaponActive(WeaponPair weapon, bool active)
    {
        // Set IsCurrent flag on logic component (do NOT disable the logic behaviour)
        if (weapon.logic != null)
        {
            weapon.logic.IsCurrent = active;
        }

        // Enable/disable visual component and show/hide viewmodel
        if (weapon.visual != null)
        {
            weapon.visual.enabled = active;

            if (active)
            {
                weapon.visual.Show();
            }
            else
            {
                weapon.visual.Hide();
            }
        }
    }

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
        public bool quickSwitch;
        public bool selectPrimary;
        public bool selectSecondary;
        public bool scrollUp;
        public bool scrollDown;

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
        if (_crossbowLogic == null) Debug.LogWarning("[WeaponManagerLegacy] Crossbow logic is missing!");
        if (_crossbowVisual == null) Debug.LogWarning("[WeaponManagerLegacy] Crossbow visual is missing!");
        if (_deagleLogic == null) Debug.LogWarning("[WeaponManagerLegacy] Deagle logic is missing!");
        if (_deagleVisual == null) Debug.LogWarning("[WeaponManagerLegacy] Deagle visual is missing!");
        if (_railgunLogic == null) Debug.LogWarning("[WeaponManagerLegacy] Railgun logic is missing!");
        if (_railgunVisual == null) Debug.LogWarning("[WeaponManagerLegacy] Railgun visual is missing!");
        if (_trackingGunLogic == null) Debug.LogWarning("[WeaponManagerLegacy] TrackingGun logic is missing!");
        if (_trackingGunVisual == null) Debug.LogWarning("[WeaponManagerLegacy] TrackingGun visual is missing!");
    }
#endif
}
