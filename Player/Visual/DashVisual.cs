using UnityEngine;

public class DashVisual : MonoBehaviour
{
    [SerializeField] private DashAbility _dashAbility;
    [SerializeField] private ParticleSystem _dashParticles;
    [SerializeField] private float _particleSystemPlaybackSpeed = 1.5f;

    [Header("Fresnel Animation")]
    [SerializeField] private Renderer[] _targetRenderers;
    [SerializeField] private float _fresnelAnimDuration = 0.5f;
    [SerializeField] private float _fresnelStartPower = 0.3f;
    [SerializeField] private float _fresnelEndPower = 30f;

    private bool _subscribed;
    private MaterialPropertyBlock _propertyBlock;
    private static readonly int FresnelPowerId = Shader.PropertyToID("_FresnelPower");
    private static readonly int FresnelEnabledId = Shader.PropertyToID("_FresnelEnabled");
    private int _fresnelTweenId = -1;
    private float _currentFresnelPower;

    private void Awake()
    {
        if (_dashParticles != null)
        {
            _dashParticles = Instantiate(_dashParticles);
            _dashParticles.transform.SetParent(transform.root, worldPositionStays: true);
            _dashParticles.Stop();
            _dashParticles.Clear();
            var main = _dashParticles.main;
            main.simulationSpeed = _particleSystemPlaybackSpeed;

            VFXPoolManager.Instance.RegisterPrefab(_dashParticles.gameObject);
        }

        _propertyBlock = new MaterialPropertyBlock();
        _currentFresnelPower = _fresnelStartPower;
    }

    private void Start()
    {
        // Disable Fresnel on all renderers initially
        EnableFresnel(false);
    }

    /// <summary>
    /// Enables or disables the Fresnel effect via shader keyword.
    ///
    /// NOTE: This uses renderer.material which creates material instances (overhead).
    /// Shader keywords cannot be controlled via MaterialPropertyBlock, so we must
    /// modify the material directly. This creates one material instance per renderer
    /// on first access, which incurs a small memory/performance cost but is necessary
    /// for per-instance keyword control.
    /// </summary>
    private void EnableFresnel(bool enabled)
    {
        if (_targetRenderers == null) return;

        foreach (var renderer in _targetRenderers)
        {
            if (renderer == null) continue;

            // Shader keywords can't be set via MaterialPropertyBlock
            // Must modify the material itself (creates instance on first access)
            if (enabled)
            {
                renderer.material.EnableKeyword("_FRESNEL_ON");
            }
            else
            {
                renderer.material.DisableKeyword("_FRESNEL_ON");
            }

            Debug.Log($"[DashVisual] EnableFresnel({enabled}) - {(enabled ? "Enabled" : "Disabled")} _FRESNEL_ON keyword");
        }
    }

    private void Update()
    {
        if (!_subscribed && _dashAbility != null && _dashAbility._onDash != null)
        {
            _dashAbility._onDash.AddListener(OnDash);
            _subscribed = true;
        }
    }

    private void OnDisable()
    {
        if (_subscribed && _dashAbility != null && _dashAbility._onDash != null)
        {
            _dashAbility._onDash.RemoveListener(OnDash);
            _subscribed = false;
        }

        // Cancel Fresnel animation
        if (_fresnelTweenId != -1)
        {
            LeanTween.cancel(_fresnelTweenId);
            _fresnelTweenId = -1;
        }
    }

    private void OnDash(Vector3 origin)
    {
        if (_dashParticles == null || VFXPoolManager.Instance == null) return;
        VFXPoolManager.Instance.Spawn(_dashParticles.gameObject, origin, Quaternion.identity);

        // Only animate Fresnel for remote players (not local player)
        if (_dashAbility != null && _dashAbility.isOwner) return;

        // Animate Fresnel with LeanTween
        if (_targetRenderers != null && _targetRenderers.Length > 0)
        {
            // Cancel previous animation
            if (_fresnelTweenId != -1)
                LeanTween.cancel(_fresnelTweenId);

            // Enable Fresnel and set to min power instantly
            EnableFresnel(true);
            UpdateFresnelPower(_fresnelStartPower);

            // Animate from min to max - starts slow, accelerates
            LTDescr tween = LeanTween.value(gameObject, UpdateFresnelPower, _fresnelStartPower, _fresnelEndPower, _fresnelAnimDuration)
                .setEaseInQuad()
                .setOnComplete(() =>
                {
                    Debug.Log("[DashVisual] Fresnel animation complete callback fired");
                    if (this != null)
                    {
                        EnableFresnel(false); // Disable when done
                        _fresnelTweenId = -1;
                        Debug.Log("[DashVisual] Fresnel disabled");
                    }
                });

            _fresnelTweenId = tween.id;
        }
    }

    private void UpdateFresnelPower(float value)
    {
        // Safety check in case object is destroyed mid-animation
        if (this == null || _targetRenderers == null || _propertyBlock == null)
            return;

        _currentFresnelPower = value;

        // Update all renderers
        foreach (var renderer in _targetRenderers)
        {
            if (renderer == null) continue;

            renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(FresnelPowerId, _currentFresnelPower);
            renderer.SetPropertyBlock(_propertyBlock);
        }
    }

    private void OnDestroy()
    {
        // Cancel Fresnel animation on destroy
        if (_fresnelTweenId != -1)
        {
            LeanTween.cancel(_fresnelTweenId);
            _fresnelTweenId = -1;
        }
    }
}
