using Heathen.SteamworksIntegration;
using API = Heathen.SteamworksIntegration.API;
using UnityEngine;

public class SteamRichPresenceManager : MonoBehaviour
{
    public void SetMenuStatus()
    {
        API.Friends.Client.ClearRichPresence();
        API.Friends.Client.SetRichPresence("steam_display", "#Status_Menu");
    }

    public void SetLobbyStatus(string lobbyId)
    {
        API.Friends.Client.SetRichPresence("steam_display", "#Status_Lobby");
        API.Friends.Client.SetRichPresence("steam_player_group", lobbyId);
    }

    public void SetMatchStatus(string ownerName, string modeName, string lobbyId)
    {
        API.Friends.Client.SetRichPresence("game_mode", modeName);

        // Update the display token
        API.Friends.Client.SetRichPresence("steam_display", "#Status_InGame");

        // Keep the group ID the same so they stay grouped from Lobby -> Game
        API.Friends.Client.SetRichPresence("steam_player_group", lobbyId);

        // Optional: Enable the "Join Game" button for friends
        API.Friends.Client.SetRichPresence("connect", $"+connect_lobby {lobbyId}");
    }
}
