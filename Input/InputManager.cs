using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private InputSystem_Actions _actions;

    public InputSystem_Actions.PlayerActions Player => _actions.Player;
    public InputSystem_Actions.UIActions UI => _actions.UI;

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

    public void EnablePlayerControls()
    {
        _actions.Player.Enable();
    }

    public void DisablePlayerControls()
    {
        _actions.Player.Disable();
    }
}
