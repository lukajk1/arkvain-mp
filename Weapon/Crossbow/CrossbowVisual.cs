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
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private Transform _bulletTrailOrigin;
    [SerializeField] private float _bulletMaxDistance = 100f;
    [SerializeField] private float _bulletSpeed = 100f;

    [SerializeField] private ParticleSystem _envHitParticles;

    [Header("Reload Effects")]
    [SerializeField] private AudioClip _reloadComplete;

    private Coroutine _activeBulletCoroutine;
    private GameObject _activeBulletObject;

    private void Awake()
    {
        if (_envHitParticles != null)
            VFXPoolManager.Instance.RegisterPrefab(_envHitParticles.gameObject);

        if (_bulletPrefab != null)
            VFXPoolManager.Instance.RegisterPrefab(_bulletPrefab, initialCapacity: 30, maxSize: 50);
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

        // Spawn bullet that travels to max distance (will be stopped early by OnHit if something is hit)
        if (_bulletPrefab != null && Camera.main != null && VFXPoolManager.Instance != null)
        {
            Vector3 startPos = _bulletTrailOrigin.position;
            Vector3 endPos = startPos + Camera.main.transform.forward * _bulletMaxDistance;
            Quaternion rotation = Quaternion.LookRotation(Camera.main.transform.forward);
            GameObject bulletObj = VFXPoolManager.Instance.Spawn(_bulletPrefab, startPos, rotation);

            if (bulletObj != null)
            {
                _activeBulletObject = bulletObj;
                _activeBulletCoroutine = StartCoroutine(AnimateBulletToHit(bulletObj, startPos, endPos));
            }
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
            if (!hitInfo.hitPlayer && distanceSqr < ClientGame.maxVFXDistance * ClientGame.maxVFXDistance)
            {
                // Orient the particle effect so its Z+ axis aligns with the surface normal
                Quaternion rotation = Quaternion.LookRotation(hitInfo.surfaceNormal);
                VFXPoolManager.Instance.Spawn(_envHitParticles.gameObject, hitInfo.position, rotation);
            }
        }

        // Stop the active bullet early and redirect it to the actual hit position
        if (_activeBulletCoroutine != null && _activeBulletObject != null)
        {
            StopCoroutine(_activeBulletCoroutine);
            Vector3 currentPos = _activeBulletObject.transform.position;
            _activeBulletCoroutine = StartCoroutine(AnimateBulletToHit(_activeBulletObject, currentPos, hitInfo.position));
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

    /// <summary>
    /// Called when the crossbow is holstered (deactivated).
    /// </summary>
    protected override void OnHolstered()
    {
        base.OnHolstered(); // Stops animations
    }

    /// <summary>
    /// Animates the bullet from start to end position, then returns it to the pool.
    /// </summary>
    private System.Collections.IEnumerator AnimateBulletToHit(GameObject bulletObj, Vector3 startPos, Vector3 endPos)
    {
        float distance = Vector3.Distance(startPos, endPos);
        float duration = distance / _bulletSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            bulletObj.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        // Ensure final position is exact
        bulletObj.transform.position = endPos;

        // Return to pool after arrival
        if (VFXPoolManager.Instance != null)
        {
            VFXPoolManager.Instance.Return(bulletObj);
        }

        // Clear active references
        _activeBulletCoroutine = null;
        _activeBulletObject = null;
    }
}
