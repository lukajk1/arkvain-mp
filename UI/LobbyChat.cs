using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Heathen.SteamworksIntegration;
using Steamworks;
using System.Collections.Generic;
using System.Text;
using UnityEngine.SceneManagement;

public class LobbyChat : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private TMP_Text chatHistoryText;
    [SerializeField] private ScrollRect chatScrollRect;

    [Header("To hide when in game scene")]
    [SerializeField] private SceneNameHolder gameScene;
    [SerializeField] private Image chatBacking;
    [SerializeField] private Image scrollbarBack;
    [SerializeField] private Image scrollbarHandle;

    [Header("Audio")]
    [SerializeField] private AudioClip onNewMessageClip;

    [Header("Chat Settings")]
    [SerializeField] private int maxMessages = 100;

    private Callback<LobbyChatMsg_t> _lobbyChatMsgCallback;
    private List<string> _chatLines = new List<string>();
    private LobbyData _currentLobby;

    private bool isGameScene = false;

    private void Start()
    {
        if (chatInputField != null)
        {
            chatInputField.onSubmit.AddListener(OnChatSubmitted);
            chatInputField.onSelect.AddListener(OnChatFocused);
            chatInputField.onDeselect.AddListener(OnChatUnfocused);
            
            // Hide by default
            chatInputField.gameObject.SetActive(false);
        }

        // Determine if we are in the game scene
        if (gameScene != null)
        {
            isGameScene = SceneManager.GetActiveScene().name == gameScene.sceneName;
        }

        // Initial visibility check
        SetImagesVisibility(!isGameScene);

        // Subscribe to low-level lobby chat messages
        _lobbyChatMsgCallback = Callback<LobbyChatMsg_t>.Create(OnLobbyChatMessage);

        RefreshChatUI();

        // Safe subscription to global input
        if (PersistentClient.Instance != null && PersistentClient.Instance.inputManager != null)
        {
            PersistentClient.Instance.inputManager.UI.Submit.performed += OnSubmitPressed;
        }
    }

    private void SetImagesVisibility(bool visible)
    {
        if (chatBacking != null) chatBacking.enabled = visible;
        if (scrollbarBack != null) scrollbarBack.enabled = visible;
        if (scrollbarHandle != null) scrollbarHandle.enabled = visible;
    }

    private void OnChatFocused(string value)
    {
        if (PersistentClient.Instance != null)
        {
            PersistentClient.Instance.currentEscapeContext = EscapeContext.CloseOutChat;
            
            // Lock player controls while typing
            if (PersistentClient.Instance.inputManager != null)
            {
                PersistentClient.Instance.inputManager.ModifyPlayerControlsLockList(true, this);
            }
        }

        if (isGameScene)
        {
            SetImagesVisibility(true);
        }
    }

    private void OnChatUnfocused(string value)
    {
        if (PersistentClient.Instance != null)
        {
            PersistentClient.Instance.currentEscapeContext = EscapeContext.Neutral;
            
            // Unlock player controls
            if (PersistentClient.Instance.inputManager != null)
            {
                PersistentClient.Instance.inputManager.ModifyPlayerControlsLockList(false, this);
            }
        }

        if (isGameScene)
        {
            SetImagesVisibility(false);
        }

        // Hide the input field when it loses focus
        if (chatInputField != null)
        {
            chatInputField.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (chatInputField != null)
            chatInputField.onSubmit.RemoveListener(OnChatSubmitted);

        if (PersistentClient.Instance != null && PersistentClient.Instance.inputManager != null)
        {
            PersistentClient.Instance.inputManager.UI.Submit.performed -= OnSubmitPressed;
        }
    }

    private void OnEnable()
    {
        // Subscribe to lobby updates
        SteamTools.Events.OnLobbyChatUpdate += OnLobbyChatUpdate;
        SteamTools.Events.OnLobbyEnterSuccess += OnLobbyEnterSuccess;
        SteamTools.Events.OnLobbyLeave += OnLobbyLeave;

        // Initialize current lobby if already in one
        UpdateCurrentLobby();
    }

    private void OnDisable()
    {
        SteamTools.Events.OnLobbyChatUpdate -= OnLobbyChatUpdate;
        SteamTools.Events.OnLobbyEnterSuccess -= OnLobbyEnterSuccess;
        SteamTools.Events.OnLobbyLeave -= OnLobbyLeave;
    }

    private void OnLobbyEnterSuccess(LobbyData lobby)
    {
        _currentLobby = lobby;
        AddSystemMessage("Joined the lobby.");
    }

    private void OnLobbyLeave(LobbyData lobby)
    {
        if (_currentLobby == lobby)
        {
            _currentLobby = default;
        }
    }

    private void OnLobbyChatUpdate(LobbyData lobby, UserData user, EChatMemberStateChange state)
    {
        // If we don't have a current lobby tracked, try to adopt this one if we are a member
        if (!_currentLobby.IsValid)
        {
            UpdateCurrentLobby();
        }

        if (!_currentLobby.IsValid || lobby != _currentLobby) return;

        // Don't show "joined" message for ourselves if we already handled it in OnLobbyEnterSuccess
        bool isLocalUser = user == UserData.Me;
        
        string systemMessage = "";
        string colorTag = "<color=#AAAAAA>"; // Gray for system messages
        string nameColor = "<color=#FFFFFF>"; // White for names in system messages

        switch (state)
        {
            case EChatMemberStateChange.k_EChatMemberStateChangeEntered:
                if (!isLocalUser)
                    systemMessage = $"{colorTag}[System]: {nameColor}{user.Name}</color> joined the lobby.</color>";
                break;
            case EChatMemberStateChange.k_EChatMemberStateChangeLeft:
                if (!isLocalUser)
                    systemMessage = $"{colorTag}[System]: {nameColor}{user.Name}</color> left the lobby.</color>";
                break;
            case EChatMemberStateChange.k_EChatMemberStateChangeDisconnected:
                systemMessage = $"{colorTag}[System]: {nameColor}{user.Name}</color> disconnected.</color>";
                break;
            case EChatMemberStateChange.k_EChatMemberStateChangeKicked:
                systemMessage = $"{colorTag}[System]: {nameColor}{user.Name}</color> was kicked from the lobby.</color>";
                break;
            case EChatMemberStateChange.k_EChatMemberStateChangeBanned:
                systemMessage = $"{colorTag}[System]: {nameColor}{user.Name}</color> was banned from the lobby.</color>";
                break;
        }

        if (!string.IsNullOrEmpty(systemMessage))
        {
            AddChatMessage(systemMessage);
        }
    }

    private void AddSystemMessage(string message)
    {
        string colorTag = "<color=#AAAAAA>";
        AddChatMessage($"{colorTag}[System]: {message}</color>");
    }

    private void AddChatMessage(string line)
    {
        _chatLines.Add(line);
        
        if (_chatLines.Count > maxMessages)
        {
            _chatLines.RemoveAt(0);
        }

        RefreshChatUI();
        
        if (onNewMessageClip != null)
            SoundManager.PlayNonDiegetic(onNewMessageClip);
    }

    private void RefreshChatUI()
    {
        if (chatHistoryText != null)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _chatLines.Count; i++)
            {
                sb.AppendLine(_chatLines[i]);
            }
            chatHistoryText.text = sb.ToString();
            
            // Force layout rebuild so ScrollRect knows the new height
            if (chatScrollRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(chatScrollRect.content);
                ScrollToBottom();
            }
        }
    }

    private void OnSubmitPressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (chatInputField != null && !chatInputField.gameObject.activeInHierarchy)
        {
            chatInputField.gameObject.SetActive(true);
            chatInputField.ActivateInputField();
        }
    }

    private void Update()
    {
        // Safety check: if we somehow lost our lobby reference but are still in one
        if (!_currentLobby.IsValid && LobbyData.MemberOfLobbies.Count > 0)
        {
            UpdateCurrentLobby();
        }
    }

    private void UpdateCurrentLobby()
    {
        if (LobbyData.MemberOfLobbies.Count > 0)
        {
            _currentLobby = LobbyData.MemberOfLobbies[0];
        }
    }

    private void OnChatSubmitted(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            chatInputField.text = "";
            // Unfocus the input field which will trigger OnChatUnfocused and hide it
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            return;
        }

        if (!_currentLobby.IsValid)
        {
            Debug.LogWarning("[LobbyChat] Not in a lobby. Cannot send chat message.");
            // Re-focus so the user can see why it didn't send or try again
            chatInputField.ActivateInputField();
            return;
        }

        SendChatMessage(message);
        chatInputField.text = "";
        
        // After sending, unfocus/hide the field
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }

    private void SendChatMessage(string message)
    {
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        bool success = SteamMatchmaking.SendLobbyChatMsg(_currentLobby, messageBytes, messageBytes.Length);

        if (!success)
        {
            Debug.LogError("[LobbyChat] Failed to send chat message.");
        }
    }

    private void OnLobbyChatMessage(LobbyChatMsg_t callback)
    {
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
            string message = Encoding.UTF8.GetString(data, 0, messageLength);
            UserData sender = senderId;
            
            string chatLine = $"<color=#FFFFFF>[{sender.Name}]:</color> {message}";
            AddChatMessage(chatLine);
        }
    }

    private void ScrollToBottom()
    {
        if (chatScrollRect == null) return;
        Canvas.ForceUpdateCanvases();
        chatScrollRect.verticalNormalizedPosition = 0f;
    }
}
