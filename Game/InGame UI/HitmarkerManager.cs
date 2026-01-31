using UnityEngine;
using UnityEngine.UI;

public class HitmarkerManager : MonoBehaviour
{
    [SerializeField] private Image _hitmarker;
    [SerializeField] private float _animationDuration = 0.3f;
    [SerializeField] private float _scaleMultiplier = 1.3f; // Scale up by 30%
    [SerializeField] private Color _bodyHitColor = Color.white;
    [SerializeField] private Color _headshotColor = Color.red;

    [SerializeField] private AudioClip _bodyShotClip;
    [SerializeField] private AudioClip _headShotClip;

    private Vector3 _initialScale;
    private int _currentTweenId = -1;

    private void Awake()
    {
        if (_hitmarker != null)
        {
            _initialScale = _hitmarker.transform.localScale;
            // Start with hitmarker invisible
            SetHitmarkerAlpha(0f);
        }
    }

    private void OnEnable()
    {
        WeaponManager.OnLocalWeaponManagerReady += OnWeaponManagerReady;
    }

    private void OnDisable()
    {
        WeaponManager.OnLocalWeaponManagerReady -= OnWeaponManagerReady;

        // Cancel any running tweens
        if (_currentTweenId != -1)
        {
            LeanTween.cancel(_currentTweenId);
        }
    }

    private void OnWeaponManagerReady(WeaponManager weaponManager)
    {
        // Subscribe to all weapon hit events
        foreach (IWeaponLogic weapon in weaponManager.GetAllWeapons())
        {
            weapon.OnHit += ShowHitmarker;
        }
    }

    private void ShowHitmarker(HitInfo hitInfo)
    {
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
        LeanTween.scale(_hitmarker.gameObject, _initialScale * _scaleMultiplier, _animationDuration)
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
}
