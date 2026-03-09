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
        string gameModeName = ArkvainLobbyData.CurrentLobby["game_mode"];
        
        if (string.IsNullOrEmpty(mapInternalName))
        {
            Debug.LogWarning("[MapLoader] No map name found in lobby metadata!");
            return;
        }

        LoadMapAndMode(mapInternalName, gameModeName);
    }

    public void LoadMapAndMode(string mapInternalName, string gameModeName)
    {
        if (gameRegistry == null)
        {
            Debug.LogError("[MapLoader] GameRegistry is not assigned!");
            return;
        }

        MapDefinition mapDef = gameRegistry.FindMapByInternalName(mapInternalName);
        var modeConfig = gameRegistry.GetModeConfig(gameModeName);

        if (mapDef == null)
        {
            Debug.LogError($"[MapLoader] Map '{mapInternalName}' not found in registry!");
            return;
        }

        if (IsLoading)
        {
            Debug.LogWarning("[MapLoader] Already loading!");
            return;
        }

        StartCoroutine(LoadMapAndModeRoutine(mapDef, modeConfig));
    }

    private IEnumerator LoadMapAndModeRoutine(MapDefinition mapDef, GameRegistry.GameModeConfig modeConfig)
    {
        IsLoading = true;
        Debug.Log($"[MapLoader] Starting load of map: {mapDef.displayName} and mode: {modeConfig?.modeName ?? "None"}");

        // 1. Unload old map if exists
        if (CurrentMapData != null)
        {
            Scene currentScene = CurrentMapData.gameObject.scene;
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(currentScene);
            while (!unloadOp.isDone) yield return null;
            CurrentMapData = null;
        }

        // 2. Load the new map scene additively
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(mapDef.SceneName, LoadSceneMode.Additive);
        while (!loadOp.isDone) yield return null;

        // 3. Find MapData
        Scene loadedScene = SceneManager.GetSceneByName(mapDef.SceneName);
        if (loadedScene.IsValid())
        {
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

        // 4. Spawn Game Mode Logic (Server Only)
        if (PurrNet.NetworkManager.main != null && PurrNet.NetworkManager.main.isServer)
        {
            if (modeConfig != null && modeConfig.gameModeLogicPrefab != null)
            {
                Debug.Log($"[MapLoader] Spawning Game Mode Logic: {modeConfig.modeName}");
                
                // Find the PredictionManager in the scene
                var predManager = FindObjectOfType<PurrNet.Prediction.PredictionManager>();
                if (predManager != null && predManager.hierarchy != null)
                {
                    predManager.hierarchy.Create(modeConfig.gameModeLogicPrefab);
                }
                else
                {
                    Debug.LogWarning("[MapLoader] PredictionManager or Hierarchy not found! Game mode logic may not be predicted.");
                    // Fallback to standard spawn if prediction isn't available
                    if (PurrNet.NetworkManager.main.TryGetModule<PurrNet.Modules.HierarchyFactory>(true, out var factory))
                    {
                        if (factory.TryGetHierarchy(gameObject.scene, out var hierarchy))
                        {
                            // In HierarchyV2, the method is InternalSpawn for GameObjects
                            hierarchy.OnGameObjectCreated(Instantiate(modeConfig.gameModeLogicPrefab), modeConfig.gameModeLogicPrefab);
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("[MapLoader] No GameModeLogic prefab found for this mode!");
            }
        }

        if (CurrentMapData == null)
        {
            Debug.LogError($"[MapLoader] Could not find MapData component in scene: {mapDef.SceneName}");
        }
        else
        {
            Debug.Log($"[MapLoader] Map and Mode loaded successfully.");
            OnMapLoaded?.Invoke(CurrentMapData);
        }

        IsLoading = false;
    }

    public void LoadMap(string internalName)
    {
        // Keep for backward compatibility or direct calls
        string gameModeName = ArkvainLobbyData.HasValidLobby() ? ArkvainLobbyData.CurrentLobby["game_mode"] : "";
        LoadMapAndMode(internalName, gameModeName);
    }
}
