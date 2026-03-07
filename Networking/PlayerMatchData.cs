using PurrNet;
using System;

[Serializable]
public class PlayerMatchData
{
    public PlayerID PlayerId { get; private set; }
    public ulong SteamId { get; private set; }
    public string PlayerName { get; private set; }

    public int Kills { get; private set; }
    public int Deaths { get; private set; }
    public int Assists { get; private set; }
    public int DamageDealt { get; private set; }

    public float AveragePing { get; private set; }
    public bool IsConnected { get; private set; }

    // Ping tracking for averaging
    private readonly float[] _pingHistory;
    private int _pingHistoryIndex;
    private const int PING_HISTORY_SIZE = 10;

    public PlayerMatchData(PlayerID playerId, ulong steamId, string playerName)
    {
        PlayerId = playerId;
        SteamId = steamId;
        PlayerName = playerName;

        Kills = 0;
        Deaths = 0;
        Assists = 0;
        AveragePing = 0f;
        IsConnected = true;

        _pingHistory = new float[PING_HISTORY_SIZE];
        _pingHistoryIndex = 0;
    }

    // Stat modification methods (server only)
    public void AddKill() => Kills++;
    public void AddDeath() => Deaths++;
    public void AddAssist() => Assists++;
    public void AddDamageDealt(int damage) => DamageDealt += damage;

    public void SetConnected(bool connected) => IsConnected = connected;

    // Update Steam info after creation
    public void UpdateSteamInfo(ulong steamId, string steamName)
    {
        SteamId = steamId;
        PlayerName = steamName;
    }

    // Ping tracking with rolling average
    public void UpdatePing(float newPing)
    {
        _pingHistory[_pingHistoryIndex] = newPing;
        _pingHistoryIndex = (_pingHistoryIndex + 1) % PING_HISTORY_SIZE;

        // Calculate average from non-zero values
        float sum = 0f;
        int count = 0;

        foreach (float ping in _pingHistory)
        {
            if (ping > 0f)
            {
                sum += ping;
                count++;
            }
        }

        AveragePing = count > 0 ? sum / count : 0f;
    }

    // Score calculation for leaderboard sorting
    public float CalculateScore()
    {
        // Basic score: Kills worth 10, Deaths worth -5, Assists worth 3
        // You can adjust these weights as needed
        return (Kills * 10f) + (Assists * 3f) - (Deaths * 5f);
    }

    // KDA ratio for display
    public float GetKDA()
    {
        if (Deaths == 0)
            return Kills + Assists;

        return (Kills + Assists) / (float)Deaths;
    }

    public override string ToString()
    {
        return $"{PlayerName} - K:{Kills} D:{Deaths} A:{Assists} Ping:{AveragePing:F0}ms Connected:{IsConnected}";
    }
}
