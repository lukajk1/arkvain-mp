using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RenderFeatureToggler : MonoBehaviour
{
    [SerializeField] private ScriptableRendererFeature _targetFeature;

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
        if (Instance._targetFeature != null)
        {
            Instance._targetFeature.SetActive(active);
        }
    }
}