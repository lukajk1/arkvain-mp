using PurrNet;
using PurrNet.Logging;
using PurrNet.Steam;
using PurrNet.Transports;
using Steamworks;
using System.Collections;
using UnityEngine;
using Heathen.SteamworksIntegration;
#if UNITY_EDITOR
using ParrelSync;
#endif

public class ArkvainConnectionStarter : MonoBehaviour
{
    private SteamTransport _steamTransport;
    private UDPTransport _udpTransport;
    private NetworkManager _networkManager;

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

        // Check if we have lobby data
        if (ArkvainLobbyData.HasValidLobby())
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

        if (_isFromLobby && ArkvainLobbyData.HasValidLobby())
        {
            LobbyData lobby = ArkvainLobbyData.CurrentLobby;
            Debug.Log($"[ArkvainConnectionStarter] Starting from lobby: {lobby.Name}");
            Debug.Log($"[ArkvainConnectionStarter] Lobby Members ({lobby.MemberCount}/{lobby.MaxMembers}):");

            foreach (var member in lobby.Members)
            {
                string roleTag = member.IsOwner ? "[HOST]" : "[CLIENT]";
                Debug.Log($"  {roleTag} {member.user.Name} (ID: {member.user.SteamId})");
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
        if (!ClonesManager.IsClone())
            _networkManager.StartServer();
#else
        _networkManager.StartServer();
#endif
        _networkManager.StartClient();
    }

    private void StartFromLobby()
    {
        _networkManager.transport = _steamTransport;

        if (!ArkvainLobbyData.HasValidLobby())
        {
            PurrLogger.LogError($"Failed to start connection. Lobby is invalid!", this);
            return;
        }

        LobbyData lobby = ArkvainLobbyData.CurrentLobby;

        // Get the lobby owner's CSteamID
        CSteamID hostId = lobby.Owner.user.id;

        if (!hostId.IsValid())
        {
            PurrLogger.LogError($"Failed to get valid lobby owner CSteamID!", this);
            return;
        }

        // Set the Steam transport address to the host's Steam ID
        _steamTransport.address = hostId.ToString();
        Debug.Log($"[ArkvainConnectionStarter] Steam transport address set to: {hostId.ToString()}");

        // If we're the owner, start the server
        if (lobby.IsOwner)
        {
            Debug.Log("[ArkvainConnectionStarter] Starting as HOST (Server + Client)");
            _networkManager.StartServer();
        }
        else
        {
            Debug.Log("[ArkvainConnectionStarter] Starting as CLIENT");
        }

        // Start client (with delay to allow server to fully initialize)
        StartCoroutine(StartClient());
    }

    private IEnumerator StartClient()
    {
        yield return new WaitForSeconds(1f);
        _networkManager.StartClient();
    }
}
