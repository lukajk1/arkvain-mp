using UnityEngine;

/// <summary>
/// Handles all visual and audio feedback for the crossbow.
/// Subscribes to events from CrossbowLogic and plays appropriate effects.
/// </summary>
public class CrossbowVisual : WeaponVisual
{
    [Header("References")]
    [SerializeField] private CrossbowLogic _crossbowLogic;

    [Header("Shoot Effects")]
    [SerializeField] private ParticleSystem _muzzleFlashParticles;
    [SerializeField] private AudioClip _shootSound;

    [Header("Hit Effects")]
    [SerializeField] private GameObject _hitBodyParticles;
    [SerializeField] private GameObject _hitWallParticles;
    [SerializeField] private AudioClip _hitSound;

    [Header("Reload Effects")]
    [SerializeField] private AudioClip _reloadSound;

    private void OnEnable()
    {
        if (_crossbowLogic == null)
        {
            Debug.LogError("[CrossbowVisual] CrossbowLogic reference is null!");
            return;
        }

        // Subscribe to events
        _crossbowLogic.onShoot += OnShoot;
        _crossbowLogic.onHit += OnHit;
        _crossbowLogic.onReload += OnReload;
        _crossbowLogic.onSwitchToActive += OnSwitchToActive;
    }

    private void OnDisable()
    {
        if (_crossbowLogic == null) return;

        // Unsubscribe from events
        _crossbowLogic.onShoot -= OnShoot;
        _crossbowLogic.onHit -= OnHit;
        _crossbowLogic.onReload -= OnReload;
        _crossbowLogic.onSwitchToActive -= OnSwitchToActive;
    }

    /// <summary>
    /// Called when the crossbow shoots. Plays muzzle flash and shoot sound.
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
    /// Called when the crossbow reloads. Plays reload sound and particles.
    /// </summary>
    private void OnReload()
    {

        if (_reloadSound != null)
        {
            SoundManager.Play(new SoundData(_reloadSound, blend: SoundData.SoundBlend.Spatial, soundPos: transform.position));
        }
    }

    /// <summary>
    /// Called when the crossbow becomes the active weapon. Plays equip animation.
    /// </summary>
    private void OnSwitchToActive()
    {
        _viewmodel.SetActive(true);
    }
}
