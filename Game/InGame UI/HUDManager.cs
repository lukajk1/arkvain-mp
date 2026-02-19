using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI _ammoText;
    [SerializeField] private string _ammoFormat = "{0} / {1}"; // "12 / 30" format

    [SerializeField] private TextMeshProUGUI _healthText;
    [SerializeField] private TextMeshProUGUI _velocityText;

    [Header("Ability")]
    [SerializeField] private TextMeshProUGUI _abilityCooldownText;
    [SerializeField] private TextMeshProUGUI _abilityBindingName;
    [SerializeField] private GameObject _abilityCooldownParentToHide;
    [SerializeField] private Image _abilityCooldown;
    [SerializeField] private Color _abilityCooldownColor = Color.gray;
    [SerializeField] private AudioClip _abilityReadySound;

    [Header("FPS Counter")]
    [SerializeField] private TextMeshProUGUI _fpsText;
    [SerializeField] private int _fpsSampleCount = 20;

    private float[] _frameTimes;
    private int _frameIndex;
    private bool _fpsBufferFilled;

    [Header("Center Display Broadcasts")]
    [SerializeField] private TextMeshProUGUI _broadcastText;
    [SerializeField] private float _broadcastRiseDistance = 80f;
    [SerializeField] private float _broadcastDuration = 1.2f;

    private int _broadcastTweenId = -1;
    private int _broadcastAlphaTweenId = -1;
    private Vector2 _broadcastStartAnchoredPos;

    private IWeaponLogic _currentWeapon;
    private WeaponManager _weaponManager;
    private bool _abilityOnCooldown;

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

    private void Start()
    {
        if (_broadcastText != null)
        {
            _broadcastStartAnchoredPos = _broadcastText.rectTransform.anchoredPosition;
            _broadcastText.alpha = 0f;
        }

        _frameTimes = new float[_fpsSampleCount];
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

    public void SetAbilityCooldown(float normalizedCooldown, float remainingSeconds)
    {
        bool onCooldown = remainingSeconds > 0f;

        if (_abilityCooldown != null)
        {
            _abilityCooldown.fillAmount = normalizedCooldown;
            _abilityCooldown.color = onCooldown ? _abilityCooldownColor : Color.white;
        }

        if (_abilityCooldownText != null)
            _abilityCooldownText.text = onCooldown ? remainingSeconds.ToString("F1") : string.Empty;

        if (_abilityOnCooldown && !onCooldown && _abilityReadySound != null)
            SoundManager.PlayNonDiegetic(_abilityReadySound);

        _abilityOnCooldown = onCooldown;
    }

    public void HideAbilityUI()
    {
        if (_abilityCooldownParentToHide != null)
            _abilityCooldownParentToHide.SetActive(false);
    }

    public void SetAbilityBindingName(string name)
    {
        if (_abilityBindingName != null)
            _abilityBindingName.text = name;    
    }

    public void BroadcastEvent(string message)
    {
        if (_broadcastText == null) return;

        if (_broadcastTweenId != -1) LeanTween.cancel(_broadcastTweenId);
        if (_broadcastAlphaTweenId != -1) LeanTween.cancel(_broadcastAlphaTweenId);

        _broadcastText.text = message;
        _broadcastText.alpha = 1f;
        _broadcastText.rectTransform.anchoredPosition = _broadcastStartAnchoredPos;

        Vector2 targetPos = _broadcastStartAnchoredPos + Vector2.up * _broadcastRiseDistance;
        _broadcastTweenId = LeanTween.move(_broadcastText.rectTransform, targetPos, _broadcastDuration)
            .setEase(LeanTweenType.easeOutCubic).id;
        _broadcastAlphaTweenId = LeanTween.value(gameObject, 1f, 0f, _broadcastDuration)
            .setEase(LeanTweenType.easeInCubic)
            .setOnUpdate((float val) => _broadcastText.alpha = val)
            .setOnComplete(() => _broadcastText.alpha = 0f).id;
    }

    private void Update()
    {
        if (_currentWeapon != null && _ammoText != null)
        {
            _ammoText.text = string.Format(_ammoFormat, _currentWeapon.CurrentAmmo, _currentWeapon.MaxAmmo);
        }

        UpdateFPS();
    }

    private void UpdateFPS()
    {
        if (_fpsText == null) return;

        _frameTimes[_frameIndex] = Time.unscaledDeltaTime;
        _frameIndex = (_frameIndex + 1) % _fpsSampleCount;
        if (_frameIndex == 0) _fpsBufferFilled = true;

        int sampleCount = _fpsBufferFilled ? _fpsSampleCount : _frameIndex;
        if (sampleCount == 0) return;

        float sum = 0f;
        for (int i = 0; i < sampleCount; i++)
            sum += _frameTimes[i];

        float fps = sampleCount / sum;
        _fpsText.text = $"FPS: {fps.ToString("F1")}";
    }
}
