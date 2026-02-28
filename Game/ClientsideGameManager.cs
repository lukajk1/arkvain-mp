using UnityEngine;

public class ClientsideGameManager : MonoBehaviour
{
    public static ClientsideGameManager Instance {  get; private set; }

    public static Camera _mainCamera;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError($"More than one instance of {Instance} in scene");
        }

        Instance = this;
    }

    public void RegisterMainCamera(Camera camera)
    {
        _mainCamera = camera;
        Debug.Log("new maincamera registered");
    }

    // the distance at which to stop rendering environmental hit effects
    public static float maxVFXDistance = 38f;

    private void Start()
    {
        PersistentClient.SetDefaultCursorState(CursorLockMode.Locked);
    }

    private void OnDestroy()
    {
        // Restore default to Confined when leaving game scene
        PersistentClient.SetDefaultCursorState(CursorLockMode.Confined);
    }

}
