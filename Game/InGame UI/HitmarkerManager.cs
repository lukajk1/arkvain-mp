using UnityEngine;
using UnityEngine.UI;

public class HitmarkerManager : MonoBehaviour
{
    [SerializeField] private Image _hitmarker;
    [SerializeField] private Image _killIcon;
    [SerializeField] private float _animationDuration = 0.3f;
    [SerializeField] private float _scaleMultiplier = 1.3f; // Scale up by 30%
    [SerializeField] private float _headshotScaleMultiplier = 1.2f; // additional scale on top

    [Header("Kill Icon")]
    [SerializeField] private float _killIconDuration = 0.3f;
    [SerializeField] private float _killIconInitialRelativeScale = 1.3f;
    [SerializeField] private float _killIconDelay = 0.3f;

    [SerializeField] private Color _bodyHitColor = Color.white;
    [SerializeField] private Color _headshotColor = Color.red;

    [SerializeField] private AudioClip _bodyShotClip;
    [SerializeField] private AudioClip _headShotClip;
    [SerializeField] private AudioClip _killSfx;

    private Vector3 _initialScale;
    private Vector3 _killIconInitialScale;
    private int _currentTweenId = -1;
    private int _killIconTweenId = -1;
    private WeaponManager _localWeaponManager;

    private void Awake()
    {
        if (_hitmarker != null)
        {
            _initialScale = _hitmarker.transform.localScale;
            // Start with hitmarker invisible
            SetHitmarkerAlpha(0f);
        }

        if (_killIcon != null)
        {
            _killIconInitialScale = _killIcon.transform.localScale;
            _killIcon.color = _headshotColor;
            SetKillIconAlpha(0f);
        }
    }

    private void OnEnable()
    {
        WeaponManager.OnLocalWeaponManagerReady += OnWeaponManagerReady;
        PlayerHealth.OnPlayerKilled += OnPlayerKilled;
    }

    private void OnDisable()
    {
        WeaponManager.OnLocalWeaponManagerReady -= OnWeaponManagerReady;
        PlayerHealth.OnPlayerKilled -= OnPlayerKilled;

        // Cancel any running tweens
        if (_currentTweenId != -1)
        {
            LeanTween.cancel(_currentTweenId);
        }

        if (_killIconTweenId != -1)
        {
            LeanTween.cancel(_killIconTweenId);
        }
    }

    private void OnWeaponManagerReady(WeaponManager weaponManager)
    {
        // Store reference to local weapon manager
        _localWeaponManager = weaponManager;

        // Subscribe to all weapon hit events
        foreach (IWeaponLogic weapon in weaponManager.GetAllWeapons())
        {
            // Capture weapon in lambda to check ownership per hit
            IWeaponLogic capturedWeapon = weapon;
            weapon.OnHit += (hitInfo) => ShowHitmarker(capturedWeapon, hitInfo);
        }
    }

    private void OnPlayerKilled(PlayerInfo attacker, PlayerInfo victim)
    {
        // Check if the local player is the attacker
        // Since _localWeaponManager is only set for the local player's WeaponManager,
        // we can simply check if it exists and the attacker matches its owner
        if (_localWeaponManager != null && _localWeaponManager.owner == attacker.playerID)
        {
            AnimateKillIcon();
            if (_killSfx != null)
                SoundManager.PlayNonDiegetic(_killSfx);
        }
    }

    private void ShowHitmarker(IWeaponLogic weapon, HitInfo hitInfo)
    {
        // Only show hitmarker for local player's hits
        if (!weapon.isOwner)
            return;

        if (hitInfo.hitPlayer)
        {
            AnimateHitmarker(hitInfo.isHeadshot);

            if (hitInfo.isHeadshot)
            {
                if (_headShotClip != null) SoundManager.Play(new SoundData(_headShotClip, varyPitch: false, varyVolume: false));
            }
            else
            {
                if (_bodyShotClip != null) SoundManager.Play(new SoundData(_bodyShotClip, varyPitch: false, varyVolume: false));
            }
        }
    }

    private void AnimateHitmarker(bool isHeadshot)
    {
        // Cancel any existing animation and restart from the beginning
        if (_currentTweenId != -1)
        {
            LeanTween.cancel(_currentTweenId);
        }

        // Reset to initial state
        _hitmarker.transform.localScale = _initialScale;

        // Set color based on headshot
        Color targetColor = isHeadshot ? _headshotColor : _bodyHitColor;
        _hitmarker.color = targetColor;

        // Animate scale
        float finalScaleMultiplier = _scaleMultiplier;
        if (isHeadshot) finalScaleMultiplier *= _headshotScaleMultiplier;

        LeanTween.scale(_hitmarker.gameObject, _initialScale * finalScaleMultiplier, _animationDuration)
            .setEase(LeanTweenType.easeOutCubic);

        // Animate alpha fade out
        _currentTweenId = LeanTween.alpha(_hitmarker.rectTransform, 0f, _animationDuration)
            .setEase(LeanTweenType.easeOutCubic)
            .id;
    }

    private void SetHitmarkerAlpha(float alpha)
    {
        Color color = _hitmarker.color;
        color.a = alpha;
        _hitmarker.color = color;
    }

    private void AnimateKillIcon()
    {
        if (_killIcon == null) return;

        // Cancel any existing animation
        if (_killIconTweenId != -1)
        {
            LeanTween.cancel(_killIconTweenId);
        }

        // Start invisible
        SetKillIconAlpha(0f);

        // Delay before showing and animating
        LeanTween.delayedCall(_killIconDelay, () =>
        {
            // Reset to initial state
            _killIcon.transform.localScale = _killIconInitialScale;
            SetKillIconAlpha(1f);

            // Animate scale up (expand outwards like hitmarker)
            LeanTween.scale(_killIcon.gameObject, _killIconInitialScale * _killIconInitialRelativeScale, _killIconDuration)
                .setEase(LeanTweenType.easeOutCubic);

            // Animate alpha fade out
            _killIconTweenId = LeanTween.alpha(_killIcon.rectTransform, 0f, _killIconDuration)
                .setEase(LeanTweenType.easeOutCubic)
                .id;
        });
    }

    private void SetKillIconAlpha(float alpha)
    {
        Color color = _killIcon.color;
        color.a = alpha;
        _killIcon.color = color;
    }
}
