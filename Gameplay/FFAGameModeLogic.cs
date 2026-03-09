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
        PlayerMatchData killerData = MatchSessionManager.Instance.GetPlayerData(killer);
        
        if (killerData != null)
        {
            Debug.Log($"[FFA] {killerData.PlayerName} kills: {killerData.Kills}/{killsToWin}");

            // Check if win condition is met
            if (killerData.Kills >= killsToWin)
            {
                Debug.Log($"[FFA] {killerData.PlayerName} has won the match!");
                EndMatch();
            }
        }
    }
}
