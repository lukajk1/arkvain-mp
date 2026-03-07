using UnityEngine;

public class InitializeSteamworksHeathen : MonoBehaviour
{
    public static bool SteamInitialized;

    private void Awake()
    {
        SteamTools.Interface.OnInitialisationError += OnInitializationError;
    }

    private void OnDestroy()
    {
        SteamTools.Interface.OnInitialisationError -= OnInitializationError;
    }

    void OnInitializationError(string error)
    {
        PersistentClient.Instance.CreateConfirmationDialog(message: $"Error: {error}", confirmText: "Okay");
    }

    private void Start()
    {
        SteamTools.Interface.Initialise();

        if (SteamTools.Interface.IsReady)
        {
            SteamInitialized = true;
        }
        else
        {
            // Not ready yet, listen for On Ready
            SteamTools.Interface.OnReady += Interface_OnReady;
        }
    }

    private void Interface_OnReady()
    {
        SteamInitialized = true;
    }
}
