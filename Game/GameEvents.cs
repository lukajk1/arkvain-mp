using System;
using PurrNet;

public static class GameEvents
{
    public static Action RespawnAllPlayers;
    public static Action<PlayerID> OnPlayerSpawned;
}
