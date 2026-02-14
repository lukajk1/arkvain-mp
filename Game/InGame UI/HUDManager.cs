using UnityEngine;
using TMPro;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI _ammoText;
    [SerializeField] private string _ammoFormat = "{0} / {1}"; // "12 / 30" format

    [SerializeField] private TextMeshProUGUI _healthText;
    [SerializeField] private TextMeshProUGUI _velocityText;

    private IWeaponLogic _currentWeapon;
    private WeaponManager _weaponManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Multiple HUDManager instances detected! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        WeaponManager.OnLocalWeaponManagerReady += OnWeaponManagerReady;
    }

    private void OnDisable()
    {
        WeaponManager.OnLocalWeaponManagerReady -= OnWeaponManagerReady;

        if (_weaponManager != null)
        {
            _weaponManager.OnWeaponSwitched -= OnWeaponSwitched;
        }
    }

    public void SetHealthReadout(int currentHealth, int maxHealth)
    {
        if (_healthText != null)
            _healthText.text = $"{currentHealth}";
    }

    public void SetVelocityReadout(Vector3 velocity)
    {
        if (_velocityText != null)
            _velocityText.text = $"{velocity.magnitude:F2}";
    }

    private void OnWeaponManagerReady(WeaponManager weaponManager)
    {
        _weaponManager = weaponManager;

        // Subscribe to weapon switch events
        _weaponManager.OnWeaponSwitched += OnWeaponSwitched;

        // Get currently active weapon (the primary weapon has already been activated)
        // Since weapon switching happens before this event fires, we need to get the active weapon
        IWeaponLogic[] weapons = weaponManager.GetAllWeapons();
        if (weapons != null && weapons.Length > 0)
        {
            // Find the enabled weapon
            foreach (var weapon in weapons)
            {
                var weaponMono = weapon as MonoBehaviour;
                if (weaponMono != null && weaponMono.enabled)
                {
                    _currentWeapon = weapon;
                    break;
                }
            }
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
