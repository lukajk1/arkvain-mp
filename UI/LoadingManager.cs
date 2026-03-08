using PurrNet;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Canvas loadingCanvas;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Slider progressSlider;

    [Header("Settings")]
    [SerializeField] private float fadeSpeed = 2f;

    private bool _isSceneLoaded;
    private bool _isNetworkReady;
    private string _targetScene;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Ensure UI is hidden initially
        if (loadingCanvas != null)
            loadingCanvas.gameObject.SetActive(false);
    }

    public void LoadScene(string sceneName)
    {
        _targetScene = sceneName;
        _isSceneLoaded = false;
        _isNetworkReady = false;
        
        StopAllCoroutines();
        StartCoroutine(LoadingRoutine(sceneName));
    }

    private IEnumerator LoadingRoutine(string sceneName)
    {
        if (loadingCanvas != null)
            loadingCanvas.gameObject.SetActive(true);

        if (progressSlider != null)
            progressSlider.value = 0f;

        // Phase 1: Local Scene Loading
        if (statusText != null)
            statusText.text = "Loading Scene...";

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = true;

        while (!op.isDone)
        {
            // Unity progress goes 0 to 0.9, then jumps to 1.0 when activated
            float progress = Mathf.Clamp01(op.progress / 0.9f);
            if (progressSlider != null)
                progressSlider.value = progress * 0.8f; // Take up first 80% of bar
            
            yield return null;
        }

        _isSceneLoaded = true;

        // Phase 2: Wait for Network Registration
        if (statusText != null)
            statusText.text = "Waiting for response from host...";

        // Wait for NetworkManager to be ready and local player to be assigned
        while (NetworkManager.main == null || NetworkManager.main.localPlayer == default)
        {
            // Fill the remaining 20% slowly to show "activity"
            if (progressSlider != null && progressSlider.value < 1.0f)
                progressSlider.value = Mathf.MoveTowards(progressSlider.value, 1.0f, Time.deltaTime * 0.1f);
            
            yield return null;
        }

        if (progressSlider != null)
            progressSlider.value = 1.0f;

        if (statusText != null)
            statusText.text = "Readying...";

        yield return new WaitForSeconds(0.5f);

        // Cleanup
        if (loadingCanvas != null)
            loadingCanvas.gameObject.SetActive(false);
    }
}
