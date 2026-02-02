using UnityEngine;

/// <summary>
/// Handles all visual and audio feedback for the crossbow.
/// Subscribes to events from CrossbowLogic and plays appropriate effects.
/// </summary>
public class CrossbowVisual : WeaponVisual<CrossbowLogic>
{
    [Header("Shoot Effects")]
    [SerializeField] private ParticleSystem _muzzleFlashParticles;
    [SerializeField] private AudioClip _shootSound;

    [Header("Reload Effects")]
    [SerializeField] private AudioClip _reloadSound;

    private int _shotCounter = 0;

    private void Awake()
    {

    }

    /// <summary>
    /// Called when the crossbow shoots. Plays muzzle flash and shoot sound (every other shot).
    /// </summary>
    protected override void OnShoot()
    {
        base.OnShoot(); // Call base to trigger animation if configured

        if (_muzzleFlashParticles != null)
        {
            _muzzleFlashParticles.Play();
        }

        // Only play sound every other shot
        _shotCounter++;
        if (_shootSound != null && _shotCounter % 2 == 0)
        {
            SoundManager.Play(new SoundData(_shootSound, blend: SoundData.SoundBlend.Spatial, soundPos: transform.position));
        }
    }

    /// <summary>
    /// Called when a shot hits something. Plays appropriate particle and sound based on what was hit.
    /// </summary>
    protected override void OnHit(HitInfo hitInfo)
    {
        // Play hit particles via centralized manager
        WeaponHitEffectsManager.PlayHitEffect(hitInfo, _weaponLogic.isOwner);
    }

    /// <summary>
    /// Called when the crossbow reloads. Plays reload sound.
    /// </summary>
    protected override void OnReload()
    {
        base.OnReload(); // Call base to trigger animation if configured

        if (_reloadSound != null)
        {
            SoundManager.Play(new SoundData(_reloadSound, blend: SoundData.SoundBlend.Spatial, soundPos: transform.position));
        }
    }

    /// <summary>
    /// Called when the crossbow becomes the active weapon. Shows the viewmodel.
    /// </summary>
    protected override void OnEquipped()
    {
        base.OnEquipped(); // Call base to trigger animation if configured
        Show(); // Show the viewmodel
    }
}
