using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class PersistentClient : MonoBehaviour
{
    public static PersistentClient Instance { get; private set; }
    [SerializeField] public InputManager inputManager;
    [SerializeField] private GameObject confirmationBox;

    [Header("Scene References")]
    [SerializeField] public SceneNameHolder gameScene;

    public static float cm360;
    public static float playerDPI;

    private static CursorLockMode _cursorLockState;
    public static CursorLockMode CursorLockState
    {
        get => _cursorLockState;
        private set
        {
            if (_cursorLockState != value)
            {
                _cursorLockState = value;
                Cursor.lockState = value;
                Cursor.visible = value == CursorLockMode.None;
            }
        }
    }
    private static List<object> cursorLockList = new();
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
        if (scene.name == gameScene.sceneName)
        {
            SetCursorToPlayMode(true);
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
    public void SetCursorToPlayMode(bool value)
    {
        PersistentClient.Instance.inputManager.ModifyPlayerControlsLockList(!value, this);
        ModifyCursorUnlockList(!value, this);
    }

    private static void ModifyCursorUnlockList(bool isAdding, object obj)
    {
        if (isAdding)
        {
            if (cursorLockList.Contains(obj)) return; // no need to modify in this case
            else
            {
                cursorLockList.Add(obj);
            }
        }
        else cursorLockList.Remove(obj);

        if (cursorLockList.Count > 0)
        {
            CursorLockState = CursorLockMode.None;
        }
        else
        {
            CursorLockState = CursorLockMode.Locked;
        }
    }

}
