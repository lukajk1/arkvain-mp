using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public enum EscapeContext
{
    Neutral,
    CloseOutChat
}

public class PersistentClient : MonoBehaviour
{
    public static PersistentClient Instance { get; private set; }
    [SerializeField] public InputManager inputManager;
    [SerializeField] private GameObject confirmationBox;

    [Header("Scene References")]
    [SerializeField] public SceneNameHolder gameScene;

    public EscapeContext currentEscapeContext = EscapeContext.Neutral;

    public static float cm360;
    public static float playerDPI;

    private static List<object> cursorUnlockList = new();
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        GameSettings.Initialize();

        // Reapply settings after a delay to ensure graphics system is fully initialized
        StartCoroutine(ReapplySettingsDelayed());
    }

    private System.Collections.IEnumerator ReapplySettingsDelayed()
    {
        // Apply after 1 frame
        yield return null;
        GameSettings.Instance?.ApplySettings();
        Debug.Log("Settings reapplied after 1 frame");
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(DelayedSceneLoaded(scene));
    }

    private System.Collections.IEnumerator DelayedSceneLoaded(Scene scene)
    {
        // Wait 1 frame to let all scene objects run Awake/Start
        yield return null;

        // refresh to default cursor state
        ClearCursorUnlockList();

        bool isGameScene = scene.name == gameScene.sceneName;

        // handles additive cases where the scene argument might be a map chunk
        if (!isGameScene)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == gameScene.sceneName)
                {
                    isGameScene = true;
                    break;
                }
            }
        }

        if (!isGameScene)
        {
            // unlock the cursor if NOT in game scene
            AddToCursorUnlockList(true, this);
        }
        else
        {
            Debug.Log("[PersistentClient] Game scene detected - ensuring cursor is locked");
            SetCursorLocked(true);
        }
    }

    private void ClearCursorUnlockList()
    {
        cursorUnlockList.Clear();
        
        if (inputManager != null)
        {
            inputManager.ClearAllLocks();
        }

        SetCursorLocked(true); // defaults to locked
    }

    private void SetCursorLocked(bool locked)
    {
        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            if (inputManager != null)
            {
                // Remove this object from the lock list to allow controls
                inputManager.ModifyPlayerControlsLockList(false, this);
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            if (inputManager != null)
            {
                // Add this object to the lock list to disable controls
                inputManager.ModifyPlayerControlsLockList(true, this);
            }
        }
    }
    private void Update()
    {
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[PersistentClient] Cursor Locked: {Cursor.lockState == CursorLockMode.Locked}, Lock Count: {cursorUnlockList.Count}");
        }
    }
    public void CreateConfirmationDialog(
        Action onConfirm = null,
        Action onCancel = null,
        string message = "no message was provided",
        string confirmText = "Confirm",
        string cancelText = "Cancel")
    {
        GameObject boxInstance = Instantiate(confirmationBox);
        boxInstance.GetComponentInChildren<ConfirmationBox>().Initialize(onConfirm, onCancel, message, confirmText, cancelText);
    }
    public static void AddToCursorUnlockList(bool isAdding, object obj)
    {
        if (isAdding)
        {
            if (cursorUnlockList.Contains(obj)) return; // no need to modify in this case
            else
            {
                cursorUnlockList.Add(obj);
            }
        }
        else cursorUnlockList.Remove(obj);

        if (cursorUnlockList.Count > 0)
        {
            Instance.SetCursorLocked(false);
        }
        else
        {
            Instance.SetCursorLocked(true);
        }
    }

}
