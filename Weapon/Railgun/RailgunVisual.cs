using UnityEngine;

/// <summary>
/// Handles all visual and audio feedback for the Railgun.
/// Subscribes to events from RailgunLogic and plays appropriate effects.
/// </summary>
public class RailgunVisual : WeaponVisual
{
    [Header("References")]
    [SerializeField] private RailgunLogic _railgunLogic;

    [Header("Shoot Effects")]
    [SerializeField] private ParticleSystem _muzzleFlashParticles;
    [SerializeField] private AudioClip _shootSound;

    [Header("Hit Effects")]
    [SerializeField] private GameObject _hitBodyParticles;
    [SerializeField] private GameObject _hitWallParticles;
    [SerializeField] private AudioClip _hitSound;

    [Header("Reload Effects")]
    [SerializeField] private AudioClip _reloadSound;

    [Header("Switch to Active")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _equipAnimationTrigger = "Equip";

    private void OnEnable()
    {
        if (_railgunLogic == null)
        {
            Debug.LogError("[RailgunVisual] RailgunLogic reference is null!");
            return;
        }

        // Subscribe to events
        _railgunLogic.OnShoot += OnShoot;
        _railgunLogic.OnHit += OnHit;
        _railgunLogic.onReload += OnReload;
        _railgunLogic.onSwitchToActive += OnSwitchToActive;
    }

    private void OnDisable()
    {
        if (_railgunLogic == null) return;

        // Unsubscribe from events
        _railgunLogic.OnShoot -= OnShoot;
        _railgunLogic.OnHit -= OnHit;
        _railgunLogic.onReload -= OnReload;
        _railgunLogic.onSwitchToActive -= OnSwitchToActive;
    }

    /// <summary>
    /// Called when the railgun shoots. Plays muzzle flash and shoot sound.
    /// </summary>
    private void OnShoot()
    {
        if (_muzzleFlashParticles != null)
        {
            _muzzleFlashParticles.Play();
        }

        if (_shootSound != null)
        {
            SoundManager.Play(new SoundData(_shootSound, blend: SoundData.SoundBlend.Spatial, soundPos: transform.position));
        }
    }

    /// <summary>
    /// Called when a shot hits something. Plays appropriate particle and sound based on what was hit.
    /// </summary>
    private void OnHit(HitInfo hitInfo)
    {
        // Play appropriate particle effect
        if (hitInfo.hitPlayer)
        {
            if (_hitBodyParticles != null)
            {
                Instantiate(_hitBodyParticles, hitInfo.position, Quaternion.identity);
            }
        }
        else
        {
            if (_hitWallParticles != null)
            {
                Instantiate(_hitWallParticles, hitInfo.position, Quaternion.identity);
            }
        }

        // Play hit sound
        if (_hitSound != null)
        {
            SoundManager.Play(new SoundData(_hitSound, blend: SoundData.SoundBlend.Spatial, soundPos: hitInfo.position));
        }
    }

    /// <summary>
    /// Called when the railgun reloads. Plays reload sound.
    /// </summary>
    private void OnReload()
    {
        if (_reloadSound != null)
        {
            SoundManager.Play(new SoundData(_reloadSound, blend: SoundData.SoundBlend.Spatial, soundPos: transform.position));
        }
    }

    /// <summary>
    /// Called when the railgun becomes the active weapon. Plays equip animation.
    /// </summary>
    private void OnSwitchToActive()
    {
        if (_animator != null && !string.IsNullOrEmpty(_equipAnimationTrigger))
        {
            _animator.SetTrigger(_equipAnimationTrigger);
        }
    }
}
