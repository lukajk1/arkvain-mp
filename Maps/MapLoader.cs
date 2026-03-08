using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapLoader : MonoBehaviour
{
    public static MapLoader Instance { get; private set; }

    [Header("Registry")]
    [SerializeField] private GameRegistry gameRegistry;

    [Header("Settings")]
    [SerializeField] private bool loadOnStart = true;

    public event Action<MapData> OnMapLoaded;
    public MapData CurrentMapData { get; private set; }
    public bool IsLoading { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (loadOnStart)
        {
            LoadMapFromLobby();
        }
    }

    public void LoadMapFromLobby()
    {
        if (!ArkvainLobbyData.HasValidLobby())
        {
            Debug.LogWarning("[MapLoader] No valid lobby found in ArkvainLobbyData! Cannot load map.");
            return;
        }

        string mapInternalName = ArkvainLobbyData.CurrentLobby["map"];
        
        if (string.IsNullOrEmpty(mapInternalName))
        {
            Debug.LogWarning("[MapLoader] No map name found in lobby metadata!");
            return;
        }

        LoadMap(mapInternalName);
    }

    public void LoadMap(string internalName)
    {
        if (gameRegistry == null)
        {
            Debug.LogError("[MapLoader] GameRegistry is not assigned!");
            return;
        }

        MapDefinition def = gameRegistry.FindMapByInternalName(internalName);

        if (def == null)
            return;

        if (IsLoading)
        {
            Debug.LogWarning("[MapLoader] Already loading a map!");
            return;
        }

        StartCoroutine(LoadMapRoutine(def));
    }

    private IEnumerator LoadMapRoutine(MapDefinition def)
    {
        IsLoading = true;
        Debug.Log($"[MapLoader] Starting additive load of map: {def.displayName} ({def.SceneName})");

        // If a map is already loaded, unload it first
        if (CurrentMapData != null)
        {
            Scene currentScene = CurrentMapData.gameObject.scene;
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(currentScene);
            while (!unloadOp.isDone) yield return null;
            CurrentMapData = null;
        }

        // Load the new map scene additively
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(def.SceneName, LoadSceneMode.Additive);
        
        while (!loadOp.isDone)
        {
            yield return null;
        }

        // Find MapData in the newly loaded scene
        Scene loadedScene = SceneManager.GetSceneByName(def.SceneName);
        if (loadedScene.IsValid())
        {
            // Set as active scene so lighting/skybox from the map scene can take effect if configured
            // SceneManager.SetActiveScene(loadedScene);

            GameObject[] rootObjects = loadedScene.GetRootGameObjects();
            foreach (var obj in rootObjects)
            {
                MapData data = obj.GetComponentInChildren<MapData>();
                if (data != null)
                {
                    CurrentMapData = data;
                    break;
                }
            }
        }

        if (CurrentMapData == null)
        {
            Debug.LogError($"[MapLoader] Could not find MapData component in scene: {def.SceneName}");
        }
        else
        {
            Debug.Log($"[MapLoader] Map '{def.displayName}' loaded successfully.");
            OnMapLoaded?.Invoke(CurrentMapData);
        }

        IsLoading = false;
    }
}
