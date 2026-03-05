using Heathen.SteamworksIntegration;
using Steamworks;
using UnityEngine;
using System;
using API = Heathen.SteamworksIntegration.API;

public class LobbyManager : MonoBehaviour
{
    public event Action<LobbyData> OnPartyCreated;
    public event Action<LobbyData> OnPartyJoined;
    public event Action OnPartyLeft;
    public event Action<string> OnPartyCreationFailed;

    private LobbyData _currentParty;

    public void CreateParty(int maxMembers = 8)
    {
        // Create an invisible party lobby (for friends only)
        API.Matchmaking.Client.CreateLobby(ELobbyType.k_ELobbyTypeInvisible, SteamLobbyModeType.Party, maxMembers, HandlePartyCreate);
    }

    private void HandlePartyCreate(EResult result, LobbyData lobby, bool ioError)
    {
        if (ioError || result != EResult.k_EResultOK)
        {
            Debug.LogError($"Party creation failed. Result: {result}, IOError: {ioError}");
            OnPartyCreationFailed?.Invoke($"Failed to create party: {result}");
            return;
        }

        _currentParty = lobby;

        // Set party metadata
        lobby["game_mode"] = "TDM"; // Custom metadata

        Debug.Log($"Party created! ID: {lobby.SteamId}");

        OnPartyCreated?.Invoke(lobby);
    }

    public void JoinParty(LobbyData lobby)
    {
        lobby.Join((joinedLobby, ioError) => HandlePartyJoin(joinedLobby, ioError));
    }

    private void HandlePartyJoin(LobbyEnter_t joinedLobby, bool ioError)
    {
        if (ioError)
        {
            Debug.LogError("Failed to join party.");
            return;
        }

        // Convert LobbyEnter_t to LobbyData
        _currentParty = LobbyData.Get(joinedLobby.m_ulSteamIDLobby);

        Debug.Log($"Joined party! ID: {_currentParty.SteamId}");

        OnPartyJoined?.Invoke(_currentParty);
    }

    public void InviteFriendToParty(UserData friend)
    {
        if (_currentParty == null)
        {
            Debug.LogWarning("No active party to invite to.");
            return;
        }

        _currentParty.InviteUserToLobby(friend);
    }

    public void LeaveParty()
    {
        if (_currentParty == null)
        {
            Debug.LogWarning("Not in a party.");
            return;
        }

        _currentParty.Leave();
        _currentParty = null;

        RichPresenceManager.ClearStatus();

        OnPartyLeft?.Invoke();
    }

    public LobbyData CurrentParty => _currentParty;
}