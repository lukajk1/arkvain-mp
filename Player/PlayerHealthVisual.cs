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
        if (_healthSlider != null)
        {
            _healthSlider.value = (float)currentHealth / maxHealth;
        }
    }
}
