using UnityEngine;

/// <summary>
/// Handles all visual and audio feedback for the Deagle.
/// Subscribes to events from DeagleLogic and plays appropriate effects.
/// </summary>
public class DeagleVisual : WeaponVisual<DeagleLogic>
{
    [Header("Shoot Effects")]
    [SerializeField] private ParticleSystem _muzzleFlashParticles;
    [SerializeField] private AudioClip _shootSound;

    [Header("Hit Effects")]
    [SerializeField] private AudioClip _hitSound;

    [Header("Reload Effects")]
    [SerializeField] private AudioClip _reloadSound;

    [Header("Equip Animation")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _equipAnimationTrigger = "Equip";

    /// <summary>
    /// Called when the deagle shoots. Plays muzzle flash and shoot sound.
    /// </summary>
    protected override void OnShoot(Vector3 fireDirection)
    {
        base.OnShoot(fireDirection); // Call base to trigger animation if configured

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
    protected override void OnHit(HitInfo hitInfo)
    {
        // Play hit particles via centralized manager
        WeaponHitEffectsManager.PlayHitEffect(hitInfo, _weaponLogic.isOwner);

        // Play hit sound for body hits (only for owner)
        if (hitInfo.hitPlayer && _weaponLogic.isOwner && _hitSound != null)
        {
            SoundManager.Play(new SoundData(_hitSound, blend: SoundData.SoundBlend.Spatial, soundPos: hitInfo.position));
        }
    }

    /// <summary>
    /// Called when the deagle reloads. Plays reload sound.
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
    /// Called when the deagle becomes the active weapon. Plays equip animation.
    /// </summary>
    protected override void OnEquipped()
    {
        base.OnEquipped(); // Call base to trigger animation if configured

        if (_animator != null && !string.IsNullOrEmpty(_equipAnimationTrigger))
        {
            _animator.SetTrigger(_equipAnimationTrigger);
        }
    }
}
