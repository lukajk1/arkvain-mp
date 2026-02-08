using UnityEngine;
using TMPro;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI _ammoText;
    [SerializeField] private string _ammoFormat = "{0} / {1}"; // "12 / 30" format

    [SerializeField] private TextMeshProUGUI _healthText;


    private IWeaponLogic _currentWeapon;
    private WeaponManager _weaponManager;
    private PlayerHealth _localPlayerHealth;

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

        // Unsubscribe from weapon switch event
        if (_weaponManager != null)
        {
            _weaponManager.OnWeaponSwitched -= OnWeaponSwitched;
        }

        // Unsubscribe from health changes
        if (_localPlayerHealth != null)
        {
            _localPlayerHealth.OnHealthChanged -= OnHealthChanged;
        }
    }

    public void RegisterPlayerHealth(PlayerHealth playerHealth)
    {
        _localPlayerHealth = playerHealth;

        // Subscribe to health changes
        _localPlayerHealth.OnHealthChanged += OnHealthChanged;

        // Initialize health text with current values
        OnHealthChanged(_localPlayerHealth.currentState.health, _localPlayerHealth._maxHealth);
    }

    public void UnregisterPlayerHealth(PlayerHealth playerHealth)
    {
        if (_localPlayerHealth == playerHealth)
        {
            _localPlayerHealth.OnHealthChanged -= OnHealthChanged;
            _localPlayerHealth = null;
        }
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


    private void OnHealthChanged(int currentHealth, int maxHealth)
    {
        if (_healthText != null)
        {
            _healthText.text = $"{currentHealth}";
        }
    }

    private void Update()
    {
        if (_currentWeapon != null && _ammoText != null)
        {
            _ammoText.text = string.Format(_ammoFormat, _currentWeapon.CurrentAmmo, _currentWeapon.MaxAmmo);
        }
    }
}
