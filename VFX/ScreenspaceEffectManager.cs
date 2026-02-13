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
    public static void SetScreenDamage(bool value)
    {
        if (Instance._ssDamageMaterial == null)
            return;

        if (value)
        {
            Instance._ssDamageMaterial.SetFloat("_vignette_darkening", 0.3f);

        }
        else
        {
            Instance._ssDamageMaterial.SetFloat("_vignette_darkening", 0f);

        }
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
}