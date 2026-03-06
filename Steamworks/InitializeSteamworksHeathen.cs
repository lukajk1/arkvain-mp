using UnityEngine;

public class InitializeSteamworksHeathen : MonoBehaviour
{
    [SerializeField] private GameObject confirmationBox;

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
        GameObject boxInstance = Instantiate(confirmationBox);
        boxInstance.GetComponentInChildren<ConfirmationBox>().Initialize(null, message: $"Error: {error}", confirmText: "Okay");
    }

    private void Start()
    {
        SteamTools.Interface.Initialise();

        if (SteamTools.Interface.IsReady)
        {
            // Ready to go
        }
        else
        {
            // Not ready yet, listen for On Ready
            SteamTools.Interface.OnReady += Interface_OnReady;
        }
    }

    private void Interface_OnReady()
    {
        // Ready now
    }
}
