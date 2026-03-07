using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Heathen.SteamworksIntegration;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenu : MonoBehaviour
{
    [Header("Elements")]
    [SerializeField] private Button host;
    [SerializeField] private Button browse;
    [SerializeField] private Button customize;
    [SerializeField] private Button options;
    [SerializeField] private Button quit;

    [Header("Classes")]
    [SerializeField] private LobbyManager lobbyView;
    [SerializeField] private ServerBrowser serverBrowser;
    [SerializeField] private SettingsMenu settingsMenu;

    public static MainMenu Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void OnEnable()
    {
        if (browse != null) browse.onClick.AddListener(OnBrowseButtonClicked);
        if (host != null) host.onClick.AddListener(OnHostButtonClicked);
        if (options != null) options.onClick.AddListener(OnOptionsButtonClicked);
        if (customize != null) customize.onClick.AddListener(OnCustomizeButtonClicked);
        if (quit != null) quit.onClick.AddListener(OnQuitButtonClicked);
    }

    void OnDisable()
    {
        if (browse != null) browse.onClick.RemoveListener(OnBrowseButtonClicked);
        if (host != null) host.onClick.RemoveListener(OnHostButtonClicked);
        if (options != null) options.onClick.RemoveListener(OnOptionsButtonClicked);
        if (customize != null) customize.onClick.RemoveListener(OnCustomizeButtonClicked);
        if (quit != null) quit.onClick.RemoveListener(OnQuitButtonClicked);
    }
    private void OnBrowseButtonClicked()
    {
        if (serverBrowser == null) return;

        serverBrowser.SetState(true);
    }
    private void OnHostButtonClicked()
    {
        if (lobbyView == null) return;

        lobbyView.CreateLobby();
    }

    private void OnOptionsButtonClicked()
    {
        if (settingsMenu != null)
        {
            settingsMenu.SetState(true);    
        }
    }

    public void JoinLobby(LobbyData lobby)
    {
        lobbyView.JoinLobby(lobby);
    }

    private void OnCustomizeButtonClicked()
    {
        LoadoutManager.Instance.SetState(true);
    }

    private void OnQuitButtonClicked()
    {
        Application.Quit();
    }
}
