using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class NewMonoBehaviourScript : MonoBehaviour
{
    [SerializeField] private Button play;
    [SerializeField] private Button options;
    [SerializeField] private Button quit;
    #if UNITY_EDITOR
    [SerializeField] private SceneAsset playSceneAsset;
    #endif
    private string playSceneName;

    #if UNITY_EDITOR
    void OnValidate()
    {
        if (playSceneAsset != null)
        {
            playSceneName = playSceneAsset.name;
        }
    }
    #endif

    void OnEnable()
    {
        play.onClick.AddListener(OnPlayButtonClicked);
        options.onClick.AddListener(OnOptionsButtonClicked);
        quit.onClick.AddListener(OnQuitButtonClicked);
    }

    void OnDisable()
    {
        play.onClick.RemoveListener(OnPlayButtonClicked);
        options.onClick.RemoveListener(OnOptionsButtonClicked);
        quit.onClick.RemoveListener(OnQuitButtonClicked);
    }

    private void OnPlayButtonClicked()
    {
        SceneManager.LoadScene(playSceneName);
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
