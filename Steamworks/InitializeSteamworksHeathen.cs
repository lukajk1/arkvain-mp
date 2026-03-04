using UnityEngine;

public class InitializeSteamworksHeathen : MonoBehaviour
{
    private void Start()
    {
        SteamTools.Interface.WhenReady(Interface_OnReady);
    }

    private void Interface_OnReady()
    {
        Debug.Log("steamworks ready");
        SteamTools.Interface.Initialise();
    }
}
