using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RenderFeatureToggler : MonoBehaviour
{
    [SerializeField] private ScriptableRendererFeature _ssGrayscale;

    public static RenderFeatureToggler Instance { get; private set; }

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
    }
    public static void ToggleFeature(bool active)
    {
        if (Instance._ssGrayscale != null)
        {
            Instance._ssGrayscale.SetActive(active);
        }
    }
}