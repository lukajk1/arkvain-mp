using PurrNet;
using Steamworks;
using UnityEngine;

/// <summary>
/// Sends the local player's Steam identity to the server when they spawn.
/// Attach this to the player prefab.
/// </summary>
public class PlayerSteamIdentity : NetworkBehaviour
{
    private void Start()
    {
        // Only run on the owning client
        if (!isOwner)
            return;

        // Get local Steam info
        CSteamID localSteamId = SteamUser.GetSteamID();
        string localSteamName = SteamFriends.GetPersonaName();

        Debug.Log($"[PlayerSteamIdentity] Sending Steam info to server: {localSteamName} ({localSteamId})");

        // Send to server
        ReportSteamInfo((ulong)localSteamId, localSteamName);
    }

    [ServerRpc(requireOwnership: false)]
    private void ReportSteamInfo(ulong steamId, string steamName)
    {
        if (!isServer)
            return;

        // Get the PlayerID from the owner of this NetworkBehaviour
        if (!owner.HasValue)
        {
            Debug.LogError("[PlayerSteamIdentity] Owner is null!");
            return;
        }

        PlayerID playerId = owner.Value;

        Debug.Log($"[PlayerSteamIdentity] Server received Steam info for PlayerID {playerId}: {steamName} ({steamId})");

        // Update MatchSessionManager with Steam info
        if (MatchSessionManager.Instance != null)
        {
            MatchSessionManager.Instance.UpdatePlayerSteamInfo(playerId, steamId, steamName);
        }
        else
        {
            Debug.LogError("[PlayerSteamIdentity] MatchSessionManager.Instance is null!");
        }
    }
}
