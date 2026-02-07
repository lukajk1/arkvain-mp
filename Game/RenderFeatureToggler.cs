using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RenderFeatureToggler : MonoBehaviour
{
    [SerializeField] private ScriptableRendererFeature _ssGrayscale;
    [SerializeField] private Material _ssDamageMaterial;
    private float _thresholdToShowSSamage = 0.4f;

    public static RenderFeatureToggler Instance { get; private set; }

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

        if (healthRatio < _thresholdToShowSSamage)
        {
            _ssDamageMaterial.SetFloat("_vignette_darkening", 0.3f);
        }
        else
        {
            _ssDamageMaterial.SetFloat("_vignette_darkening", 0f);
        }
    }
    public static void ToggleFeature(bool active)
    {
        if (Instance._ssGrayscale != null)
        {
            Instance._ssGrayscale.SetActive(active);
        }
    }
}