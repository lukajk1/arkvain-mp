using UnityEngine;
using UnityEngine.InputSystem;
using PurrNet;

public class TestNetworkScript : NetworkIdentity
{
    [SerializeField] private Color color;
    [SerializeField] private Renderer myRenderer;
    [SerializeField] private InputActionReference setColorAction;
    [SerializeField] private InputActionReference takeDamageAction;

    [SerializeField] private SyncVar<int> health = new(100);

    private void OnEnable()
    {
        if (takeDamageAction != null)
        {
            takeDamageAction.action.Enable();
            takeDamageAction.action.performed += OnTakeDamagePerformed;
        }
    }

    private void OnDisable()
    {
        if (takeDamageAction != null)
        {
            takeDamageAction.action.performed -= OnTakeDamagePerformed;
            takeDamageAction.action.Disable();
        }
    }

    private void OnTakeDamagePerformed(InputAction.CallbackContext context)
    {
        TakeDamage(100);
    }

    [ObserversRpc]
    void SetColor(Color color)
    {
        myRenderer.material.color = color;  
    }

    [ServerRpc]

    void TakeDamage(int damage)
    {
        health.value -= damage;
    }
}
