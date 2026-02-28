using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ClientGame : MonoBehaviour
{
    public static ClientGame Instance { get; private set; }

    public static float targetCm360 = 34.6f; // Standard competitive sensitivity
    public static float playerDPI = 800f;

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

    public static Camera _mainCamera;

    private static List<object> cursorLockList = new();

    // the distance at which to stop rendering environmental hit effects
    public static float maxVFXDistance = 38f;
    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError($"More than one instance of {Instance} in scene");
        }

        Instance = this;
    }

    private void Start()
    {
        CursorLockState = CursorLockMode.Locked;
    }

    public void RegisterMainCamera(Camera camera)
    {
        _mainCamera = camera;
        Debug.Log("new maincamera registered");
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
            CursorLockState = CursorLockMode.Locked;
        }
    }

}
