using Heathen.SteamworksIntegration;
using PurrLobby;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

public class LoadoutManager : MonoBehaviour
{
    [SerializeField] private Canvas canvas;

    [SerializeField] private TMP_Dropdown heroDropdown;
    [SerializeField] private TMP_Dropdown weaponDropdown;
    [SerializeField] private Button closeButton;

    public static LoadoutSelection CurrentLoadout;
    public static LoadoutManager Instance { get; private set; }

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

        SetState(false);
    }

    void OnEnable()
    {
        if (closeButton != null) closeButton.onClick.AddListener(CloseClicked);
    }

    void OnDisable()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseClicked);
    }

    void Start()
    {
        SetState(false);

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

    public void SetState(bool value)
    {
        canvas.gameObject.SetActive(value); 
    }

    private void CloseClicked()
    {
        SetState(false);
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
