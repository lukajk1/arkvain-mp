using Heathen.SteamworksIntegration;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using API = Heathen.SteamworksIntegration.API;

public class LobbyTester : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button searchLobbiesButton;
    [SerializeField] private TMP_Text resultsText;

    [Header("Lobby Settings")]
    [SerializeField] private int maxMembers = 4;
    [SerializeField] private ELobbyType lobbyType = ELobbyType.k_ELobbyTypePublic;

    [Header("Search Settings")]
    [SerializeField] private bool autoRefresh = false;
    [SerializeField] private float refreshInterval = 5f;
    [SerializeField] private int maxSearchResults = 10;

    private LobbyData _currentLobby;
    private float _lastSearchTime;
    private List<LobbyData> _searchResults = new List<LobbyData>();

    private void Start()
    {
        if (createLobbyButton != null)
            createLobbyButton.onClick.AddListener(OnCreateLobbyClicked);

        if (searchLobbiesButton != null)
            searchLobbiesButton.onClick.AddListener(OnSearchLobbiesClicked);

        UpdateResultsText("Ready to create or search lobbies.");
    }

    private void Update()
    {
        if (autoRefresh && Time.time - _lastSearchTime >= refreshInterval)
        {
            SearchForLobbies();
        }
    }

    private void OnCreateLobbyClicked()
    {
        UpdateResultsText("Creating lobby...");

        API.Matchmaking.Client.CreateLobby(lobbyType, SteamLobbyModeType.Session, maxMembers, HandleLobbyCreated);
    }

    private void HandleLobbyCreated(EResult result, LobbyData lobby, bool ioError)
    {
        if (ioError || result != EResult.k_EResultOK)
        {
            UpdateResultsText($"Failed to create lobby!\nResult: {result}\nIO Error: {ioError}");
            return;
        }

        _currentLobby = lobby;

        // Set some test metadata
        lobby["game_mode"] = "Test Mode";
        lobby["map"] = "Test Map";

        UpdateResultsText($"Lobby Created Successfully!\n" +
                         $"Lobby ID: {lobby.SteamId}\n" +
                         $"Type: {lobbyType}\n" +
                         $"Max Members: {maxMembers}\n" +
                         $"Owner: {lobby.Owner.user.Name}");

        Debug.Log($"[LobbyTester] Created lobby with ID: {lobby.SteamId}");
    }

    private void OnSearchLobbiesClicked()
    {
        SearchForLobbies();
    }

    private void SearchForLobbies()
    {
        _lastSearchTime = Time.time;
        UpdateResultsText("Searching for lobbies...");

        // Create search arguments
        SearchArguments args = new SearchArguments();
        args.distance = ELobbyDistanceFilter.k_ELobbyDistanceFilterDefault;

        // Example: Add filters for specific metadata
        // args.stringFilters.Add(new SearchArguments.StringFilter
        // {
        //     key = "game_mode",
        //     value = "Test Mode",
        //     comparison = ELobbyComparison.k_ELobbyComparisonEqual
        // });

        LobbyData.Request(args, maxSearchResults, HandleSearchComplete);
    }

    private void HandleSearchComplete(LobbyData[] results, bool ioError)
    {
        if (ioError)
        {
            UpdateResultsText("Failed to search lobbies!\nIO Error occurred.");
            return;
        }

        _searchResults.Clear();
        _searchResults.AddRange(results);

        if (results.Length == 0)
        {
            UpdateResultsText("No lobbies found.\n" +
                            "(Note: Steam limits search results to 50 lobbies)");
            return;
        }

        string resultsMessage = $"Found {results.Length} lobby/lobbies:\n\n";

        for (int i = 0; i < results.Length; i++)
        {
            var lobby = results[i];
            resultsMessage += $"[{i + 1}] Lobby ID: {lobby.SteamId}\n";
            resultsMessage += $"    Owner: {lobby.Owner.user.Name}\n";
            resultsMessage += $"    Members: {lobby.MemberCount}/{lobby.MaxMembers}\n";

            // Display metadata if available
            string gameMode = lobby["game_mode"];
            if (!string.IsNullOrEmpty(gameMode))
                resultsMessage += $"    Game Mode: {gameMode}\n";

            string map = lobby["map"];
            if (!string.IsNullOrEmpty(map))
                resultsMessage += $"    Map: {map}\n";

            resultsMessage += "\n";
        }

        UpdateResultsText(resultsMessage);
        Debug.Log($"[LobbyTester] Found {results.Length} lobbies");
    }

    private void UpdateResultsText(string message)
    {
        if (resultsText != null)
            resultsText.text = message;

        Debug.Log($"[LobbyTester] {message}");
    }

    public void JoinLobbyByIndex(int index)
    {
        if (index < 0 || index >= _searchResults.Count)
        {
            UpdateResultsText($"Invalid lobby index: {index}");
            return;
        }

        var lobbyToJoin = _searchResults[index];
        UpdateResultsText($"Joining lobby {lobbyToJoin.SteamId}...");

        lobbyToJoin.Join((enterData, ioError) => HandleLobbyJoined(enterData, ioError));
    }

    private void HandleLobbyJoined(LobbyEnter_t enterData, bool ioError)
    {
        if (ioError)
        {
            UpdateResultsText($"Failed to join lobby!\nIO Error occurred.");
            return;
        }

        _currentLobby = LobbyData.Get(enterData.m_ulSteamIDLobby);
        UpdateResultsText($"Successfully joined lobby!\n" +
                         $"Lobby ID: {_currentLobby.SteamId}\n" +
                         $"Members: {_currentLobby.MemberCount}/{_currentLobby.MaxMembers}");

        Debug.Log($"[LobbyTester] Joined lobby: {_currentLobby.SteamId}");
    }

    public void LeaveLobby()
    {
        if (_currentLobby.IsValid)
        {
            _currentLobby.Leave();
            UpdateResultsText("Left the lobby.");
            _currentLobby = default;
        }
        else
        {
            UpdateResultsText("Not in a lobby.");
        }
    }

    private void OnDestroy()
    {
        if (createLobbyButton != null)
            createLobbyButton.onClick.RemoveListener(OnCreateLobbyClicked);

        if (searchLobbiesButton != null)
            searchLobbiesButton.onClick.RemoveListener(OnSearchLobbiesClicked);
    }
}
