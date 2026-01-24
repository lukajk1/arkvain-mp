using UnityEngine;
using UnityEngine.InputSystem;
using PurrNet.Prediction;

public class SwitchWeapons : PredictedIdentity<SwitchWeapons.SwitchInput, SwitchWeapons.SwitchState>
{
    [SerializeField] private PlayerShooter shooter;
    [SerializeField] private PlayerGun2 gun2;
    [SerializeField] private InputActionReference _switchAction;
    [SerializeField] private InputActionReference _selectShooterAction;
    [SerializeField] private InputActionReference _selectGun2Action;

    protected override void LateAwake()
    {
        base.LateAwake();

        if (isOwner)
        {
            EnableAction(_switchAction);
            EnableAction(_selectShooterAction);
            EnableAction(_selectGun2Action);
        }

        // Initialize with first weapon active
        SetActiveWeapon(true);
    }

    private void EnableAction(InputActionReference actionRef)
    {
        if (actionRef != null)
        {
            actionRef.action.Enable();
        }
    }

    private void SetActiveWeapon(bool useShooter)
    {
        if (shooter != null) shooter.enabled = useShooter;
        if (gun2 != null) gun2.enabled = !useShooter;
    }

    protected override void Simulate(SwitchInput input, ref SwitchState state, float delta)
    {
        bool weaponChanged = false;

        if (input.selectShooter && !state.isFirstWeapon)
        {
            state.isFirstWeapon = true;
            weaponChanged = true;
        }
        else if (input.selectGun2 && state.isFirstWeapon)
        {
            state.isFirstWeapon = false;
            weaponChanged = true;
        }
        else if (input.switchWeapon)
        {
            state.isFirstWeapon = !state.isFirstWeapon;
            weaponChanged = true;
        }

        if (weaponChanged)
        {
            SetActiveWeapon(state.isFirstWeapon);
        }
    }

    protected override void UpdateInput(ref SwitchInput input)
    {
        if (_switchAction != null)
        {
            input.switchWeapon |= _switchAction.action.WasPressedThisFrame();
        }

        if (_selectShooterAction != null)
        {
            input.selectShooter |= _selectShooterAction.action.WasPressedThisFrame();
        }

        if (_selectGun2Action != null)
        {
            input.selectGun2 |= _selectGun2Action.action.WasPressedThisFrame();
        }
    }

    protected override void ModifyExtrapolatedInput(ref SwitchInput input)
    {
        input.switchWeapon = false;
        input.selectShooter = false;
        input.selectGun2 = false;
    }

    public struct SwitchInput : IPredictedData<SwitchInput>
    {
        public bool switchWeapon;
        public bool selectShooter;
        public bool selectGun2;

        public void Dispose()
        {
        }
    }

    public struct SwitchState : IPredictedData<SwitchState>
    {
        public bool isFirstWeapon;

        public void Dispose()
        {
        }
    }
}
