using UnityEngine;
using UnityEngine.Serialization;
using Animancer;

/// <summary>
/// Handles all visual and audio feedback for the Railgun.
/// Subscribes to events from RailgunLogic and plays appropriate effects.
/// </summary>
public class RailgunVisual : WeaponVisual
{
    [Header("References")]
    [SerializeField] private RailgunLogic _railgunLogic;
    [SerializeField] private AnimancerComponent _animancer;

    [Header("Animation Clips")]
    [SerializeField] private AnimationClip _equipClip;
    [SerializeField] private AnimationClip _idleClip;
    [SerializeField] private AnimationClip _shootClip;
    [SerializeField] private AnimationClip _reloadClip;

    [Header("Animation Settings")]
    [SerializeField] private float _defaultFadeDuration = 0.25f;
    [SerializeField] private float _shootFadeDuration = 0.1f;

    [Header("Shoot Effects")]
    [SerializeField] private ParticleSystem _muzzleFlashParticles;
    [SerializeField] private AudioClip _shootSound;

    [Header("Hit Effects")]
    [SerializeField] private GameObject _hitBodyParticles;
    [SerializeField] private GameObject _hitWallParticles;
    [SerializeField] private AudioClip _hitSound;

    [Header("Reload Effects")]
    [FormerlySerializedAs("_reloadSound")] [SerializeField] private AudioClip _reloadCompleteClip;
    [SerializeField] private AudioClip _passiveShotReadyClip;

    [Header("VFX Settings")]
    [SerializeField] private float _maxVFXDistance = 50f;

    private AudioSource _passiveHumObject;
    private AnimancerState _currentState;
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
        if (_railgunLogic == null)
        {
            Debug.LogError("[RailgunVisual] RailgunLogic reference is null!");
            return;
        }

        // Subscribe to events
        _railgunLogic.OnShoot += OnShoot;
        _railgunLogic.OnHit += OnHit;
        _railgunLogic.OnEquipped += OnEquipped;
        _railgunLogic.OnHolstered += OnHolstered;
        _railgunLogic.onReload += OnReload;
        _railgunLogic.onReloadComplete += OnReloadComplete;
    }

    private void OnDisable()
    {
        if (_railgunLogic == null) return;

        // Unsubscribe from events
        _railgunLogic.OnShoot -= OnShoot;
        _railgunLogic.OnHit -= OnHit;
        _railgunLogic.OnEquipped -= OnEquipped;
        _railgunLogic.OnHolstered -= OnHolstered;
        _railgunLogic.onReload -= OnReload;
        _railgunLogic.onReloadComplete -= OnReloadComplete;
    }

    /// <summary>
    /// Called when the railgun shoots. Plays muzzle flash and shoot sound.
    /// </summary>
    private void OnShoot()
    {
        // Play shoot animation with quick fade, then return to idle
        if (_animancer != null && _shootClip != null)
        {
            var shootState = _animancer.Play(_shootClip, _shootFadeDuration);
            shootState.Events(this).OnEnd = PlayIdle;
        }

        if (_muzzleFlashParticles != null)
        {
            _muzzleFlashParticles.Play();
        }

        if (_shootSound != null)
        {
            SoundManager.Play(new SoundData(_shootSound, blend: SoundData.SoundBlend.Spatial, soundPos: transform.position));
        }

        if (_passiveHumObject != null)
        {
            SoundManager.StopLoop(_passiveHumObject);
            _passiveHumObject = null;
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
            if (_railgunLogic.isOwner)
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
    /// Called when the railgun starts reloading. Triggers reload animation.
    /// </summary>
    private void OnReload()
    {
        // Play reload animation, then return to idle
        if (_animancer != null && _reloadClip != null)
        {
            var reloadState = _animancer.Play(_reloadClip, _defaultFadeDuration);
            reloadState.Events(this).OnEnd = PlayIdle;
        }
    }
    
    private void OnReloadComplete()
    {
        if (_reloadCompleteClip != null)
        {
            SoundManager.Play(new SoundData(_reloadCompleteClip, blend: SoundData.SoundBlend.Spatial, soundPos: transform.position));
        }

        if (_passiveShotReadyClip != null)
        {
            _passiveHumObject = SoundManager.StartLoop(new SoundData(_passiveShotReadyClip, isLooping: true));
        }
    }

    /// <summary>
    /// Called when the railgun is equipped (becomes active weapon).
    /// Plays equip animation and any equip-specific effects.
    /// </summary>
    private void OnEquipped()
    {
        // Play equip animation, then transition to idle
        if (_animancer != null && _equipClip != null)
        {
            var equipState = _animancer.Play(_equipClip, _defaultFadeDuration);
            equipState.Events(this).OnEnd = PlayIdle;
        }
        else
        {
            // If no equip animation, go straight to idle
            PlayIdle();
        }

        if (_passiveShotReadyClip != null && _passiveHumObject == null) // ensure that there is not a hum already playing
        {
            _passiveHumObject = SoundManager.StartLoop(new SoundData(_passiveShotReadyClip, isLooping: true));
        }

        Debug.Log("[RailgunVisual] Weapon equipped");
    }

    /// <summary>
    /// Called when the railgun is holstered (deactivated).
    /// </summary>
    private void OnHolstered()
    {
        // Stop all animations
        if (_animancer != null)
        {
            _animancer.Stop();
        }

        if (_passiveHumObject != null)
        {
            SoundManager.StopLoop(_passiveHumObject);
            _passiveHumObject = null;
        }

        Debug.Log("[RailgunVisual] Weapon holstered");
    }

    /// <summary>
    /// Plays the idle animation in a loop.
    /// </summary>
    private void PlayIdle()
    {
        if (_animancer != null && _idleClip != null)
        {
            _currentState = _animancer.Play(_idleClip, _defaultFadeDuration);
        }
    }
}
