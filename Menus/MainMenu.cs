using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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
    [SerializeField] private LobbyCreator lobbyCreator;
    [SerializeField] private ServerBrowser serverBrowser;
    

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
        if (lobbyCreator == null) return;

        lobbyCreator.SetState(true);
        lobbyCreator.CreateLobby();
    }

    private void OnOptionsButtonClicked()
    {
        // Handle options button click
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
