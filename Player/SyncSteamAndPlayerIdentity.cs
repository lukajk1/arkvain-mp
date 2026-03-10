using PurrNet;
using PurrNet.Prediction;
using Heathen.SteamworksIntegration;
using UnityEngine;
using Steamworks;

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

        // Get PlayerID before sending
        if (!owner.HasValue)
        {
            Debug.LogError("[PlayerSteamIdentity] Owner is null!");
            return;
        }

        PlayerID playerId = owner.Value;
        ulong localSteamId = 0;
        string localSteamName = $"Player {playerId.id}";

        // Check if Steam is actually initialized before using Heathen/Steamworks
        if (SteamAPI.IsSteamRunning())
        {
            try 
            {
                // Get local Steam info using Heathen (same as UserProfile)
                UserData localUser = UserData.Me;
                localSteamId = (ulong)localUser.id;
                localSteamName = localUser.Name;
                Debug.Log($"[PlayerSteamIdentity] Steam active. Using: {localSteamName} ({localSteamId})");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PlayerSteamIdentity] Failed to get Steam info despite API running: {e.Message}. Using fallback.");
            }
        }
        else
        {
            Debug.Log($"[PlayerSteamIdentity] Steam NOT running. Using fallback: {localSteamName}");
        }

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
