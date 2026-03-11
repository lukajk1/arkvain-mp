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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (loadingCanvas != null)
            loadingCanvas.gameObject.SetActive(false);
    }

    /// <summary>
    /// Loads the core game loop and a specific map.
    /// Used when transitioning from Lobby to Match.
    /// </summary>
    public void LoadGame(string coreSceneName, string mapInternalName)
    {
        StopAllCoroutines();
        StartCoroutine(GameLoadRoutine(coreSceneName, mapInternalName));
    }

    /// <summary>
    /// Simple scene load with flavor text.
    /// Used for returning to menus or non-networked transitions.
    /// </summary>
    public void LoadScene(string sceneName)
    {
        StopAllCoroutines();
        StartCoroutine(SimpleLoadingRoutine(sceneName));
    }

    private IEnumerator GameLoadRoutine(string coreSceneName, string mapInternalName)
    {
        PrepareUI();

        // Phase 1: Core Scene Loading
        SetStatus("Loading Core Systems...");
        yield return StartCoroutine(AsyncLoad(coreSceneName, 0f, 0.4f));

        // Phase 2: Wait for MapLoader
        SetStatus("Initializing Map Loader...");
        while (MapLoader.Instance == null) yield return null;

        // Phase 3: Additive Map Loading
        SetStatus($"Loading Map: {mapInternalName}...");
        MapLoader.Instance.LoadMap(mapInternalName);

        while (MapLoader.Instance.IsLoading || MapLoader.Instance.CurrentMapData == null)
        {
            // Progress from 40% to 80%
            UpdateProgress(Mathf.MoveTowards(progressSlider.value, 0.8f, Time.deltaTime * 0.2f));
            yield return null;
        }

        // Phase 4: Network Registration
        SetStatus("Waiting for response from host...");
        yield return StartCoroutine(WaitForNetwork(0.8f, 1.0f));

        yield return StartCoroutine(FinishLoading());
    }

    private IEnumerator SimpleLoadingRoutine(string sceneName)
    {
        PrepareUI();

        SetStatus("Returning to Menu...");
        yield return StartCoroutine(AsyncLoad(sceneName, 0f, 1.0f));

        yield return StartCoroutine(FinishLoading());
    }

    private void PrepareUI()
    {
        UpdateTips();
        if (loadingCanvas != null) loadingCanvas.gameObject.SetActive(true);
        UpdateProgress(0f);
    }

    private IEnumerator AsyncLoad(string sceneName, float minProgress, float maxProgress)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
        {
            float normalized = Mathf.Clamp01(op.progress / 0.9f);
            UpdateProgress(Mathf.Lerp(minProgress, maxProgress, normalized));
            yield return null;
        }
    }

    private IEnumerator WaitForNetwork(float minProgress, float maxProgress)
    {
        while (NetworkManager.main == null || NetworkManager.main.localPlayer == default)
        {
            // Slow crawl to max progress to show activity
            UpdateProgress(Mathf.MoveTowards(progressSlider.value, maxProgress, Time.deltaTime * 0.1f));
            yield return null;
        }
        UpdateProgress(maxProgress);
    }

    private IEnumerator FinishLoading()
    {
        SetStatus("Readying...");
        UpdateProgress(1.0f);
        yield return new WaitForSeconds(0.5f);
        if (loadingCanvas != null) loadingCanvas.gameObject.SetActive(false);
    }

    private void SetStatus(string text)
    {
        if (statusText != null) statusText.text = text;
    }

    private void UpdateProgress(float value)
    {
        if (progressSlider != null) progressSlider.value = value;
    }

    private void UpdateTips()
    {
        if (flavorText == null || tipsData == null) return;
        flavorText.text = tipsData.GetRandomAny();
    }
}
