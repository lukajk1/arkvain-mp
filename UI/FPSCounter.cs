using UnityEngine;
using TMPro;

/// <summary>
/// FPS counter using linear interpolation (low-pass filter) for smooth display
/// </summary>
public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _fpsText;
    [SerializeField] private string _format = "FPS: {0:F1}"; // Display format
    [SerializeField, Range(0.01f, 0.99f)] private float _smoothing = 0.9f; // Higher = smoother but slower to react

    private float _averageFPS;
    private bool _initialized;

    private void Update()
    {
        float currentFPS = 1f / Time.unscaledDeltaTime;

        // Initialize on first frame
        if (!_initialized)
        {
            _averageFPS = currentFPS;
            _initialized = true;
        }
        else
        {
            // Low-pass filter: blend current FPS with previous average
            _averageFPS = (_averageFPS * _smoothing) + (currentFPS * (1f - _smoothing));
        }

        if (_fpsText != null)
        {
            _fpsText.text = string.Format(_format, _averageFPS);
        }
    }
}
