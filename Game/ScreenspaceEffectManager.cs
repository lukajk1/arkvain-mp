using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenspaceEffectManager : MonoBehaviour
{
    [SerializeField] private ScriptableRendererFeature _ssGrayscale;
    [SerializeField] private Material _ssDamageMaterial;
    [SerializeField] private Volume _postProcessVolume;
    [SerializeField] private float _deathBloomIntensity = 8f;
    [SerializeField] private float _deathBloomDuration = 0.5f;


    private float _thresholdToShowSSDamage = 0.4f;

    private Bloom _bloom;
    private float _defaultBloomIntensity;

    public static ScreenspaceEffectManager Instance { get; private set; }

    private PlayerHealth _localPlayerHealth;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }

        if (_ssDamageMaterial != null) _ssDamageMaterial.SetFloat("_vignette_darkening", 0f);

        if (_postProcessVolume != null && _postProcessVolume.profile.TryGet(out _bloom))
        {
            _defaultBloomIntensity = _bloom.intensity.value;
        }
    }

    private void OnEnable()
    {
        PlayerHealth.OnLocalPlayerHealthReady += OnLocalPlayerHealthReady;
    }

    private void OnDisable()
    {
        PlayerHealth.OnLocalPlayerHealthReady -= OnLocalPlayerHealthReady;

        // Unsubscribe from health changes
        if (_localPlayerHealth != null)
        {
            _localPlayerHealth.OnHealthChanged -= OnHealthChanged;
        }

        // Reset render features since ScriptableRendererFeature assets persist across play sessions
        if (_ssGrayscale != null) _ssGrayscale.SetActive(false);
        if (_ssDamageMaterial != null) _ssDamageMaterial.SetFloat("_vignette_darkening", 0f);
        if (_bloom != null) _bloom.intensity.value = _defaultBloomIntensity;
    }

    private void OnLocalPlayerHealthReady(PlayerHealth playerHealth)
    {
        _localPlayerHealth = playerHealth;

        // Subscribe to health changes
        _localPlayerHealth.OnHealthChanged += OnHealthChanged;
    }

    private void OnHealthChanged(int currentHealth, int maxHealth)
    {
        if (_ssDamageMaterial == null)
            return;

        float healthRatio = (float)currentHealth / maxHealth;

        if (healthRatio < _thresholdToShowSSDamage)
        {
            _ssDamageMaterial.SetFloat("_vignette_darkening", 0.3f);
        }
        else
        {
            _ssDamageMaterial.SetFloat("_vignette_darkening", 0f);
        }
    }
    public static void SetGrayscale(bool active)
    {
        if (Instance._ssGrayscale != null)
        {
            Instance._ssGrayscale.SetActive(active);
        }
    }

    public static void FlashBloom()
    {
        if (Instance._bloom == null) return;

        LeanTween.cancel(Instance.gameObject);
        Instance._bloom.intensity.value = Instance._deathBloomIntensity;
        LeanTween.value(Instance.gameObject, Instance._deathBloomIntensity, Instance._defaultBloomIntensity, Instance._deathBloomDuration)
            .setOnUpdate(val => Instance._bloom.intensity.value = val)
            .setEase(LeanTweenType.easeOutExpo);
    }
}