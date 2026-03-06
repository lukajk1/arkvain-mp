using UnityEngine;
using Heathen.SteamworksIntegration;
using System.Collections.Generic;
using TMPro;

public class ServerBrowser : MonoBehaviour
{
    [SerializeField] private GameObject serverRowPrefab;
    [SerializeField] private Transform contentParent; // The "Content" GameObject of ScrollView
    [SerializeField] private TMP_Text refreshTimerText;
    [SerializeField] private UnityEngine.UI.Button refreshButton;

    [Header("Auto Refresh Settings")]
    [SerializeField] private bool autoRefresh = true;
    [SerializeField] private float refreshInterval = 10f;

    private List<GameObject> activeRows = new List<GameObject>();
    private float _lastRefreshTime;

    void Start()
    {
        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnRefreshButtonClicked);
    }

    void Update()
    {
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
    }
}
