using NUnit.Framework;
using System.Collections.Generic;
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

    [Header("SFX")]
    [SerializeField] private List<AudioClip> hurtSFX;
    [SerializeField] private float _hurtSFXCooldown = 0.3f;

    private float _previousHealthPercent = 1f;
    private int _previousBreakpoint = -1;
    private float _lastHurtSFXTime = -999f;

    private void Start()
    {
        ScreenspaceEffectManager.SetScreenDamage(0f);

        if (_playerHealth != null)
        {
            // Initialize with current health to avoid false damage flash on spawn
            var state = _playerHealth.viewState;
            _previousHealthPercent = (float)state.health / _playerHealth._maxHealth;
            _previousBreakpoint = Mathf.FloorToInt((1f - _previousHealthPercent) * _healthBreakpoints);

            // Set initial slider value
            if (_healthSlider != null)
            {
                _healthSlider.value = _previousHealthPercent;
            }

            _playerHealth._onHealthChanged.AddListener(OnHealthChanged);
        }
    }

    private void OnDestroy()
    {
        if (_playerHealth != null)
        {
            _playerHealth._onHealthChanged.RemoveListener(OnHealthChanged);
        }
    }

    /// <summary>
    /// Updates the health bar visual based on current health percentage.
    /// </summary>
    private void OnHealthChanged((int health, int maxHealth) values)
    {
        int currentHealth = values.health;
        int maxHealth = values.maxHealth;
        float newHealthPercent = (float)currentHealth / maxHealth;

        // Update HUD readout (always, for owner)
        if (_playerHealth.isOwner)
        {
            HUDManager.Instance?.SetHealthReadout(currentHealth, maxHealth);
        }

        // Determine if this is damage or healing
        bool isInitializing = _previousBreakpoint == -1;
        bool isDamage = !isInitializing && newHealthPercent < _previousHealthPercent;
        bool isHealing = !isInitializing && newHealthPercent > _previousHealthPercent;

        // Handle damage effects (owner only)
        if (_playerHealth.isOwner && isDamage)
        {
            HandleDamageEffects(newHealthPercent);
        }

        // Update health bar slider
        UpdateHealthBarSlider(newHealthPercent, isDamage);

        // Store for next comparison
        _previousHealthPercent = newHealthPercent;
    }

    /// <summary>
    /// Handles screen flash and sound effects for taking damage.
    /// </summary>
    private void HandleDamageEffects(float newHealthPercent)
    {
        bool lowHealth = newHealthPercent < _thresholdToShowSSDamage;
        ScreenspaceEffectManager.FlashScreenDamage(lowHealth);

        if (hurtSFX != null && hurtSFX.Count > 0 && Time.time - _lastHurtSFXTime >= _hurtSFXCooldown)
        {
            _lastHurtSFXTime = Time.time;
            var clip = hurtSFX[Random.Range(0, hurtSFX.Count)];
            SoundManager.Play(new SoundData(clip, pitch: 0.4f));
        }
    }

    /// <summary>
    /// Updates the health bar slider and creates damage flash animations.
    /// </summary>
    private void UpdateHealthBarSlider(float newHealthPercent, bool isDamage)
    {
        if (_healthSlider == null) return;

        // Calculate current breakpoint (0 = 100%, 1 = 90%, 2 = 80%, etc.)
        int currentBreakpoint = Mathf.FloorToInt((1f - newHealthPercent) * _healthBreakpoints);

        // Initialize previous breakpoint on first update
        if (_previousBreakpoint == -1)
        {
            _previousBreakpoint = currentBreakpoint;
            _healthSlider.value = newHealthPercent;
            return;
        }

        // Create damage flash animations if we crossed breakpoints
        if (isDamage && currentBreakpoint > _previousBreakpoint && _damageFlashPrefab != null)
        {
            int breakpointsCrossed = currentBreakpoint - _previousBreakpoint;

            for (int i = 0; i < breakpointsCrossed; i++)
            {
                int targetBreakpoint = _previousBreakpoint + i + 1;
                float segmentEnd = 1f - (targetBreakpoint / (float)_healthBreakpoints);
                float segmentStart = 1f - ((targetBreakpoint - 1) / (float)_healthBreakpoints);
                float delay = i * _cascadeDelay;

                CreateDamageFlashDelayed(segmentStart, segmentEnd, delay);
            }
        }

        // Always update slider value (handles both damage and healing)
        _previousBreakpoint = currentBreakpoint;
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
