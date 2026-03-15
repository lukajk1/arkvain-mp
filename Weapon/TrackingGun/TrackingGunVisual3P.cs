using UnityEngine;

/// <summary>
/// Handles 3rd person visual feedback for the tracking gun.
/// Responsible for beam LineRenderer and environment hit effects.
/// </summary>
public class TrackingGunVisual3P : MonoBehaviour
{
    [SerializeField] private TrackingGunLogic _weaponLogic;

    [Header("Beam")]
    [SerializeField] private LineRenderer _beamLineRenderer;
    [SerializeField] private Transform _beamOrigin;
    [SerializeField] private float _beamMaxRange = 9f;

    [Header("Continuous Hit Effect")]
    [SerializeField] private ParticleSystem _envHitParticles;
    [SerializeField] private float _envHitNormalOffset = 0.05f;

    private Vector3 _lastHitPosition;
    private Vector3 _lastHitNormal;
    private bool _isHitting;

    private void Awake()
    {
        if (_beamLineRenderer != null)
            _beamLineRenderer.enabled = false;

        if (_envHitParticles != null)
            _envHitParticles.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (_weaponLogic == null)
        {
            Debug.LogError($"[TrackingGunVisual3P] Weapon logic reference is null!");
            return;
        }

        _weaponLogic.OnShoot += OnShoot;
        _weaponLogic.OnHit += OnHit;
    }

    private void OnDisable()
    {
        if (_weaponLogic == null) return;

        _weaponLogic.OnShoot -= OnShoot;
        _weaponLogic.OnHit -= OnHit;

        _isHitting = false;

        // Disable hit particles
        if (_envHitParticles != null)
            _envHitParticles.gameObject.SetActive(false);

        // Disable beam
        if (_beamLineRenderer != null)
            _beamLineRenderer.enabled = false;
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

        // Draw beam while weapon is firing
        if (_beamLineRenderer != null && _beamOrigin != null && _weaponLogic != null && _weaponLogic.IsCurrent)
        {
            // Check if attack button is held (third-person view)
            bool attackHeld = PersistentClient.Instance.inputManager.Player.Attack.IsPressed();

            if (attackHeld)
            {
                Vector3 beamStart = _beamOrigin.position;
                Vector3 beamEnd;

                // If hitting, draw beam to hit point, otherwise to max range
                if (_isHitting)
                {
                    beamEnd = _lastHitPosition;
                }
                else
                {
                    beamEnd = beamStart + _beamOrigin.forward * _beamMaxRange;
                }

                _beamLineRenderer.SetPosition(0, beamStart);
                _beamLineRenderer.SetPosition(1, beamEnd);

                if (!_beamLineRenderer.enabled)
                {
                    _beamLineRenderer.enabled = true;
                }
            }
            else
            {
                if (_beamLineRenderer.enabled)
                {
                    _beamLineRenderer.enabled = false;
                }
                _isHitting = false;
            }
        }
    }

    private void OnShoot(Vector3 fireDirection)
    {
        // Reset hit flag - will be set by OnHit if something is hit
        _isHitting = false;
    }

    private void OnHit(HitInfo hitInfo)
    {
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
            _isHitting = false;
            _envHitParticles.gameObject.SetActive(false);
        }
    }
}
