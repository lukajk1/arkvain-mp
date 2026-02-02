using UnityEngine;
using UnityEngine.Serialization;
using Animancer;

/// <summary>
/// Handles all visual and audio feedback for the Railgun.
/// Subscribes to events from RailgunLogic and plays appropriate effects.
/// </summary>
public class RailgunVisual : WeaponVisual<RailgunLogic>
{

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


    /// <summary>
    /// Called when the railgun shoots. Plays muzzle flash and shoot sound.
    /// </summary>
    protected override void OnShoot()
    {
        base.OnShoot(); // Plays shoot animation

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
    protected override void OnHit(HitInfo hitInfo)
    {
        // Play appropriate particle effect from pool
        if (hitInfo.hitPlayer)
        {
            // Blood effects and hit sound only for attacker
            if (_weaponLogic.isOwner)
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
    protected override void OnReload()
    {
        base.OnReload(); // Plays reload animation
    }

    protected override void OnReloadComplete()
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
    protected override void OnEquipped()
    {
        base.OnEquipped(); // Plays equip animation

        if (_passiveShotReadyClip != null && _passiveHumObject == null) // ensure that there is not a hum already playing
        {
            _passiveHumObject = SoundManager.StartLoop(new SoundData(_passiveShotReadyClip, isLooping: true));
        }

        Debug.Log("[RailgunVisual] Weapon equipped");
    }

    /// <summary>
    /// Called when the railgun is holstered (deactivated).
    /// </summary>
    protected override void OnHolstered()
    {
        base.OnHolstered(); // Stops animations

        if (_passiveHumObject != null)
        {
            SoundManager.StopLoop(_passiveHumObject);
            _passiveHumObject = null;
        }

        Debug.Log("[RailgunVisual] Weapon holstered");
    }
}
