using UnityEngine;
using Heathen.SteamworksIntegration;
using System.Collections.Generic;
using TMPro;

public class ServerBrowser : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject serverRowPrefab;
    [SerializeField] private Transform contentParent; // The "Content" GameObject of ScrollView
    [SerializeField] private TMP_Text refreshTimerText;
    [SerializeField] private UnityEngine.UI.Button refreshButton;
    [SerializeField] private UnityEngine.UI.Button backButton;
    [SerializeField] private TMP_InputField joinCodeInputField;

    [Header("Auto Refresh Settings")]
    [SerializeField] private bool autoRefresh = true;
    [SerializeField] private float refreshInterval = 10f;

    private List<GameObject> activeRows = new List<GameObject>();
    private float _lastRefreshTime;

    private void Awake()
    {
        SetState(false);
    }
    void Start()
    {
        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnRefreshButtonClicked);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClicked);

        if (joinCodeInputField != null)
            joinCodeInputField.onSubmit.AddListener(OnJoinCodeSubmitted);
    }

    public void SetState(bool value)
    {
        if (canvas != null)
        {
            canvas.gameObject.SetActive(value);
        }

        if (value) RefreshServerList();
    }

    void Update()
    {
        // don't refresh when not visible
        if (!canvas.gameObject.activeSelf) return;

        if (autoRefresh)
        {
            float timeSinceLastRefresh = Time.time - _lastRefreshTime;
            float timeRemaining = refreshInterval - timeSinceLastRefresh;

            if (refreshTimerText != null)
            {
                if (timeRemaining > 0)
                {
                    refreshTimerText.text = $"Refreshing in {Mathf.CeilToInt(timeRemaining)}s";
                }
                else
                {
                    refreshTimerText.text = "Refreshing...";
                }
            }

            if (timeSinceLastRefresh >= refreshInterval)
            {
                RefreshServerList();
            }
        }
        else
        {
            if (refreshTimerText != null)
                refreshTimerText.text = "Auto-refresh disabled";
        }
    }

    public void PopulateServerList(LobbyData[] lobbies)
    {
        // Clear existing rows
        ClearServerList();

        // Create a row for each lobby
        foreach (var lobby in lobbies)
        {
            GameObject rowObject = Instantiate(serverRowPrefab, contentParent);
            ServerRow row = rowObject.GetComponent<ServerRow>();

            if (row != null)
            {
                row.Setup(lobby);
            }
            else
            {
                Debug.LogError("[ServerBrowser] ServerRow component not found on prefab!");
            }

            activeRows.Add(rowObject);
        }

        Debug.Log($"[ServerBrowser] Populated {lobbies.Length} servers");
    }

    public void ClearServerList()
    {
        foreach (var row in activeRows)
        {
            Destroy(row);
        }
        activeRows.Clear();
    }

    private void OnRefreshButtonClicked()
    {
        RefreshServerList();
    }

    private void OnBackButtonClicked()
    {
        SetState(false);
    }

    private void OnJoinCodeSubmitted(string lobbyCode)
    {
        if (string.IsNullOrEmpty(lobbyCode))
        {
            Debug.LogWarning("[ServerBrowser] Lobby code is empty!");
            return;
        }

        Debug.Log($"[ServerBrowser] Attempting to join lobby with code: {lobbyCode}");

        // Use the static Join method that accepts a string (HexId)
        LobbyData.Join(lobbyCode, (enterData, ioError) =>
        {
            if (ioError)
            {
                Debug.LogError($"[ServerBrowser] Failed to join lobby: IO Error");
                return;
            }

            if (enterData.Response == Steamworks.EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
                Debug.Log($"[ServerBrowser] Successfully joined lobby via code!");

                // Clear the input field on success
                if (joinCodeInputField != null)
                    joinCodeInputField.text = "";
            }
            else
            {
                Debug.LogWarning($"[ServerBrowser] Failed to join lobby: {enterData.Response}");
            }
        });
    }

    public void RefreshServerList()
    {
        _lastRefreshTime = Time.time;

        // Create search arguments
        SearchArguments args = new SearchArguments();
        args.distance = Steamworks.ELobbyDistanceFilter.k_ELobbyDistanceFilterDefault;

        LobbyData.Request(args, 50, (lobbies, ioError) =>
        {
            if (ioError)
            {
                Debug.LogError("[ServerBrowser] Failed to search for lobbies!");
                return;
            }

            PopulateServerList(lobbies);
        });
    }

    void OnDestroy()
    {
        if (refreshButton != null)
            refreshButton.onClick.RemoveListener(OnRefreshButtonClicked);

        if (backButton != null)
            backButton.onClick.RemoveListener(OnBackButtonClicked);

        if (joinCodeInputField != null)
            joinCodeInputField.onSubmit.RemoveListener(OnJoinCodeSubmitted);
    }
}
