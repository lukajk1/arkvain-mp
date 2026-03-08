using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "MapRegistry", menuName = "Arkvain/Map Registry")]
public class MapRegistry : ScriptableObject
{
    [SerializeField] private List<MapDefinition> maps = new List<MapDefinition>();

    public List<MapDefinition> AllMaps => maps;

    /// <summary>
    /// Finds a MapDefinition by its internal Steam name (e.g., "Dust2").
    /// </summary>
    public MapDefinition GetMapByInternalName(string internalName)
    {
        return maps.FirstOrDefault(m => m.InternalName == internalName);
    }

    /// <summary>
    /// Finds a MapDefinition by its scene name string.
    /// </summary>
    public MapDefinition GetMapBySceneName(string sceneName)
    {
        return maps.FirstOrDefault(m => m.SceneName == sceneName);
    }

    /// <summary>
    /// Gets a list of maps that support a specific game mode.
    /// </summary>
    public List<MapDefinition> GetMapsForGameMode(string gameMode)
    {
        // Add more logic here as game modes become more complex
        return maps; 
    }
}
