using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ClientGame : MonoBehaviour
{
    public static ClientGame Instance { get; private set; }

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
    public static float mouseSensitivity = 250f;
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

    public static void ModifyCursorUnlockList(bool isAdding, object obj)
    {
        if (isAdding)
        {
            if (cursorLockList.Contains(obj)) return; // no need to modify then
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
