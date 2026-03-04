using UnityEngine;
using TMPro;

public enum HeroType
{
    Richter
}

public enum WeaponType
{
    Crossbow,
    LightningGun,
    Revolver,
    Rifle
}

public struct LoadoutSelection
{
    public HeroType Hero;
    public WeaponType Weapon;
}

public class Loadout : MonoBehaviour
{
    [SerializeField] private Canvas canvas;

    [SerializeField] private TMP_Dropdown heroDropdown;
    [SerializeField] private TMP_Dropdown weaponDropdown;

    public static LoadoutSelection CurrentLoadout;
    public static Loadout Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (heroDropdown != null)
        {
            heroDropdown.onValueChanged.AddListener(OnHeroChanged);
            OnHeroChanged(heroDropdown.value);
        }

        if (weaponDropdown != null)
        {
            weaponDropdown.onValueChanged.AddListener(OnWeaponChanged);
            OnWeaponChanged(weaponDropdown.value);
        }
    }

    private void OnHeroChanged(int index)
    {
        CurrentLoadout.Hero = (HeroType)index;
    }

    private void OnWeaponChanged(int index)
    {
        CurrentLoadout.Weapon = (WeaponType)index;
    }

    void Update()
    {

    }
}
