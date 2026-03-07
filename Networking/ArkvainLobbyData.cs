using UnityEngine;
using Heathen.SteamworksIntegration;

public static class ArkvainLobbyData
{
    public static LobbyData CurrentLobby { get; private set; }

    public static void SetLobby(LobbyData lobby)
    {
        CurrentLobby = lobby;
        Debug.Log($"[ArkvainLobbyData] Lobby set: {lobby.Name} (Owner: {lobby.Owner.user.Name})");
    }

    public static bool HasValidLobby()
    {
        return CurrentLobby.IsValid;
    }

    public static void Clear()
    {
        CurrentLobby = default;
        Debug.Log("[ArkvainLobbyData] Lobby cleared");
    }
}
