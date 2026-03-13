using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    private InputSystem_Actions _actions;
    public InputSystem_Actions.PlayerActions Player => _actions.Player;
    public InputSystem_Actions.UIActions UI => _actions.UI;

    // Track what's locking player controls (merged from LockActionMap)
    private readonly HashSet<object> _playerControlsLocks = new HashSet<object>();

    private bool initialized;
    private void Awake()
    {
        // child of persistentclient -- doesn't have to check for uniqueness 
        if (!initialized)
        {
            _actions = new InputSystem_Actions();
            _actions.Player.Enable();
            _actions.UI.Enable();
            initialized = true;
        }
    }
    private void OnDestroy()
    {
        _actions?.Dispose();
    }
    public void EnablePlayerControls()
    {
        _actions.Player.Enable();
    }

    public void DisablePlayerControls()
    {
        _actions.Player.Disable();
    }

    /// <summary>
    /// Adds or removes an object from the list of things locking the player controls.
    /// Controls are only enabled when the lock list is empty.
    /// </summary>
    public void ModifyPlayerControlsLockList(bool isAdding, object lockObject)
    {
        if (isAdding)
        {
            _playerControlsLocks.Add(lockObject);
        }
        else
        {
            _playerControlsLocks.Remove(lockObject);
        }

        UpdatePlayerControlsState();
    }

    public void UnlockPlayerControls(object lockObject)
    {
        ModifyPlayerControlsLockList(false, lockObject);
    }

    public void ClearAllLocks()
    {
        _playerControlsLocks.Clear();
        UpdatePlayerControlsState();
    }

    private void UpdatePlayerControlsState()
    {
        if (_actions == null) return;

        if (_playerControlsLocks.Count > 0)
        {
            _actions.Player.Disable();
        }
        else
        {
            _actions.Player.Enable();
        }
    }
}
