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
    /// Lock player controls (merged from LockActionMap functionality).
    /// Multiple systems can lock - controls only unlock when all locks are removed.
    /// </summary>
    public void LockPlayerControls(object requester)
    {
        _playerControlsLocks.Add(requester);
        UpdatePlayerControlsState();
    }

    /// <summary>
    /// Unlock player controls for a specific requester.
    /// </summary>
    public void UnlockPlayerControls(object requester)
    {
        _playerControlsLocks.Remove(requester);
        UpdatePlayerControlsState();
    }

    /// <summary>
    /// Updates the player controls enabled state based on active locks.
    /// </summary>
    private void UpdatePlayerControlsState()
    {
        if (_playerControlsLocks.Count > 0)
        {
            _actions.Player.Disable();
        }
        else
        {
            _actions.Player.Enable();
        }
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
