using Heathen.SteamworksIntegration;
using API = Heathen.SteamworksIntegration.API;
using UnityEngine;

// unity complains if this class doesn't inherit from monobehavior so it does even though it is not necessary
public class RichPresenceManager : MonoBehaviour
{
    public static void SetMenuStatus()
    {
        API.Friends.Client.ClearRichPresence();
        API.Friends.Client.SetRichPresence("steam_display", "#Status_Menu");
    }
    public static void ClearStatus()
    {
        API.Friends.Client.ClearRichPresence();
    }

    public static void SetLobbyStatus(string lobbyId, int maxMembers)
    {
        if (string.IsNullOrEmpty(lobbyId)) return;

        API.Friends.Client.SetRichPresence("steam_display", "#Status_Lobby");
        API.Friends.Client.SetRichPresence("steam_player_group", lobbyId);
        API.Friends.Client.SetRichPresence("steam_player_group_size", maxMembers.ToString());
    }

    public static void SetMatchStatus(string modeName, string lobbyId, int maxPlayers)
    {
        if (string.IsNullOrEmpty(lobbyId)) return;

        // Set the game_mode variable used in the #Status_InGame token
        API.Friends.Client.SetRichPresence("game_mode", modeName ?? "Unknown");

        // Update the display token
        API.Friends.Client.SetRichPresence("steam_display", "#Status_InGame");

        // Keep the group ID the same so they stay grouped from Lobby -> Game
        API.Friends.Client.SetRichPresence("steam_player_group", lobbyId);
        API.Friends.Client.SetRichPresence("steam_player_group_size", maxPlayers.ToString());

        // Optional: Enable the "Join Game" button for friends
        API.Friends.Client.SetRichPresence("connect", $"+connect_lobby {lobbyId}");
    }
}
