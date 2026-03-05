using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class NewMonoBehaviourScript : MonoBehaviour
{
    [Header("Elements")]
    [SerializeField] private Button browse;
    [SerializeField] private Button host;
    [SerializeField] private Button customize;
    [SerializeField] private Button options;
    [SerializeField] private Button quit;
    


    void OnEnable()
    {
        if (host != null) host.onClick.AddListener(OnHostButtonClicked);
        if (options != null) options.onClick.AddListener(OnOptionsButtonClicked);
        if (customize != null) customize.onClick.AddListener(OnCustomizeButtonClicked);
        if (quit != null) quit.onClick.AddListener(OnQuitButtonClicked);
    }

    void OnDisable()
    {
        if (host != null) host.onClick.RemoveListener(OnHostButtonClicked);
        if (options != null) options.onClick.RemoveListener(OnOptionsButtonClicked);
        if (customize != null) customize.onClick.RemoveListener(OnCustomizeButtonClicked);
        if (quit != null) quit.onClick.RemoveListener(OnQuitButtonClicked);
    }

    private void OnHostButtonClicked()
    {
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
