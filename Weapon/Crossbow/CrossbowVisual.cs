using Mono.CSharp;
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

    [Header("VFX Settings")]
    [SerializeField] private float _maxVFXDistance = 50f;

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
        if (_crossbowLogic == null)
        {
            Debug.LogError("[CrossbowVisual] CrossbowLogic reference is null!");
            return;
        }

        // Subscribe to events
        _crossbowLogic.OnShoot += OnShoot;
        _crossbowLogic.OnHit += OnHit;
        _crossbowLogic.onReload += OnReload;
        _crossbowLogic.OnEquipped += OnEquipped;
    }

    private void OnDisable()
    {
        if (_crossbowLogic == null) return;

        // Unsubscribe from events
        _crossbowLogic.OnShoot -= OnShoot;
        _crossbowLogic.OnHit -= OnHit;
        _crossbowLogic.onReload -= OnReload;
        _crossbowLogic.OnEquipped -= OnEquipped;
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
        // Play appropriate particle effect from pool
        if (hitInfo.hitPlayer)
        {
            // Blood effects and hit sound only for attacker
            if (_crossbowLogic.isOwner)
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
                if (distanceSqr < _maxVFXDistance * _maxVFXDistance)
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
    private void OnEquipped()
    {
        _viewmodel.SetActive(true);
    }
}
