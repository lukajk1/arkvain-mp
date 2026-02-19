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
    [SerializeField] private float _envHitSimSpeed = 1f;
    [SerializeField] private float _envHitNormalOffset = 0.05f;

    [Header("Tracer Line")]
    [SerializeField] private LineRenderer _tracerLine;
    [SerializeField] private float _tracerDuration = 0.1f;
    [SerializeField] private float _tracerMinDistance = 3f;

    [Header("Reload Effects")]
    [SerializeField] private AudioClip _reloadComplete;

    private Coroutine _activeBulletCoroutine;
    private GameObject _activeBulletObject;

    private Coroutine _tracerCoroutine;
    private Vector3 _tracerMuzzlePos;

    private void Awake()
    {
        if (_envHitParticles != null)
            VFXPoolManager.Instance.RegisterPrefab(_envHitParticles.gameObject, simSpeed: _envHitSimSpeed);

        if (_bulletPrefab != null)
            VFXPoolManager.Instance.RegisterPrefab(_bulletPrefab, initialCapacity: 30, maxSize: 50);
    }

    /// <summary>
    /// Called when the crossbow shoots. Plays muzzle flash and shoot sound (every other shot).
    /// </summary>
    protected override void OnShoot(Vector3 fireDirection)
    {
        // Immediately stop any currently playing animation and play shoot animation
        if (_animancer != null && _shootClip != null)
        {
            _animancer.Stop();
            var shootState = _animancer.Play(_shootClip, 0f); // No fade, immediate transition
            shootState.Events(this).OnEnd = PlayIdleAnimation;
        }

        if (_muzzleFlashParticles != null)
        {
            if (!_muzzleFlashParticles.gameObject.activeInHierarchy)
            {
                _muzzleFlashParticles.gameObject.SetActive(true);
            }
            _muzzleFlashParticles.Clear();
            _muzzleFlashParticles.Play(true);
        }

        if (_shootSound != null && _weaponLogic.isOwner)
            SoundManager.PlayNonDiegetic(_shootSound, varyVolume: false);

        // Draw tracer line from muzzle to max range, hide after duration
        if (_tracerLine != null)
        {
            if (_tracerCoroutine != null)
                StopCoroutine(_tracerCoroutine);

            _tracerMuzzlePos = _bulletTrailOrigin != null ? _bulletTrailOrigin.position : transform.position;
            _tracerLine.SetPosition(0, _tracerMuzzlePos);
            _tracerLine.SetPosition(1, _tracerMuzzlePos + fireDirection * _bulletMaxDistance);
_tracerLine.enabled = true;
            _tracerCoroutine = StartCoroutine(HideTracerAfterDelay());
        }

        // Spawn bullet that travels to max distance (will be stopped early by OnHit if something is hit)
        // disable for now
        if (false && _bulletPrefab != null && VFXPoolManager.Instance != null)
        {
            Vector3 startPos = _bulletTrailOrigin.position;
            Vector3 endPos = startPos + fireDirection * _bulletMaxDistance;
            Quaternion rotation = Quaternion.LookRotation(fireDirection);
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
        // Play hit particles via centralized manager (blood effects if applicable)
        WeaponHitEffectsManager.PlayHitEffect(hitInfo, _weaponLogic.isOwner);

        if (VFXPoolManager.Instance != null && Camera.main != null && _envHitParticles != null)
        {
            float distanceSqr = (Camera.main.transform.position - hitInfo.position).sqrMagnitude;
            if (!hitInfo.hitPlayer && distanceSqr < ClientGame.maxVFXDistance * ClientGame.maxVFXDistance)
            {
                // Orient the particle effect so its Z+ axis aligns with the surface normal
                Quaternion rotation = Quaternion.LookRotation(hitInfo.surfaceNormal);
                Vector3 spawnPos = hitInfo.position + hitInfo.surfaceNormal * _envHitNormalOffset;
                VFXPoolManager.Instance.Spawn(_envHitParticles.gameObject, spawnPos, rotation);
            }
        }

        // Stop the active bullet early and redirect it to the actual hit position
        if (_activeBulletCoroutine != null && _activeBulletObject != null)
        {
            StopCoroutine(_activeBulletCoroutine);
            Vector3 currentPos = _activeBulletObject.transform.position;
            _activeBulletCoroutine = StartCoroutine(AnimateBulletToHit(_activeBulletObject, currentPos, hitInfo.position));
        }

        // Correct tracer end point to actual hit position and restart hide timer
        if (_tracerLine != null && _tracerLine.enabled)
        {
            if (_tracerCoroutine != null)
                StopCoroutine(_tracerCoroutine);

            if (Vector3.Distance(_tracerMuzzlePos, hitInfo.position) < _tracerMinDistance)
            {
                _tracerLine.enabled = false;
                _tracerCoroutine = null;
            }
            else
            {
                _tracerLine.SetPosition(0, _tracerMuzzlePos);
                _tracerLine.SetPosition(1, hitInfo.position);
                _tracerCoroutine = StartCoroutine(HideTracerAfterDelay());
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
            if (_weaponLogic.isOwner)
                SoundManager.PlayNonDiegetic(_reloadComplete, varyPitch: false, varyVolume: false);
            else
                SoundManager.PlayDiegetic(_reloadComplete, transform.position, varyPitch: false, varyVolume: false);
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

    private System.Collections.IEnumerator HideTracerAfterDelay()
    {
        yield return new WaitForSeconds(_tracerDuration);
        _tracerLine.enabled = false;
        _tracerCoroutine = null;
    }
}
