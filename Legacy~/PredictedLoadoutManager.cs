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
    [SerializeField] private TMP_Dropdown _weapon1Dropdown;
    [SerializeField] private TMP_Dropdown _weapon2Dropdown;
    [SerializeField] private Button _closeButton;
    [SerializeField] private TMP_Text _respawnNoticeText;

    public static PredictedLoadoutManager Instance { get; private set; }

    private HeroType _appliedHero;
    private int _appliedWeapon1Index = -1;
    private int _appliedWeapon2Index = -1;
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
        if (_weapon1Dropdown != null) _weapon1Dropdown.onValueChanged.AddListener(OnWeapon1Changed);
        if (_weapon2Dropdown != null) _weapon2Dropdown.onValueChanged.AddListener(OnWeapon2Changed);

        GameEvents.OnPlayerSpawned += OnPlayerSpawned;
    }

    private void OnDisable()
    {
        if (_closeButton != null) _closeButton.onClick.RemoveListener(CloseClicked);
        if (_heroDropdown != null) _heroDropdown.onValueChanged.RemoveListener(OnHeroChanged);
        if (_weapon1Dropdown != null) _weapon1Dropdown.onValueChanged.RemoveListener(OnWeapon1Changed);
        if (_weapon2Dropdown != null) _weapon2Dropdown.onValueChanged.RemoveListener(OnWeapon2Changed);

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
                _appliedWeapon1Index = localLoadout.weapon1Index;
                _appliedWeapon2Index = localLoadout.weapon2Index;
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

    private void OnWeapon1Changed(int index)
    {
        SyncToNetwork();
        UpdateRespawnNotice();
    }

    private void OnWeapon2Changed(int index)
    {
        SyncToNetwork();
        UpdateRespawnNotice();
    }

    private void SyncToNetwork()
    {
        if (_heroDropdown == null || _weapon1Dropdown == null || _weapon2Dropdown == null) return;

        HeroType hero = (HeroType)_heroDropdown.value;
        int weapon1Index = _weapon1Dropdown.value;
        int weapon2Index = _weapon2Dropdown.value;

        var localLoadout = GetLocalPlayerLoadout();
        if (localLoadout != null)
        {
            localLoadout.SetIntendedLoadout(hero, weapon1Index, weapon2Index);
        }
    }

    /// <summary>
    /// Updates the local player's hero and weapon selection.
    /// This change is predicted instantly on the client.
    /// </summary>
    public void SetLocalLoadout(HeroType hero, int weapon1Index, int weapon2Index)
    {
        var localLoadout = GetLocalPlayerLoadout();
        if (localLoadout != null)
        {
            localLoadout.SetIntendedLoadout(hero, weapon1Index, weapon2Index);
        }
        else
        {
            Debug.LogWarning("[PredictedLoadoutManager] Local PlayerLoadoutState not found. Change will not be networked!");
        }
    }

    private void UpdateRespawnNotice()
    {
        if (_respawnNoticeText == null || !_hasSpawnedOnce) return;

        bool isDifferent = (HeroType)_heroDropdown.value != _appliedHero ||
                          _weapon1Dropdown.value != _appliedWeapon1Index ||
                          _weapon2Dropdown.value != _appliedWeapon2Index;

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
