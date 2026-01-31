using UnityEngine;

/// <summary>
/// Handles all visual and audio feedback for the Deagle.
/// Subscribes to events from DeagleLogic and plays appropriate effects.
/// </summary>
public class DeagleVisual : WeaponVisual
{
    [Header("References")]
    [SerializeField] private DeagleLogic _deagleLogic;

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
        if (_deagleLogic == null)
        {
            Debug.LogError("[DeagleVisual] DeagleLogic reference is null!");
            return;
        }

        // Subscribe to events
        _deagleLogic.OnShoot += OnShoot;
        _deagleLogic.OnHit += OnHit;
        _deagleLogic.onReload += OnReload;
        _deagleLogic.onSwitchToActive += OnSwitchToActive;
    }

    private void OnDisable()
    {
        if (_deagleLogic == null) return;

        // Unsubscribe from events
        _deagleLogic.OnShoot -= OnShoot;
        _deagleLogic.OnHit -= OnHit;
        _deagleLogic.onReload -= OnReload;
        _deagleLogic.onSwitchToActive -= OnSwitchToActive;
    }

    /// <summary>
    /// Called when the deagle shoots. Plays muzzle flash and shoot sound.
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
    /// Called when the deagle reloads. Plays reload sound.
    /// </summary>
    private void OnReload()
    {
        if (_reloadSound != null)
        {
            SoundManager.Play(new SoundData(_reloadSound, blend: SoundData.SoundBlend.Spatial, soundPos: transform.position));
        }
    }

    /// <summary>
    /// Called when the deagle becomes the active weapon. Plays equip animation.
    /// </summary>
    private void OnSwitchToActive()
    {
        if (_animator != null && !string.IsNullOrEmpty(_equipAnimationTrigger))
        {
            _animator.SetTrigger(_equipAnimationTrigger);
        }
    }
}
