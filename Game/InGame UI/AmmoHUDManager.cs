using UnityEngine;
using TMPro;

public class AmmoHUDManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _ammoText;
    [SerializeField] private string _ammoFormat = "{0} / {1}"; // "12 / 30" format

    private IWeaponLogic _currentWeapon;
    private WeaponManager _weaponManager;

    private void OnEnable()
    {
        WeaponManager.OnLocalWeaponManagerReady += OnWeaponManagerReady;
    }

    private void OnDisable()
    {
        WeaponManager.OnLocalWeaponManagerReady -= OnWeaponManagerReady;

        // Unsubscribe from weapon switch event
        if (_weaponManager != null)
        {
            _weaponManager.OnWeaponSwitched -= OnWeaponSwitched;
        }
    }

    private void OnWeaponManagerReady(WeaponManager weaponManager)
    {
        _weaponManager = weaponManager;

        // Subscribe to weapon switch events
        _weaponManager.OnWeaponSwitched += OnWeaponSwitched;

        // Get the first weapon to display initially
        IWeaponLogic[] weapons = weaponManager.GetAllWeapons();
        if (weapons != null && weapons.Length > 0)
        {
            _currentWeapon = weapons[0];
        }
    }

    private void OnWeaponSwitched(IWeaponLogic newWeapon)
    {
        _currentWeapon = newWeapon;
    }

    private void Update()
    {
        if (_currentWeapon != null && _ammoText != null)
        {
            _ammoText.text = string.Format(_ammoFormat, _currentWeapon.CurrentAmmo, _currentWeapon.MaxAmmo);
        }
    }
}
