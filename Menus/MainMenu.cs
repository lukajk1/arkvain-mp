using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class NewMonoBehaviourScript : MonoBehaviour
{
    [SerializeField] private Button browse;
    [SerializeField] private Button host;
    [SerializeField] private Button options;
    [SerializeField] private Button quit;

    void OnEnable()
    {
        if (host != null) host.onClick.AddListener(OnHostButtonClicked);
        if (options != null) options.onClick.AddListener(OnOptionsButtonClicked);
        if (quit != null) quit.onClick.AddListener(OnQuitButtonClicked);
    }

    void OnDisable()
    {
        if (host != null) host.onClick.RemoveListener(OnHostButtonClicked);
        if (options != null) options.onClick.RemoveListener(OnOptionsButtonClicked);
        if (quit != null) quit.onClick.RemoveListener(OnQuitButtonClicked);
    }

    private void OnHostButtonClicked()
    {
    }

    private void OnOptionsButtonClicked()
    {
        // Handle options button click
    }

    private void OnQuitButtonClicked()
    {
        Application.Quit();
    }
}
