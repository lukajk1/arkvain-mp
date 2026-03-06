using UnityEngine;
using Heathen.SteamworksIntegration;
using System.Collections.Generic;

public class ServerBrowser : MonoBehaviour
{
    [SerializeField] private GameObject serverRowPrefab;
    [SerializeField] private Transform contentParent; // The "Content" GameObject of ScrollView

    private List<GameObject> activeRows = new List<GameObject>();

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

    public void RefreshServerList()
    {
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
}
