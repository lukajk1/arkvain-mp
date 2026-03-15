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
    [SerializeField] private TMP_Dropdown weapon1Dropdown;
    [SerializeField] private TMP_Dropdown weapon2Dropdown;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text respawnNoticeText;

    public static LoadoutSelection CurrentLoadout = new LoadoutSelection
    {
        Hero = HeroType.Richter,
        Weapon1 = WeaponType.Crossbow,
        Weapon2 = WeaponType.Revolver
    };
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

        if (weapon1Dropdown != null)
        {
            weapon1Dropdown.onValueChanged.AddListener(OnWeapon1Changed);
            CurrentLoadout.Weapon1 = (WeaponType)weapon1Dropdown.value;
        }

        if (weapon2Dropdown != null)
        {
            weapon2Dropdown.onValueChanged.AddListener(OnWeapon2Changed);
            CurrentLoadout.Weapon2 = (WeaponType)weapon2Dropdown.value;
        }

        // Sync to PersistentClient immediately
        PersistentClient.currentLoadout = CurrentLoadout;
    }

    public void SetState(bool value)
    {
        canvas.gameObject.SetActive(value); 
        if (value) UpdateRespawnNotice();
    }

    private void CloseClicked()
    {
        SetState(false);
    }

    private void OnHeroChanged(int index)
    {
        CurrentLoadout.Hero = (HeroType)index;
        PersistentClient.currentLoadout = CurrentLoadout;
        UpdateRespawnNotice();
    }

    private void OnWeapon1Changed(int index)
    {
        CurrentLoadout.Weapon1 = (WeaponType)index;
        PersistentClient.currentLoadout = CurrentLoadout;
        UpdateRespawnNotice();
    }

    private void OnWeapon2Changed(int index)
    {
        CurrentLoadout.Weapon2 = (WeaponType)index;
        PersistentClient.currentLoadout = CurrentLoadout;
        UpdateRespawnNotice();
    }

    private void UpdateRespawnNotice()
    {
        if (respawnNoticeText == null) return;

        bool isDifferent = CurrentLoadout.Hero != _appliedLoadout.Hero ||
                          CurrentLoadout.Weapon1 != _appliedLoadout.Weapon1 ||
                          CurrentLoadout.Weapon2 != _appliedLoadout.Weapon2;

        // Only show if we've actually spawned once and it's different
        respawnNoticeText.gameObject.SetActive(_hasSpawnedOnce && isDifferent);
    }

    void Update()
    {

    }
}
