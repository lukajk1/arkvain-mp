using UnityEngine;
using PurrNet.Prediction;

public class SwitchWeapons : PredictedIdentity<SwitchWeapons.SwitchInput, SwitchWeapons.SwitchState>
{
    [SerializeField] private PlayerShooter shooter;
    [SerializeField] private PlayerGun2 gun2;

    protected override void LateAwake()
    {
        base.LateAwake();

        // Initialize with first weapon active
        SetActiveWeapon(true);
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
        input.switchWeapon |= InputManager.Instance.Player.QuickSwitchWeapon.WasPressedThisFrame();
        input.selectShooter |= InputManager.Instance.Player.PrimaryWeapon.WasPressedThisFrame();
        input.selectGun2 |= InputManager.Instance.Player.SecondaryWeapon.WasPressedThisFrame();
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
