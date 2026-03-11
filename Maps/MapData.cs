using UnityEngine;

public class MapData : MonoBehaviour
{
    [Header("Team Spawn Points")]
    [Tooltip("Spawns for Team A (e.g., CT in TDM)")]
    public Transform[] teamASpawns;
    
    [Tooltip("Spawns for Team B (e.g., T in TDM)")]
    public Transform[] teamBSpawns;

    [Header("FFA Spawn Points")]
    [Tooltip("Spawns for Free-for-All or Deathmatch modes")]
    public Transform[] freeForAllSpawns;

    [Header("Environmental Settings")]
    [Tooltip("Optional: Material for the skybox when this map loads.")]
    public Material skyboxMaterial;

    [Tooltip("Optional: Ambient light intensity for this map.")]
    public float ambientIntensity = 1.0f;

    /// <summary>
    /// Gets a random spawn point for a specific team or FFA.
    /// </summary>
    public Transform GetRandomSpawnPoint(int teamIndex = -1)
    {
        Transform[] selectedGroup = null;

        if (teamIndex == 0) selectedGroup = teamASpawns;
        else if (teamIndex == 1) selectedGroup = teamBSpawns;

        // Fallback to FFA spawns if team-specific ones are missing
        if (selectedGroup == null || selectedGroup.Length == 0)
        {
            selectedGroup = freeForAllSpawns;
        }

        if (selectedGroup == null || selectedGroup.Length == 0)
        {
            Debug.LogWarning($"[MapData] No spawn points found for team {teamIndex} or FFA! Spawning at map root: {transform.position}");
            return transform;
        }

        Transform selected = selectedGroup[Random.Range(0, selectedGroup.Length)];
        Debug.Log($"[MapData] Selected spawn point: {selected.name} at {selected.position} (Team: {teamIndex})");
        return selected;
    }

    /// <summary>
    /// Gets a deterministic spawn point based on an index (useful for networking).
    /// </summary>
    public Transform GetSpawnPointSequential(int index, int teamIndex = -1)
    {
        Transform[] selectedGroup = null;

        if (teamIndex == 0) selectedGroup = teamASpawns;
        else if (teamIndex == 1) selectedGroup = teamBSpawns;

        if (selectedGroup == null || selectedGroup.Length == 0)
        {
            selectedGroup = freeForAllSpawns;
        }

        if (selectedGroup == null || selectedGroup.Length == 0)
        {
            Debug.LogWarning($"[MapData] No spawn points found for index {index} (Team: {teamIndex})! Spawning at map root.");
            return transform;
        }

        return selectedGroup[index % selectedGroup.Length];
    }
}
