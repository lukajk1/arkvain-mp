using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using QFSW.QC;

public class ScreenspaceEffectManager : MonoBehaviour
{
    [SerializeField] private ScriptableRendererFeature _ssGrayscale;
    [SerializeField] private Material _ssDamageMaterial;
    [SerializeField] private Volume _postProcessVolume;
    [SerializeField] private float _deathBloomIntensity = 8f;
    [SerializeField] private float _deathBloomDuration = 0.5f;

    [Header("Screen Damage Flash")]
    [SerializeField] private float _damageFlashPeak = 0.3f;
    [SerializeField] private float _damageFlashPeakLowHealth = 0.45f;
    [SerializeField] private float _damageBaselineLowHealth = 0.3f;
    [SerializeField] private float _damageFlashDuration = 0.3f;


    private Bloom _bloom;
    private float _defaultBloomIntensity;

    public static ScreenspaceEffectManager Instance { get; private set; }

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
    private void Start()
    {
        QuantumRegistry.RegisterObject<MonoBehaviour>(this);

    }

    private void OnDisable()
    {
        // Reset render features since ScriptableRendererFeature assets persist across play sessions
        if (_ssGrayscale != null) _ssGrayscale.SetActive(false);
        if (_ssDamageMaterial != null) _ssDamageMaterial.SetFloat("_vignette_darkening", 0f);
        if (_bloom != null) _bloom.intensity.value = _defaultBloomIntensity;
    }

    [Command("set-screendamage")]
    public static void SetScreenDamage(float value)
    {
        if (Instance._ssDamageMaterial == null)
            return;

        Instance._ssDamageMaterial.SetFloat("_vignette_darkening", value);
    }

    [Command("set-grayscale")]
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

    public static void FlashScreenDamage(bool lowHealth)
    {
        if (Instance._ssDamageMaterial == null) return;

        float baseline = lowHealth ? Instance._damageBaselineLowHealth : 0f;
        float peak = lowHealth ? Instance._damageFlashPeakLowHealth : Instance._damageFlashPeak;

        LeanTween.cancel(Instance.gameObject, false);
        Instance._ssDamageMaterial.SetFloat("_vignette_darkening", peak);
        LeanTween.value(Instance.gameObject, peak, baseline, Instance._damageFlashDuration)
            .setOnUpdate(val => Instance._ssDamageMaterial.SetFloat("_vignette_darkening", val))
            .setEase(LeanTweenType.easeOutCubic);
    }
}