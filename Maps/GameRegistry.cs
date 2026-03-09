using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "GameRegistry", menuName = "Arkvain/Game Registry")]
public class GameRegistry : ScriptableObject
{
    [Serializable]
    public class GameModeConfig
    {
        public string modeName;
        public GameObject gameModeLogicPrefab;
        public List<MapDefinition> allowedMaps = new List<MapDefinition>();
    }

    [Header("Game Mode Configuration")]
    public List<GameModeConfig> modeConfigs = new List<GameModeConfig>();

    /// <summary>
    /// Searches all game modes to find a map definition by its internal name.
    /// Used by the MapLoader in the game scene.
    /// </summary>
    public MapDefinition FindMapByInternalName(string internalName)
    {
        if (string.IsNullOrEmpty(internalName)) return null;

        foreach (var config in modeConfigs)
        {
            var map = config.allowedMaps.FirstOrDefault(m => m.InternalName == internalName);
            if (map != null) return map;
        }

        return null;
    }

    /// <summary>
    /// Returns the configuration for a specific game mode by name.
    /// </summary>
    public GameModeConfig GetModeConfig(string modeName)
    {
        return modeConfigs.FirstOrDefault(m => m.modeName == modeName);
    }
}
