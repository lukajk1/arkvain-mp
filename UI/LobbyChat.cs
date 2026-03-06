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

    private Callback<LobbyChatMsg_t> _lobbyChatMsgCallback;
    private string _chatHistory = "";
    private LobbyData _currentLobby;

    void Start()
    {
        if (chatInputField != null)
            chatInputField.onSubmit.AddListener(OnChatSubmitted);

        // Subscribe to lobby chat messages
        _lobbyChatMsgCallback = Callback<LobbyChatMsg_t>.Create(OnLobbyChatMessage);

        UpdateChatHistory("");
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
            UpdateChatHistory(_chatHistory);

            Debug.Log($"[LobbyChat] Chat message received: {chatLine}");
        }
    }

    private void UpdateChatHistory(string history)
    {
        if (chatHistoryText != null)
            chatHistoryText.text = history;
    }

    void OnDestroy()
    {
        if (chatInputField != null)
            chatInputField.onSubmit.RemoveListener(OnChatSubmitted);
    }
}
