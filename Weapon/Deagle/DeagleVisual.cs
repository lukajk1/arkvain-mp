using UnityEngine;

/// <summary>
/// Handles all visual and audio feedback for the Deagle.
/// Subscribes to events from DeagleLogic and plays appropriate effects.
/// </summary>
public class DeagleVisual : WeaponVisual<DeagleLogic>
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

    private void Awake()
    {
        // Register VFX prefabs with the pool manager
        if (VFXPoolManager.Instance != null)
        {
            if (_hitBodyParticles != null)
                VFXPoolManager.Instance.RegisterPrefab(_hitBodyParticles);
            if (_hitWallParticles != null)
                VFXPoolManager.Instance.RegisterPrefab(_hitWallParticles);
        }
    }

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
        _deagleLogic.OnEquipped += OnEquipped;
    }

    private void OnDisable()
    {
        if (_deagleLogic == null) return;

        // Unsubscribe from events
        _deagleLogic.OnShoot -= OnShoot;
        _deagleLogic.OnHit -= OnHit;
        _deagleLogic.onReload -= OnReload;
        _deagleLogic.OnEquipped -= OnEquipped;
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
        // Play appropriate particle effect from pool
        if (hitInfo.hitPlayer)
        {
            // Blood effects and hit sound only for attacker
            if (_deagleLogic.isOwner)
            {
                if (_hitBodyParticles != null && VFXPoolManager.Instance != null)
                {
                    VFXPoolManager.Instance.Spawn(_hitBodyParticles, hitInfo.position, Quaternion.identity);
                }

                if (_hitSound != null)
                {
                    SoundManager.Play(new SoundData(_hitSound, blend: SoundData.SoundBlend.Spatial, soundPos: hitInfo.position));
                }
            }
        }
        else
        {
            // Wall/environment effects for everyone, but only if close enough to local player
            if (Camera.main != null)
            {
                float distanceSqr = (Camera.main.transform.position - hitInfo.position).sqrMagnitude;
                if (distanceSqr < ClientGame.maxVFXDistance * ClientGame.maxVFXDistance)
                {
                    if (_hitWallParticles != null && VFXPoolManager.Instance != null)
                    {
                        VFXPoolManager.Instance.Spawn(_hitWallParticles, hitInfo.position, Quaternion.identity);
                    }
                }
            }
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
    private void OnEquipped()
    {
        if (_animator != null && !string.IsNullOrEmpty(_equipAnimationTrigger))
        {
            _animator.SetTrigger(_equipAnimationTrigger);
        }
    }
}
