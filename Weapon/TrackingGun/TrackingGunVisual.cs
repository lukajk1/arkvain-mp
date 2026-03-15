using UnityEngine;

/// <summary>
/// Handles all visual and audio feedback for the Tracking Gun.
/// Subscribes to events from TrackingGunLogic and plays appropriate effects.
/// </summary>
public class TrackingGunVisual : WeaponVisual<TrackingGunLogic>
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

    [Header("Beam")]
    [SerializeField] private LineRenderer _tracerLine;
    [SerializeField] private Transform _beamOrigin;
    [SerializeField] private float _beamMaxRange = 9f;

    [Header("Continuous Beam Hit Effect")]
    [SerializeField] private ParticleSystem _continuousHitParticles;

    [Header("Reload Effects")]
    [SerializeField] private AudioClip _reloadComplete;

    private Coroutine _activeBulletCoroutine;
    private GameObject _activeBulletObject;

    private Vector3 _lastHitPosition;
    private Vector3 _lastHitNormal;
    private bool _isHitting;

    private void Awake()
    {
        if (_tracerLine != null)
            _tracerLine.enabled = false;

        if (_continuousHitParticles != null)
        {
            _continuousHitParticles.Stop();
            _continuousHitParticles.gameObject.SetActive(false);
        }

        if (_envHitParticles != null)
            VFXPoolManager.Instance.RegisterPrefab(_envHitParticles.gameObject, simSpeed: _envHitSimSpeed);

        if (_bulletPrefab != null)
            VFXPoolManager.Instance.RegisterPrefab(_bulletPrefab, initialCapacity: 30, maxSize: 50);
    }

    private void Update()
    {
        // Update continuous hit particle position if hitting
        if (_isHitting && _continuousHitParticles != null)
        {
            _continuousHitParticles.transform.position = _lastHitPosition;
            _continuousHitParticles.transform.rotation = Quaternion.LookRotation(_lastHitNormal);
        }
        else if (!_isHitting && _continuousHitParticles != null && _continuousHitParticles.isPlaying)
        {
            // Stop playing if we're no longer hitting
            DisableContinuousHitEffect();
        }

        // Draw beam while attack button is held
        if (_tracerLine != null && _beamOrigin != null && _weaponLogic != null && _weaponLogic.IsCurrent)
        {
            bool attackHeld = PersistentClient.Instance.inputManager.Player.Attack.IsPressed();

            if (attackHeld)
            {
                Vector3 beamStart = _beamOrigin.position;
                Vector3 beamEnd = beamStart + _beamOrigin.forward * _beamMaxRange;

                _tracerLine.SetPosition(0, beamStart);
                _tracerLine.SetPosition(1, beamEnd);

                if (!_tracerLine.enabled)
                {
                    _tracerLine.enabled = true;
                }
            }
            else if (_tracerLine.enabled)
            {
                _tracerLine.enabled = false;
            }
        }
    }

    /// <summary>
    /// Called when the tracking gun shoots. Plays muzzle flash and shoot sound.
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

        // Reset hit flag - will be set to true by OnHit if we actually hit something this frame
        _isHitting = false;

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

        // Update continuous hit effect for environment hits
        if (!hitInfo.hitPlayer && _continuousHitParticles != null)
        {
            _isHitting = true;
            _lastHitPosition = hitInfo.position + hitInfo.surfaceNormal * _envHitNormalOffset;
            _lastHitNormal = hitInfo.surfaceNormal;

            if (!_continuousHitParticles.gameObject.activeInHierarchy)
            {
                _continuousHitParticles.gameObject.SetActive(true);
            }

            if (!_continuousHitParticles.isPlaying)
            {
                _continuousHitParticles.Play();
            }

            _continuousHitParticles.transform.position = _lastHitPosition;
            _continuousHitParticles.transform.rotation = Quaternion.LookRotation(_lastHitNormal);
        }
        else if (hitInfo.hitPlayer && _continuousHitParticles != null)
        {
            // Don't show continuous particles when hitting players
            DisableContinuousHitEffect();
        }

        if (VFXPoolManager.Instance != null && Camera.main != null && _envHitParticles != null)
        {
            float distanceSqr = (Camera.main.transform.position - hitInfo.position).sqrMagnitude;
            if (!hitInfo.hitPlayer && distanceSqr < ClientsideGameManager.maxVFXDistance * ClientsideGameManager.maxVFXDistance)
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

    }

    /// <summary>
    /// Called when the tracking gun reloads. Plays reload sound.
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
    /// Called when the tracking gun becomes the active weapon. Shows the viewmodel.
    /// </summary>
    protected override void OnEquipped()
    {
        base.OnEquipped(); // Call base to trigger animation if configured
        Show(); // Show the viewmodel
    }

    /// <summary>
    /// Called when the tracking gun is holstered (deactivated).
    /// </summary>
    protected override void OnHolstered()
    {
        base.OnHolstered(); // Stops animations
        DisableContinuousHitEffect();

        // Disable beam
        if (_tracerLine != null)
        {
            _tracerLine.enabled = false;
        }
    }

    private void DisableContinuousHitEffect()
    {
        _isHitting = false;

        if (_continuousHitParticles != null)
        {
            _continuousHitParticles.Stop();
            _continuousHitParticles.gameObject.SetActive(false);
        }
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
