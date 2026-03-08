using PurrNet;
using PurrNet.Prediction;
using Heathen.SteamworksIntegration;
using UnityEngine;

/// <summary>
/// Sends the local player's Steam identity to the server when they spawn.
/// Attach this to the player prefab.
/// </summary>
public class SyncSteamAndPlayerIdentity : StatelessPredictedIdentity
{
    protected override void LateAwake()
    {
        Debug.Log($"[PlayerSteamIdentity] LateAwake - isOwner: {isOwner}");

        // Only run on the owning client
        if (!isOwner)
            return;

        // Get local Steam info using Heathen (same as UserProfile)
        UserData localUser = UserData.Me;
        ulong localSteamId = (ulong)localUser.id;
        string localSteamName = localUser.Name;

        Debug.Log($"[PlayerSteamIdentity] Sending Steam info to server: {localSteamName} ({localSteamId})");

        // Get PlayerID before sending
        if (!owner.HasValue)
        {
            Debug.LogError("[PlayerSteamIdentity] Owner is null!");
            return;
        }

        PlayerID playerId = owner.Value;

        // Send to server with PlayerID included
        ReportSteamInfo(playerId, localSteamId, localSteamName);
    }

    [ServerRpc(requireOwnership: false)]
    private static void ReportSteamInfo(PlayerID playerId, ulong steamId, string steamName)
    {
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
