using PurrNet;
using UnityEngine;

public abstract class BaseGameModeLogic : NetworkBehaviour
{
    public static BaseGameModeLogic Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] protected string modeDisplayName = "Game Mode";
    public string DisplayName => modeDisplayName;

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Called when the match transition from Waiting/Spawning to Running.
    /// </summary>
    public virtual void OnMatchStarted() 
    {
        Debug.Log($"[GameMode] {modeDisplayName} Started!");
    }

    /// <summary>
    /// Called by MatchSessionManager when a kill is confirmed.
    /// </summary>
    public abstract void OnPlayerKilled(PlayerID killer, PlayerID victim);

    /// <summary>
    /// Called when the match transition to the End state.
    /// </summary>
    public virtual void OnMatchEnded() 
    {
        Debug.Log($"[GameMode] {modeDisplayName} Ended!");
    }

    /// <summary>
    /// Requests the match to end, transitioning the global state machine.
    /// </summary>
    protected void EndMatch()
    {
        if (!isServer) return;
        
        Debug.Log($"[GameMode] {modeDisplayName} win condition met. Ending match...");
        
        // This will be linked to MatchSessionManager to trigger the state machine change
        MatchSessionManager.Instance.RequestEndMatch();
    }
}
