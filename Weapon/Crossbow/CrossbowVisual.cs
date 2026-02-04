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

    [SerializeField] private ParticleSystem _envHitParticles;

    [Header("Reload Effects")]
    [SerializeField] private AudioClip _reloadComplete;

    private void Awake()
    {
        if (_envHitParticles != null)
            VFXPoolManager.Instance.RegisterPrefab(_envHitParticles.gameObject);
    }

    /// <summary>
    /// Called when the crossbow shoots. Plays muzzle flash and shoot sound (every other shot).
    /// </summary>
    protected override void OnShoot()
    {
        Debug.Log("[CrossbowVisual] OnShoot called");
        Debug.Log($"[CrossbowVisual] Animancer: {(_animancer != null ? "assigned" : "NULL")}, ShootClip: {(_shootClip != null ? "assigned" : "NULL")}");

        // Immediately stop any currently playing animation and play shoot animation
        if (_animancer != null && _shootClip != null)
        {
            _animancer.Stop();
            var shootState = _animancer.Play(_shootClip, 0f); // No fade, immediate transition
            shootState.Events(this).OnEnd = PlayIdleAnimation;
        }

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

        if (VFXPoolManager.Instance != null && Camera.main != null && _envHitParticles != null)
        {
            float distanceSqr = (Camera.main.transform.position - hitInfo.position).sqrMagnitude;
            if (distanceSqr < ClientGame.maxVFXDistance * ClientGame.maxVFXDistance)
            {
                // Orient the particle effect so its Z+ axis aligns with the surface normal
                Quaternion rotation = Quaternion.LookRotation(hitInfo.surfaceNormal);
                VFXPoolManager.Instance.Spawn(_envHitParticles.gameObject, hitInfo.position, rotation);
            }
        }
    }

    /// <summary>
    /// Called when the crossbow reloads. Plays reload sound.
    /// </summary>
    protected override void OnReload()
    {
        base.OnReload(); // Call base to trigger animation if configured
    }

    protected override void OnReloadComplete()
    {
        base.OnReloadComplete(); // Call base to trigger animation if configured

        if (_reloadComplete != null)
        {
            SoundManager.Play(new SoundData(_reloadComplete));
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
