using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles visual representation of player health.
/// Subscribes to PlayerHealth events and updates UI accordingly.
/// </summary>
public class PlayerHealthVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Image _damageFlashPrefab;

    [Header("Screen Effects")]
    [SerializeField] private float _thresholdToShowSSDamage = 0.4f;

    [Header("Healthbar Tick Down")]
    [SerializeField] private Transform _damageFlashContainer;
    [SerializeField] private float _damageFlashScaleY = 2f;
    [SerializeField] private float _damageFlashDuration = 0.3f;
    [SerializeField] private int _healthBreakpoints = 10;
    [SerializeField] private float _cascadeDelay = 0.05f;

    private float _previousHealthPercent = 1f;
    private int _previousBreakpoint = -1;

    private void Awake()
    {
        if (_healthSlider != null)
        {
            _healthSlider.value = 1f;
        }
    }

    private void OnEnable()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged += OnHealthChanged;
            Debug.Log("[PlayerHealthVisual] Subscribed to OnHealthChanged");
        }
        else
        {
            Debug.LogWarning("[PlayerHealthVisual] _playerHealth is null in OnEnable — reference not assigned");
        }
    }

    private void OnDisable()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged -= OnHealthChanged;
        }
    }

    /// <summary>
    /// Updates the health bar visual based on current health percentage.
    /// </summary>
    private void OnHealthChanged(int currentHealth, int maxHealth)
    {
        float newHealthPercent = (float)currentHealth / maxHealth;
        //Debug.Log($"[PlayerHealthVisual] UpdateHealthBar called — health: {currentHealth}/{maxHealth}, isOwner: {_playerHealth.isOwner}");

        if (_playerHealth.isOwner)
        {
            bool lowHealth = newHealthPercent < _thresholdToShowSSDamage;
            ScreenspaceEffectManager.FlashScreenDamage(lowHealth);
            HUDManager.Instance?.SetHealthReadout(currentHealth, maxHealth);
        }

        if (_healthSlider == null) return;

        // Calculate current breakpoint (0 = 100%, 1 = 90%, 2 = 80%, etc.)
        int currentBreakpoint = Mathf.FloorToInt((1f - newHealthPercent) * _healthBreakpoints);

        // Initialize previous breakpoint on first update
        if (_previousBreakpoint == -1)
        {
            _previousBreakpoint = currentBreakpoint;
        }

        // Check if we crossed into a new breakpoint (took enough damage)
        if (currentBreakpoint > _previousBreakpoint && _damageFlashPrefab != null)
        {
            // Calculate how many breakpoints were crossed
            int breakpointsCrossed = currentBreakpoint - _previousBreakpoint;

            // Trigger a flash for each breakpoint crossed, with cascading delay
            for (int i = 0; i < breakpointsCrossed; i++)
            {
                int targetBreakpoint = _previousBreakpoint + i + 1;
                float segmentEnd = 1f - (targetBreakpoint / (float)_healthBreakpoints);
                float segmentStart = 1f - ((targetBreakpoint - 1) / (float)_healthBreakpoints);
                float delay = i * _cascadeDelay;

                CreateDamageFlashDelayed(segmentStart, segmentEnd, delay);
            }
        }

        _previousBreakpoint = currentBreakpoint;
        _previousHealthPercent = newHealthPercent;
        _healthSlider.value = newHealthPercent;
    }

    /// <summary>
    /// Creates a damage flash with a delay.
    /// </summary>
    private void CreateDamageFlashDelayed(float previousPercent, float currentPercent, float delay)
    {
        if (delay > 0)
        {
            LeanTween.delayedCall(gameObject, delay, () => CreateDamageFlash(previousPercent, currentPercent));
        }
        else
        {
            CreateDamageFlash(previousPercent, currentPercent);
        }
    }

    /// <summary>
    /// Creates a visual flash showing the health lost between previous and current health.
    /// </summary>
    private void CreateDamageFlash(float previousPercent, float currentPercent)
    {
        // Get the fill RectTransform from the slider
        RectTransform fillRect = _healthSlider.fillRect;
        if (fillRect == null) return;

        // Instantiate the damage flash image
        Transform parent = _damageFlashContainer != null ? _damageFlashContainer : fillRect.parent;
        Image damageFlash = Instantiate(_damageFlashPrefab, parent);
        RectTransform flashRect = damageFlash.rectTransform;

        // Copy the fill rect's anchors and settings
        flashRect.anchorMin = fillRect.anchorMin;
        flashRect.anchorMax = fillRect.anchorMax;
        flashRect.anchoredPosition = fillRect.anchoredPosition;
        flashRect.sizeDelta = fillRect.sizeDelta;
        flashRect.pivot = fillRect.pivot;

        // Set the flash to span from currentPercent to previousPercent
        flashRect.anchorMin = new Vector2(currentPercent, flashRect.anchorMin.y);
        flashRect.anchorMax = new Vector2(previousPercent, flashRect.anchorMax.y);
        flashRect.offsetMin = new Vector2(0, flashRect.offsetMin.y);
        flashRect.offsetMax = new Vector2(0, flashRect.offsetMax.y);

        // Animate: scale Y up and fade out
        Vector3 targetScale = new Vector3(flashRect.localScale.x, _damageFlashScaleY, flashRect.localScale.z);
        LeanTween.scale(flashRect, targetScale, _damageFlashDuration)
            .setEase(LeanTweenType.easeOutCubic);

        LeanTween.alpha(flashRect, 0f, _damageFlashDuration)
            .setEase(LeanTweenType.easeOutCubic)
            .setOnComplete(() => Destroy(damageFlash.gameObject));

        //Debug.Log($"[PlayerHealthVisual] Created damage flash from {previousPercent:F2} to {currentPercent:F2}");
    }
}
