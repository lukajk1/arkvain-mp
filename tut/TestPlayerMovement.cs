using UnityEngine;
using UnityEngine.InputSystem;
using PurrNet;

public class TestPlayerMovement : NetworkIdentity
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private InputActionReference moveAction;

    [Header("Sync Settings")]
    [SerializeField] private SyncVar<Vector3> networkPosition = new(Vector3.zero);

    private Vector2 moveInput;
    private CharacterController characterController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
        }
    }

    private void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.action.Enable();
        }

        // Subscribe to network position changes for non-owner clients
        networkPosition.onChanged += OnNetworkPositionChanged;
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.action.Disable();
        }

        networkPosition.onChanged -= OnNetworkPositionChanged;
    }

    private void Update()
    {
        // Only the owner (local player) reads input and moves directly
        if (isOwner)
        {
            moveInput = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;

            // Apply movement locally for immediate response
            Vector3 movement = new Vector3(moveInput.x, 0f, moveInput.y) * moveSpeed * Time.deltaTime;
            characterController.Move(movement);

            // Send movement input to server so it can validate and sync to others
            if (moveInput != Vector2.zero)
            {
                SendMovementToServer(moveInput);
            }

            // Update our network position for others to see
            networkPosition.value = transform.position;
        }
        else
        {
            // Non-owners smoothly interpolate to the synced position
            transform.position = Vector3.Lerp(transform.position, networkPosition.value, Time.deltaTime * 10f);
        }
    }

    [ServerRpc]
    private void SendMovementToServer(Vector2 input)
    {
        // Server validates and processes the movement
        // This helps prevent cheating and keeps server authoritative
        Vector3 movement = new Vector3(input.x, 0f, input.y) * moveSpeed * Time.deltaTime;

        // Server could add validation here (speed checks, collision checks, etc.)
        // For now, we trust the client's input
    }

    private void OnNetworkPositionChanged(Vector3 newPos)
    {
        // This is called when the synced position changes from the network
        // Non-owners will smoothly move to this position in Update()
    }
}
