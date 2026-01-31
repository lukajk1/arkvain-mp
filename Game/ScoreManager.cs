using PurrNet;
using System.Collections.Generic;

/// <summary>
/// Server-authoritative score tracking system.
/// Uses standard PurrNet (non-predicted) networking for reliable, synchronized game stats.
/// </summary>
public class ScoreManager : NetworkBehaviour
{
    // Server-authoritative score dictionary
    // Automatically syncs to all clients when changed on server
    public SyncVar<Dictionary<PlayerID, int>> kills = new SyncVar<Dictionary<PlayerID, int>>();
    public SyncVar<Dictionary<PlayerID, int>> deaths = new SyncVar<Dictionary<PlayerID, int>>();

    protected override void OnSpawned(bool isRetroactive)
    {
        base.OnSpawned(isRetroactive);

        // Initialize dictionaries on server
        if (isServer)
        {
            kills.value = new Dictionary<PlayerID, int>();
            deaths.value = new Dictionary<PlayerID, int>();
        }
    }

    /// <summary>
    /// Called by server to record a kill.
    /// </summary>
    [ServerRpc]
    public void RecordKill(PlayerID killer, PlayerID victim)
    {
        // Initialize if needed
        if (!kills.value.ContainsKey(killer))
            kills.value[killer] = 0;
        if (!deaths.value.ContainsKey(victim))
            deaths.value[victim] = 0;

        // Update scores
        kills.value[killer]++;
        deaths.value[victim]++;

        // Force sync to clients (required when modifying dictionary contents)
        kills.SetDirty();
        deaths.SetDirty();
    }

    /// <summary>
    /// Get kills for a specific player.
    /// </summary>
    public int GetKills(PlayerID player)
    {
        return kills.value.TryGetValue(player, out int count) ? count : 0;
    }

    /// <summary>
    /// Get deaths for a specific player.
    /// </summary>
    public int GetDeaths(PlayerID player)
    {
        return deaths.value.TryGetValue(player, out int count) ? count : 0;
    }

    /// <summary>
    /// Get K/D ratio for a specific player.
    /// </summary>
    public float GetKDRatio(PlayerID player)
    {
        int killCount = GetKills(player);
        int deathCount = GetDeaths(player);

        if (deathCount == 0)
            return killCount;

        return (float)killCount / deathCount;
    }

    /// <summary>
    /// Reset all scores (server only).
    /// </summary>
    [ServerRpc]
    public void ResetScores()
    {
        kills.value.Clear();
        deaths.value.Clear();
        kills.SetDirty();
        deaths.SetDirty();
    }
}
