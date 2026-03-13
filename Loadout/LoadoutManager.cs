using Heathen.SteamworksIntegration;
using PurrLobby;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PurrNet;

public class LoadoutManager : MonoBehaviour
{
    [SerializeField] private Canvas canvas;

    [SerializeField] private TMP_Dropdown heroDropdown;
    [SerializeField] private TMP_Dropdown weaponDropdown;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text respawnNoticeText;

    public static LoadoutSelection CurrentLoadout;
    private static LoadoutSelection _appliedLoadout;
    private static bool _hasSpawnedOnce;

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
        if (respawnNoticeText != null) respawnNoticeText.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (closeButton != null) closeButton.onClick.AddListener(CloseClicked);
        GameEvents.OnPlayerSpawned += OnPlayerSpawned;
    }

    void OnDisable()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseClicked);
        GameEvents.OnPlayerSpawned -= OnPlayerSpawned;
    }

    private void OnPlayerSpawned(PlayerID player)
    {
        if (NetworkManager.main != null && player == NetworkManager.main.localPlayer)
        {
            _appliedLoadout = CurrentLoadout;
            _hasSpawnedOnce = true;
            UpdateRespawnNotice();
        }
    }

    void Start()
    {
        SetState(false);

        if (heroDropdown != null)
        {
            heroDropdown.onValueChanged.AddListener(OnHeroChanged);
            CurrentLoadout.Hero = (HeroType)heroDropdown.value;
        }

        if (weaponDropdown != null)
        {
            weaponDropdown.onValueChanged.AddListener(OnWeaponChanged);
            CurrentLoadout.Weapon = (WeaponType)weaponDropdown.value;
        }
        
        // Initial sync
        RequestUpdate();
    }

    public void SetState(bool value)
    {
        canvas.gameObject.SetActive(value); 
        if (value) UpdateRespawnNotice();
    }

    private void CloseClicked()
    {
        SetState(false);
        RequestUpdate();
    }
    private void OnHeroChanged(int index)
    {
        CurrentLoadout.Hero = (HeroType)index;
        RequestUpdate();
        UpdateRespawnNotice();
    }

    private void OnWeaponChanged(int index)
    {
        CurrentLoadout.Weapon = (WeaponType)index;
        RequestUpdate();
        UpdateRespawnNotice();
    }

    private void UpdateRespawnNotice()
    {
        if (respawnNoticeText == null) return;

        bool isDifferent = CurrentLoadout.Hero != _appliedLoadout.Hero || 
                          CurrentLoadout.Weapon != _appliedLoadout.Weapon;
        
        // Only show if we've actually spawned once and it's different
        respawnNoticeText.gameObject.SetActive(_hasSpawnedOnce && isDifferent);
    }

    public void SyncToNetwork()
    {
        if (PredictedLoadoutManager.Instance == null) return;

        HeroType hero = (HeroType)heroDropdown.value;
        int weaponIndex = weaponDropdown.value;

        PredictedLoadoutManager.Instance.SetLocalLoadout(hero, weaponIndex);
    }

    private void RequestUpdate()
    {
        SyncToNetwork();
    }

    void Update()
    {

    }
}
