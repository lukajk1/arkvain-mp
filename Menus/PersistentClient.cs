using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PersistentClient : MonoBehaviour
{
    public static PersistentClient Instance { get; private set; }

    public static float cm360;
    public static float playerDPI;

    private static CursorLockMode _cursorDefaultState;

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
            Debug.LogError($"More than one instance of {Instance} in scene");
            Destroy(gameObject);
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _cursorDefaultState = CursorLockMode.Confined;
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

        //// Apply after 100ms
        //yield return new WaitForSeconds(0.1f);
        //GameSettings.Instance?.ApplySettings();
        //Debug.Log("Settings reapplied after 100ms delay");
    }

    public static void SetDefaultCursorState(CursorLockMode state)
    {
        _cursorDefaultState = state;

        // Reapply cursor state if no overrides are active
        if (cursorLockList.Count == 0)
        {
            CursorLockState = _cursorDefaultState;
        }
    }

    public static void ModifyCursorUnlockList(bool isAdding, object obj)
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
            CursorLockState = _cursorDefaultState;
        }
    }

}
