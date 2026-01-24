using PurrLobby;
using PurrNet;
using PurrNet.Logging;
using PurrNet.Steam;
using PurrNet.Transports;
using Steamworks;
using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using ParrelSync;
#endif

public class CustomConnectionStarter : MonoBehaviour
{
    private SteamTransport _steamTransport;
    private UDPTransport _udpTransport;
    private NetworkManager _networkManager;
    private LobbyDataHolder _lobbyDataHolder;

    private bool _isFromLobby;

    private void Awake()
    {
        if (!TryGetComponent(out _networkManager))
        {
            PurrLogger.LogError($"Failed to get {nameof(NetworkManager)} component.", this);
        }

        if (!TryGetComponent(out _steamTransport))
        {
            PurrLogger.LogError($"Failed to get {nameof(SteamTransport)} component.", this);
        }

        if (!TryGetComponent(out _udpTransport))
        {
            PurrLogger.LogError($"Failed to get {nameof(UDPTransport)} component.", this);
        }

        // if we have lobby data, start as a lobby. else start as a local thingy
        _lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
        if (_lobbyDataHolder)
        {
            _isFromLobby = true;
        }

    }

    private void Start()
    {
        if (!_networkManager)
        {
            PurrLogger.LogError($"Failed to start connection. {nameof(NetworkManager)} is null!", this);
            return;
        }
        if (!_steamTransport)
        {
            PurrLogger.LogError($"Failed to start connection. {nameof(SteamTransport)} is null!", this);
            return;
        }

        if (_isFromLobby && _lobbyDataHolder != null && _lobbyDataHolder.CurrentLobby.IsValid)
        {
            Debug.Log($"[CONNECTION STARTER] Lobby Members ({_lobbyDataHolder.CurrentLobby.Members.Count}):");
            foreach (var member in _lobbyDataHolder.CurrentLobby.Members)
            {
                Debug.Log($"  - DisplayName: {member.DisplayName}, ID: {member.Id}, Ready: {member.IsReady}");
            }
        }

        if (_isFromLobby)
        {
            StartFromLobby();
        }
        else
        {
            StartNormal();
        }

    }

    private void StartNormal()
    {
        _networkManager.transport = _udpTransport;

#if UNITY_EDITOR
        if (!ParrelSync.ClonesManager.IsClone())
            _networkManager.StartServer();
#else
        _networkManager.StartServer();
#endif
        _networkManager.StartClient();

    }

    private void StartFromLobby()
    {
        _networkManager.transport = _steamTransport;
        if (!_lobbyDataHolder)
        {
            PurrLogger.LogError($"Failed to start connection. {nameof(LobbyDataHolder)} is null!", this);
            return;
        }

        if (!_lobbyDataHolder.CurrentLobby.IsValid)
        {
            PurrLogger.LogError($"Failed to start connection. Lobby is invalid!", this);
            return;
        }

        //if (_networkManager.transport is PurrTransport)
        //{
        //    (_networkManager.transport as PurrTransport).roomName = _lobbyDataHolder.CurrentLobby.LobbyId;
        //}

#if UTP_LOBBYRELAY
            else if(_networkManager.transport is UTPTransport) {
                if(_lobbyDataHolder.CurrentLobby.IsOwner) {
                    (_networkManager.transport as UTPTransport).InitializeRelayServer((Allocation)_lobbyDataHolder.CurrentLobby.ServerObject);
                }
                (_networkManager.transport as UTPTransport).InitializeRelayClient(_lobbyDataHolder.CurrentLobby.Properties["JoinCode"]);
            }
#else
        //P2P Connection, receive IP/Port from server
#endif

        if (!ulong.TryParse(_lobbyDataHolder.CurrentLobby.LobbyId, out ulong ulongId))
        {
            Debug.Log($"Failed to parse lobbyid into ulong", this);
            return;
        }
        var lobbyOwner = SteamMatchmaking.GetLobbyOwner(new CSteamID(ulongId));
        if (!lobbyOwner.IsValid())
        {
            Debug.Log($"Failed to get lobby owner from parsed lobby ID", this);
            return;
        }
        _steamTransport.address = lobbyOwner.ToString();

        if (_lobbyDataHolder.CurrentLobby.IsOwner)
            _networkManager.StartServer();
        StartCoroutine(StartClient());
    }

    private IEnumerator StartClient()
    {
        yield return new WaitForSeconds(1f);
        _networkManager.StartClient();
    }
}
