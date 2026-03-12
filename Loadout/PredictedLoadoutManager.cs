using PurrNet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Replaces the old LoadoutManager. 
/// Handles the UI for hero/weapon selection and syncs it to the networked PlayerLoadoutState.
/// </summary>
public class PredictedLoadoutManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private TMP_Dropdown _heroDropdown;
    [SerializeField] private TMP_Dropdown _weaponDropdown;
    [SerializeField] private Button _closeButton;
    [SerializeField] private TMP_Text _respawnNoticeText;

    public static PredictedLoadoutManager Instance { get; private set; }

    private HeroType _appliedHero;
    private int _appliedWeaponIndex = -1;
    private bool _hasSpawnedOnce = false;

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
        if (_respawnNoticeText != null) _respawnNoticeText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (_closeButton != null) _closeButton.onClick.AddListener(CloseClicked);
        if (_heroDropdown != null) _heroDropdown.onValueChanged.AddListener(OnHeroChanged);
        if (_weaponDropdown != null) _weaponDropdown.onValueChanged.AddListener(OnWeaponChanged);
        
        GameEvents.OnPlayerSpawned += OnPlayerSpawned;
    }

    private void OnDisable()
    {
        if (_closeButton != null) _closeButton.onClick.RemoveListener(CloseClicked);
        if (_heroDropdown != null) _heroDropdown.onValueChanged.RemoveListener(OnHeroChanged);
        if (_weaponDropdown != null) _weaponDropdown.onValueChanged.RemoveListener(OnWeaponChanged);
        
        GameEvents.OnPlayerSpawned -= OnPlayerSpawned;
    }

    private void OnPlayerSpawned(PlayerID player)
    {
        if (NetworkManager.main != null && player == NetworkManager.main.localPlayer)
        {
            var localLoadout = GetLocalPlayerLoadout();
            if (localLoadout != null)
            {
                _appliedHero = localLoadout.hero;
                _appliedWeaponIndex = localLoadout.weaponIndex;
            }
            
            _hasSpawnedOnce = true;
            UpdateRespawnNotice();
        }
    }

    public void SetState(bool value)
    {
        if (_canvas != null) _canvas.gameObject.SetActive(value); 
        if (value) UpdateRespawnNotice();
    }

    private void CloseClicked()
    {
        SetState(false);
    }

    private void OnHeroChanged(int index)
    {
        SyncToNetwork();
        UpdateRespawnNotice();
    }

    private void OnWeaponChanged(int index)
    {
        SyncToNetwork();
        UpdateRespawnNotice();
    }

    private void SyncToNetwork()
    {
        if (_heroDropdown == null || _weaponDropdown == null) return;

        HeroType hero = (HeroType)_heroDropdown.value;
        int weaponIndex = _weaponDropdown.value;

        var localLoadout = GetLocalPlayerLoadout();
        if (localLoadout != null)
        {
            localLoadout.SetIntendedLoadout(hero, weaponIndex);
        }
    }

    private void UpdateRespawnNotice()
    {
        if (_respawnNoticeText == null || !_hasSpawnedOnce) return;

        bool isDifferent = (HeroType)_heroDropdown.value != _appliedHero || 
                          _weaponDropdown.value != _appliedWeaponIndex;
        
        _respawnNoticeText.gameObject.SetActive(isDifferent);
    }

    /// <summary>
    /// Gets the loadout selection for the local player.
    /// </summary>
    public PlayerLoadoutState GetLocalPlayerLoadout()
    {
        if (NetworkManager.main == null || NetworkManager.main.localPlayer == null)
            return null;

        return PlayerLoadoutState.GetLoadout(NetworkManager.main.localPlayer);
    }
}
