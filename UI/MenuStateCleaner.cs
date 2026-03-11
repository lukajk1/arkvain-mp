using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures that persistent UI menus (Escape, Loadout, Settings) are hidden when loading a new scene.
/// </summary>
public class MenuStateCleaner : MonoBehaviour
{
    private void Awake()
    {
        // Only one cleaner needed, but it should persist
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[MenuStateCleaner] Scene loaded: {scene.name}. Cleaning up menu states...");

        // Hide Escape Menu
        if (EscapeMenu.Instance != null)
        {
            EscapeMenu.Instance.SetState(false);
        }

        // Hide Loadout Manager
        if (LoadoutManager.Instance != null)
        {
            LoadoutManager.Instance.SetState(false);
        }
        
        // Reset Cursor just in case
        if (PersistentClient.Instance != null)
        {
            PersistentClient.Instance.SetCursorToPlayMode(false);
        }
    }
}
