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
    [SerializeField] private GameObject _beamLinePrefab;
    [SerializeField] private float _beamMaxDistance = 300f;
    [SerializeField] private float _beamFadeTime = 0.7f;
    [SerializeField] private AudioClip _shootSound;

    [Header("Reload Effects")]
    [SerializeField] private AudioClip _reloadStartClip;
    [FormerlySerializedAs("_reloadSound")] [SerializeField] private AudioClip _reloadCompleteClip;
    [SerializeField] private AudioClip _passiveShotReadyClip;

    [Header("VFX Settings")]
    [SerializeField] private Material _chargeMaterial;

    private AudioSource _passiveHumObject;
    private HitInfo? _pendingHitInfo;

    protected override void OnEnable()
    {
        base.OnEnable();
    }

    private void Awake()
    {

    }


    /// <summary>
    /// Called when the railgun shoots. Plays muzzle flash and shoot sound.
    /// </summary>
    protected override void OnShoot()
    {
        base.OnShoot(); // Plays shoot animation

        // Clear any pending hit info - we'll wait for OnHit to set it
        _pendingHitInfo = null;

        if (_muzzleFlashParticles != null && !_muzzleFlashParticles.isPlaying)
        {
            // Ensure the particle system GameObject is active
            if (!_muzzleFlashParticles.gameObject.activeInHierarchy)
            {
                _muzzleFlashParticles.gameObject.SetActive(true);
            }

            // Clear and restart the particle system
            _muzzleFlashParticles.Clear();
            _muzzleFlashParticles.Play(true);

            // Force emit some particles as a fallback
            _muzzleFlashParticles.Emit(0);

            //Debug.Log($"[RailgunVisual] Muzzle flash - IsPlaying: {_muzzleFlashParticles.isPlaying}, ParticleCount: {_muzzleFlashParticles.particleCount}");
        }
        else
        {
            //Debug.LogWarning("[RailgunVisual] Muzzle flash particle system is null!");
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

        if (_chargeMaterial != null)
        {
            _chargeMaterial.SetFloat("_Alpha", 0f);
        }

        // Create beam immediately (will use hit info if available shortly after)
        StartCoroutine(CreateBeamAfterHit());
    }

    /// <summary>
    /// Called when a shot hits something. Plays appropriate particle and sound based on what was hit.
    /// </summary>
    protected override void OnHit(HitInfo hitInfo)
    {
        // Store hit info for beam creation
        _pendingHitInfo = hitInfo;

        // Play hit particles via centralized manager
        WeaponHitEffectsManager.PlayHitEffect(hitInfo, _weaponLogic.isOwner);
    }

    private System.Collections.IEnumerator CreateBeamAfterHit()
    {
        // Wait one frame to allow OnHit to fire and set _pendingHitInfo
        yield return null;

        if (_beamLinePrefab == null || _muzzleFlashParticles == null)
            yield break;

        Vector3 startPos = _muzzleFlashParticles.transform.position;
        Vector3 endPos;

        // Use hit position if we hit something, otherwise shoot forward at max distance
        if (_pendingHitInfo.HasValue)
        {
            endPos = _pendingHitInfo.Value.position;
        }
        else
        {
            // No hit - beam goes forward at max distance
            Vector3 forward = _muzzleFlashParticles.transform.forward;
            endPos = startPos + forward * _beamMaxDistance;
        }

        CreateBeam(startPos, endPos);
    }

    private void CreateBeam(Vector3 startPos, Vector3 endPos)
    {
        GameObject beamObj = Instantiate(_beamLinePrefab);
        LineRenderer lineRenderer = beamObj.GetComponent<LineRenderer>();

        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);

            // Store initial color with full alpha
            Color startColor = lineRenderer.startColor;
            Color endColor = lineRenderer.endColor;

            // Store initial width
            float initialStartWidth = lineRenderer.startWidth;
            float initialEndWidth = lineRenderer.endWidth;
            float targetStartWidth = initialStartWidth / 3f;
            float targetEndWidth = initialEndWidth / 3f;

            // Fade out the line renderer and shrink width over the beam duration
            LeanTween.value(beamObj, 1f, 0f, _beamFadeTime)
                .setOnUpdate((float t) =>
                {
                    if (lineRenderer != null)
                    {
                        // Lerp alpha
                        Color newStartColor = startColor;
                        newStartColor.a = t;
                        Color newEndColor = endColor;
                        newEndColor.a = t;

                        lineRenderer.startColor = newStartColor;
                        lineRenderer.endColor = newEndColor;

                        // Lerp width from initial to 1/3 size
                        lineRenderer.startWidth = Mathf.Lerp(targetStartWidth, initialStartWidth, t);
                        lineRenderer.endWidth = Mathf.Lerp(targetEndWidth, initialEndWidth, t);
                    }
                });
        }

        // Destroy beam after duration
        Destroy(beamObj, _beamFadeTime);
    }

    /// <summary>
    /// Called when the railgun starts reloading. Triggers reload animation.
    /// </summary>
    protected override void OnReload()
    {
        base.OnReload(); // Plays reload animation

        if (_chargeMaterial != null)
        {
            float delay = _weaponLogic.reloadSpeed - 1.25f;
            LeanTween.value(gameObject, 0f, 1f, 1.25f)
                .setDelay(delay)
                .setOnUpdate((float val) => {
                    _chargeMaterial.SetFloat("_Alpha", val);
                });
        }

        if (_reloadStartClip != null)
        {
            SoundManager.Play(new SoundData(_reloadStartClip));
        }
    }

    protected override void OnReloadComplete()
    {
        if (_reloadCompleteClip != null)
        {
            SoundManager.Play(new SoundData(_reloadCompleteClip));
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

        //Debug.Log("[RailgunVisual] Weapon equipped");
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

        //Debug.Log("[RailgunVisual] Weapon holstered");
    }
}
