using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private InputSystem_Actions _actions;

    public InputSystem_Actions.PlayerActions Player => _actions.Player;
    public InputSystem_Actions.UIActions UI => _actions.UI;

    // Track what's locking player controls (merged from LockActionMap)
    private readonly HashSet<object> _playerControlsLocks = new HashSet<object>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            _actions = new InputSystem_Actions();
            _actions.Player.Enable();
            _actions.UI.Enable();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            _actions?.Dispose();
        }
    }

    /// <summary>
    /// Modify the player controls lock list.
    /// Multiple systems can lock - controls only unlock when all locks are removed.
    /// </summary>
    public void ModifyPlayerControlsLockList(bool isAdding, object obj)
    {
        if (isAdding)
        {
            if (_playerControlsLocks.Contains(obj)) return; // no need to modify then
            else
            {
                _playerControlsLocks.Add(obj);
            }
        }
        else _playerControlsLocks.Remove(obj);

        if (_playerControlsLocks.Count > 0)
        {
            _actions.Player.Disable();
        }
        else
        {
            _actions.Player.Enable();
        }
    }

    /// <summary>
    /// Lock player controls (legacy method - use ModifyPlayerControlsLockList instead).
    /// </summary>
    [System.Obsolete("Use ModifyPlayerControlsLockList(true, this) instead")]
    public void LockPlayerControls(object requester)
    {
        ModifyPlayerControlsLockList(true, requester);
    }

    /// <summary>
    /// Unlock player controls for a specific requester (legacy method - use ModifyPlayerControlsLockList instead).
    /// </summary>
    [System.Obsolete("Use ModifyPlayerControlsLockList(false, this) instead")]
    public void UnlockPlayerControls(object requester)
    {
        ModifyPlayerControlsLockList(false, requester);
    }

    // Legacy methods for backwards compatibility (now use lock/unlock instead)
    [System.Obsolete("Use LockPlayerControls/UnlockPlayerControls instead")]
    public void EnablePlayerControls()
    {
        _actions.Player.Enable();
    }

    [System.Obsolete("Use LockPlayerControls/UnlockPlayerControls instead")]
    public void DisablePlayerControls()
    {
        _actions.Player.Disable();
    }
}
