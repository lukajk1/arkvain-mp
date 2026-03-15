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

    [Header("Beam")]
    [SerializeField] private LineRenderer _tracerLine;
    [SerializeField] private Transform _beamOrigin;
    [SerializeField] private float _beamMaxRange = 9f;

    [Header("Continuous Hit Effect")]
    [SerializeField] private ParticleSystem _envHitParticles;
    [SerializeField] private float _envHitNormalOffset = 0.05f;

    [Header("Reload Effects")]
    [SerializeField] private AudioClip _reloadComplete;

    private Vector3 _lastHitPosition;
    private Vector3 _lastHitNormal;
    private bool _isHitting;

    private void Awake()
    {
        if (_tracerLine != null)
            _tracerLine.enabled = false;

        if (_envHitParticles != null)
        {
            _envHitParticles.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // Update continuous hit particle position if hitting
        if (_isHitting && _envHitParticles != null)
        {
            _envHitParticles.transform.position = _lastHitPosition;
            _envHitParticles.transform.rotation = Quaternion.LookRotation(_lastHitNormal);
        }
        else if (!_isHitting && _envHitParticles != null && _envHitParticles.gameObject.activeInHierarchy)
        {
            // Disable if we're no longer hitting
            _envHitParticles.gameObject.SetActive(false);
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
    }

    /// <summary>
    /// Called when a shot hits something. Plays appropriate particle and sound based on what was hit.
    /// </summary>
    protected override void OnHit(HitInfo hitInfo)
    {
        // Play hit particles via centralized manager (blood effects if applicable)
        WeaponHitEffectsManager.PlayHitEffect(hitInfo, _weaponLogic.isOwner);

        // Update continuous hit effect for environment hits
        if (!hitInfo.hitPlayer && _envHitParticles != null)
        {
            _isHitting = true;
            _lastHitPosition = hitInfo.position + hitInfo.surfaceNormal * _envHitNormalOffset;
            _lastHitNormal = hitInfo.surfaceNormal;

            _envHitParticles.transform.position = _lastHitPosition;
            _envHitParticles.transform.rotation = Quaternion.LookRotation(_lastHitNormal);

            if (!_envHitParticles.gameObject.activeInHierarchy)
            {
                _envHitParticles.gameObject.SetActive(true);
            }
        }
        else if (hitInfo.hitPlayer && _envHitParticles != null)
        {
            // Don't show continuous particles when hitting players
            _isHitting = false;
            _envHitParticles.gameObject.SetActive(false);
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

        _isHitting = false;

        // Disable hit particles
        if (_envHitParticles != null)
        {
            _envHitParticles.gameObject.SetActive(false);
        }

        // Disable beam
        if (_tracerLine != null)
        {
            _tracerLine.enabled = false;
        }
    }


}
