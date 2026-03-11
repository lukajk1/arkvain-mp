using PurrNet;
using System;
using UnityEngine;

public enum PlayerStatus
{
    Alive,
    Dead,
    Spectating
}

[Serializable]
public class PlayerMatchData
{
    public PlayerID PlayerId { get; set; }
    public ulong SteamId { get; set; }
    public string PlayerName { get; set; }

    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public int DamageDealt { get; set; }

    public float AveragePing { get; set; }
    public bool IsConnected { get; set; }

    public LoadoutSelection Loadout { get; set; }
    public PlayerStatus Status { get; set; }

    public PlayerMatchData() { }

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
        Loadout = new LoadoutSelection { Hero = HeroType.Richter, Weapon = WeaponType.Crossbow };
        Status = PlayerStatus.Spectating;
    }

    // Stat modification methods
    public void AddKill() => Kills++;
    public void AddDeath() => Deaths++;
    public void AddAssist() => Assists++;
    public void AddDamageDealt(int damage) => DamageDealt += damage;

    public void SetConnected(bool connected) => IsConnected = connected;

    public void UpdateLoadout(LoadoutSelection newLoadout)
    {
        Loadout = newLoadout;
    }

    public void UpdateStatus(PlayerStatus newStatus)
    {
        Status = newStatus;
    }

    // Update Steam info after creation
    public void UpdateSteamInfo(ulong steamId, string steamName)
    {
        SteamId = steamId;
        PlayerName = steamName;
    }

    public void UpdatePing(float newPing)
    {
        // Simplified ping update for synced data
        if (AveragePing == 0) AveragePing = newPing;
        else AveragePing = Mathf.Lerp(AveragePing, newPing, 0.2f);
    }

    // Score calculation for leaderboard sorting
    public float CalculateScore()
    {
        return (Kills * 10f) + (Assists * 3f) - (Deaths * 5f);
    }

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
