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
    [SerializeField] private TMP_Text flavorText;
    [SerializeField] private Slider progressSlider;

    [Header("Content")]
    [SerializeField] private LoadingTipsSO tipsData;

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

    public void LoadGame(string coreSceneName, string mapInternalName)
    {
        _isSceneLoaded = false;
        _isNetworkReady = false;
        
        StopAllCoroutines();
        StartCoroutine(GameLoadRoutine(coreSceneName, mapInternalName));
    }

    private IEnumerator GameLoadRoutine(string coreSceneName, string mapInternalName)
    {
        UpdateTips();

        if (loadingCanvas != null)
            loadingCanvas.gameObject.SetActive(true);

        if (progressSlider != null)
            progressSlider.value = 0f;

        // Phase 1: Core Scene Loading
        if (statusText != null)
            statusText.text = "Loading Core Systems...";

        AsyncOperation coreOp = SceneManager.LoadSceneAsync(coreSceneName);
        while (!coreOp.isDone)
        {
            if (progressSlider != null)
                progressSlider.value = (coreOp.progress / 0.9f) * 0.4f; // First 40%
            yield return null;
        }

        // Phase 2: Wait for MapLoader to initialize in the new scene
        if (statusText != null)
            statusText.text = "Initializing Map Loader...";

        while (MapLoader.Instance == null)
        {
            yield return null;
        }

        // Phase 3: Additive Map Loading
        if (statusText != null)
            statusText.text = $"Loading Map: {mapInternalName}...";

        MapLoader.Instance.LoadMap(mapInternalName);

        while (MapLoader.Instance.IsLoading || MapLoader.Instance.CurrentMapData == null)
        {
            // Fill from 40% to 80% while map loads
            if (progressSlider != null && progressSlider.value < 0.8f)
                progressSlider.value = Mathf.MoveTowards(progressSlider.value, 0.8f, Time.deltaTime * 0.2f);
            yield return null;
        }

        _isSceneLoaded = true;

        // Phase 4: Wait for Network Registration
        if (statusText != null)
            statusText.text = "Waiting for response from host...";

        while (NetworkManager.main == null || NetworkManager.main.localPlayer == default)
        {
            if (progressSlider != null && progressSlider.value < 1.0f)
                progressSlider.value = Mathf.MoveTowards(progressSlider.value, 1.0f, Time.deltaTime * 0.1f);
            
            yield return null;
        }

        if (progressSlider != null)
            progressSlider.value = 1.0f;

        if (statusText != null)
            statusText.text = "Readying...";

        yield return new WaitForSeconds(0.5f);

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
        UpdateTips();

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

    private void UpdateTips()
    {
        if (flavorText == null || tipsData == null) return;
        
        flavorText.text = tipsData.GetRandomAny();
    }
}
