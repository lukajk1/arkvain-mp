using TMPro;
using UnityEngine;
using PurrNet.Prediction.StateMachine;

public class GameStateUI : MonoBehaviour
{
    public static GameStateUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private GameObject defeatScreen;
    [SerializeField] private TMP_Text countdownText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Hide screens initially
        if (victoryScreen != null) victoryScreen.SetActive(false);
        if (defeatScreen != null) defeatScreen.SetActive(false);
        if (countdownText != null) countdownText.text = "";
    }

    /// <summary>
    /// Updates the main status text (e.g., "Waiting for Players...")
    /// </summary>
    public void UpdateStatus(string message, bool visible = true)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Specifically updates the waiting status with a player count fraction.
    /// </summary>
    public void UpdateWaitingStatus(int current, int required)
    {
        UpdateStatus($"Waiting for players... ({current}/{required})");
    }

    /// <summary>
    /// Shows a countdown before the round starts
    /// </summary>
    public void ShowCountdown(int seconds)
    {
        if (countdownText != null)
        {
            countdownText.text = seconds > 0 ? seconds.ToString() : "GO!";
            countdownText.gameObject.SetActive(seconds >= 0);
        }
    }

    public void HideCountdown()
    {
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Shows the match result screen
    /// </summary>
    public void ShowMatchResult(bool isVictory)
    {
        if (victoryScreen != null) victoryScreen.SetActive(isVictory);
        if (defeatScreen != null) defeatScreen.SetActive(!isVictory);
        UpdateStatus(""); // Hide general status
    }

    public void HideAll()
    {
        UpdateStatus("", false);
        HideCountdown();
        if (victoryScreen != null) victoryScreen.SetActive(false);
        if (defeatScreen != null) defeatScreen.SetActive(false);
    }
}
