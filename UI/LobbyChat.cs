using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Heathen.SteamworksIntegration;
using Steamworks;

public class LobbyChat : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private TMP_Text chatHistoryText;
    [SerializeField] private ScrollRect chatScrollRect;

    [Header("Audio")]
    [SerializeField] private AudioClip onNewMessageClip;

    [Header("Chat Settings")]
    [SerializeField] private int maxMessages = 100;

    private Callback<LobbyChatMsg_t> _lobbyChatMsgCallback;
    private string _chatHistory = "";
    private LobbyData _currentLobby;

    void Start()
    {
        if (chatInputField != null)
        {
            chatInputField.onSubmit.AddListener(OnChatSubmitted);
            chatInputField.onSelect.AddListener(OnChatFocused);
            chatInputField.onDeselect.AddListener(OnChatUnfocused);
        }

        // Subscribe to lobby chat messages
        _lobbyChatMsgCallback = Callback<LobbyChatMsg_t>.Create(OnLobbyChatMessage);

        UpdateChatHistory("");
    }

    private void OnChatFocused(string value)
    {
        if (PersistentClient.Instance != null)
            PersistentClient.Instance.currentEscapeContext = EscapeContext.CloseOutChat;
    }

    private void OnChatUnfocused(string value)
    {
        if (PersistentClient.Instance != null)
            PersistentClient.Instance.currentEscapeContext = EscapeContext.Neutral;
    }

    private void OnEnable()
    {
        if (PersistentClient.Instance != null && PersistentClient.Instance.inputManager != null)
        {
            PersistentClient.Instance.inputManager.UI.Submit.performed += OnSubmitPressed;
        }

        // Subscribe to lobby chat updates (join/leave)
        SteamTools.Events.OnLobbyChatUpdate += OnLobbyChatUpdate;
    }

    private void OnDisable()
    {
        if (PersistentClient.Instance != null && PersistentClient.Instance.inputManager != null)
        {
            PersistentClient.Instance.inputManager.UI.Submit.performed -= OnSubmitPressed;
        }

        SteamTools.Events.OnLobbyChatUpdate -= OnLobbyChatUpdate;
    }

    private void OnLobbyChatUpdate(LobbyData lobby, UserData user, EChatMemberStateChange state)
    {
        if (!_currentLobby.IsValid || lobby != _currentLobby) return;

        string systemMessage = "";
        string colorTag = "<color=#AAAAAA>"; // Gray for system messages

        switch (state)
        {
            case EChatMemberStateChange.k_EChatMemberStateChangeEntered:
                systemMessage = $"{colorTag}[System]: {user.Name} joined the lobby.</color>";
                break;
            case EChatMemberStateChange.k_EChatMemberStateChangeLeft:
            case EChatMemberStateChange.k_EChatMemberStateChangeDisconnected:
                systemMessage = $"{colorTag}[System]: {user.Name} left the lobby.</color>";
                break;
        }

        if (!string.IsNullOrEmpty(systemMessage))
        {
            _chatHistory += systemMessage + "\n";
            LimitMessageHistory();
            UpdateChatHistory(_chatHistory);
            ScrollToBottom();
        }
    }

    private void OnSubmitPressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (chatInputField != null && !chatInputField.isFocused)
        {
            chatInputField.ActivateInputField();
        }
    }

    void Update()
    {
        // Automatically track which lobby we're in
        if (LobbyData.MemberOfLobbies.Count > 0)
        {
            _currentLobby = LobbyData.MemberOfLobbies[0];
        }
        else
        {
            _currentLobby = default;
        }
    }

    private void OnChatSubmitted(string message)
    {
        if (!_currentLobby.IsValid)
        {
            Debug.LogWarning("[LobbyChat] Not in a lobby. Cannot send chat message.");
            chatInputField.ActivateInputField();
            return;
        }

        if (string.IsNullOrEmpty(message))
        {
            Debug.LogWarning("[LobbyChat] Chat message is empty.");
            chatInputField.ActivateInputField();
            return;
        }

        SendChatMessage(message);
        chatInputField.text = "";
        chatInputField.ActivateInputField();
    }

    private void SendChatMessage(string message)
    {
        byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
        bool success = SteamMatchmaking.SendLobbyChatMsg(_currentLobby, messageBytes, messageBytes.Length);

        if (success)
        {
            Debug.Log($"[LobbyChat] Sent chat message: {message}");
        }
        else
        {
            Debug.LogError("[LobbyChat] Failed to send chat message.");
        }
    }

    private void OnLobbyChatMessage(LobbyChatMsg_t callback)
    {
        // Get the message data
        byte[] data = new byte[4096];
        CSteamID senderId;
        EChatEntryType chatType;

        int messageLength = SteamMatchmaking.GetLobbyChatEntry(
            new CSteamID(callback.m_ulSteamIDLobby),
            (int)callback.m_iChatID,
            out senderId,
            data,
            data.Length,
            out chatType
        );

        if (messageLength > 0)
        {
            // Convert bytes to string
            string message = System.Text.Encoding.UTF8.GetString(data, 0, messageLength);

            // Get sender name
            UserData sender = senderId;
            string senderName = sender.Name;

            string chatLine = $"[{senderName}]: {message}";
            _chatHistory += chatLine + "\n";

            // Limit message history
            LimitMessageHistory();

            UpdateChatHistory(_chatHistory);
            ScrollToBottom();

            Debug.Log($"[LobbyChat] Chat message received: {chatLine}");
            SoundManager.PlayNonDiegetic(onNewMessageClip);
        }
    }

    private void LimitMessageHistory()
    {
        string[] lines = _chatHistory.Split('\n');
        if (lines.Length > maxMessages)
        {
            int startIndex = lines.Length - maxMessages;
            _chatHistory = string.Join("\n", lines, startIndex, maxMessages);
        }
    }

    private void UpdateChatHistory(string history)
    {
        if (chatHistoryText != null)
        {
            chatHistoryText.text = history;
            // The tutorial mentions forcing a layout rebuild so the ScrollRect knows the new height
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatScrollRect.content);
            ScrollToBottom();
        }
    }

    private void ScrollToBottom()
    {
        if (chatScrollRect == null) return;

        // Force canvas to update so the normalized position calculation is accurate
        Canvas.ForceUpdateCanvases();
        chatScrollRect.verticalNormalizedPosition = 0f;
    }

    void OnDestroy()
    {
        if (chatInputField != null)
            chatInputField.onSubmit.RemoveListener(OnChatSubmitted);
    }
}
