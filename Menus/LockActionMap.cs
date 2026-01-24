using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ActionMapType
{
    PlayerControls = 5,
}

public class LockActionMap : MonoBehaviour
{
    public static LockActionMap i;
    [SerializeField] InputActionAsset inputActions;
    private InputActionMap mainMap;

    private Dictionary<ActionMapType, List<object>> lockingDictionary = new();

    private void Awake()
    {
        i = this;
    }

    private void Start()
    {
        mainMap = inputActions.FindActionMap("Player");
    }

    public void ModifyLockList(ActionMapType targetMap, bool isLocking, object obj)
    {
        InputActionMap targetActionMap = GetActionMap(targetMap);
        if (targetActionMap == null) return;

        if (!lockingDictionary.ContainsKey(targetMap))
        {
            lockingDictionary[targetMap] = new List<object>();
        }

        List<object> lockingList = lockingDictionary[targetMap];

        if (isLocking)
        {
            if (!lockingList.Contains(obj))
            {
                lockingList.Add(obj);
            }
        }
        else
        {
            lockingList.Remove(obj);
        }

        if (lockingList.Count > 0)
        {
            targetActionMap.Disable();
        }
        else
        {
            targetActionMap.Enable();
        }
    }

    private InputActionMap GetActionMap(ActionMapType mapType)
    {
        switch (mapType)
        {
            case ActionMapType.PlayerControls:
                return mainMap;
            default:
                return mainMap;
        }
    }
}
