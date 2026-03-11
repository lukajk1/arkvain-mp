using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple script to trigger the initial transition from the Bootstrap scene 
/// to the Main Menu using the persistent LoadingManager.
/// </summary>
public class BootstrapLoader : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private SceneNameHolder mainMenuScene;
    
    [Header("Settings")]
    [Tooltip("If true, the load will start immediately on Start. If false, you must call TriggerLoad() manually.")]
    [SerializeField] private bool loadOnStart = true;

    private void Start()
    {
        if (loadOnStart)
        {
            TriggerLoad();
        }
    }

    public void TriggerLoad()
    {
        if (mainMenuScene == null || string.IsNullOrEmpty(mainMenuScene.sceneName))
        {
            Debug.LogError("[BootstrapLoader] Main Menu scene reference is missing or empty!");
            return;
        }

        if (LoadingManager.Instance != null)
        {
            Debug.Log($"[BootstrapLoader] Initializing load for: {mainMenuScene.sceneName}");
            LoadingManager.Instance.LoadScene(mainMenuScene.sceneName);
        }
        else
        {
            Debug.LogWarning("[BootstrapLoader] LoadingManager.Instance not found! Falling back to direct scene load.");
            SceneManager.LoadScene(mainMenuScene.sceneName);
        }
    }
}
