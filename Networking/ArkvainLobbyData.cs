using UnityEngine;
using Heathen.SteamworksIntegration;

public class ArkvainLobbyData : MonoBehaviour
{
    private static ArkvainLobbyData _instance;
    public static ArkvainLobbyData Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ArkvainLobbyData>();
            }
            return _instance;
        }
    }

    public LobbyData CurrentLobby { get; private set; }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetLobby(LobbyData lobby)
    {
        CurrentLobby = lobby;
        Debug.Log($"[ArkvainLobbyData] Lobby set: {lobby.Name} (Owner: {lobby.Owner.user.Name})");
    }

    public bool HasValidLobby()
    {
        return CurrentLobby.IsValid;
    }

    public void ClearLobby()
    {
        CurrentLobby = default;
        Debug.Log("[ArkvainLobbyData] Lobby cleared");
    }
}
