using PurrNet;
using UnityEngine;
using System.Collections.Generic;

public class FFAGameModeLogic : BaseGameModeLogic
{
    [Header("FFA Rules")]
    [SerializeField] private int killsToWin = 10;
    
    // We can use the existing MatchSessionManager's data to track kills, 
    // but the Logic class should be the one to "decide" if that data means a win.
    
    protected override void Awake()
    {
        base.Awake();
        modeDisplayName = "Free For All";
    }

    public override void OnPlayerKilled(PlayerID killer, PlayerID victim)
    {
        if (!isServer) return;

        // Fetch current data for the killer
        var killerData = MatchSessionManager.Instance.GetPlayerData(killer);
        
        if (killerData.HasValue)
        {
            string killerName = MatchSessionManager.Instance.GetPlayerName(killer);
            Debug.Log($"[FFA] {killerName} kills: {killerData.Value.kills}/{killsToWin}");

            // Check if win condition is met
            if (killerData.Value.kills >= killsToWin)
            {
                Debug.Log($"[FFA] {killerName} has won the match!");
                EndMatch();
            }
        }
    }
}
