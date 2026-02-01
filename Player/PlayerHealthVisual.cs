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

    [Header("Damage Flash Settings")]
    [SerializeField] private Transform _damageFlashContainer;
    [SerializeField] private float _damageFlashScaleY = 2f;
    [SerializeField] private float _damageFlashDuration = 0.3f;

    private float _previousHealthPercent = 1f;

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
            _playerHealth.OnHealthChanged += UpdateHealthBar;
        }
    }

    private void OnDisable()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged -= UpdateHealthBar;
        }
    }

    /// <summary>
    /// Updates the health bar visual based on current health percentage.
    /// </summary>
    private void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (_healthSlider == null) return;

        float newHealthPercent = (float)currentHealth / maxHealth;

        // Check if health decreased (took damage)
        if (newHealthPercent < _previousHealthPercent && _damageFlashPrefab != null)
        {
            CreateDamageFlash(_previousHealthPercent, newHealthPercent);
        }

        _previousHealthPercent = newHealthPercent;
        _healthSlider.value = newHealthPercent;
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

        Debug.Log($"[PlayerHealthVisual] Created damage flash from {previousPercent:F2} to {currentPercent:F2}");
    }
}
