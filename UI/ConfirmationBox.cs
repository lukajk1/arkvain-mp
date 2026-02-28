using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Confirmation dialog box that can be instantiated with custom confirm/cancel callbacks.
/// Instantiate into an existing canvas.
/// </summary>
public class ConfirmationBox : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private TMP_Text _confirmButtonText;
    [SerializeField] private TMP_Text _cancelButtonText;

    private Action _onConfirm;
    private Action _onCancel;

    private void Start()
    {
        // Subscribe to button clicks
        if (_confirmButton != null)
            _confirmButton.onClick.AddListener(OnConfirmClicked);

        if (_cancelButton != null)
            _cancelButton.onClick.AddListener(OnCancelClicked);
    }

    private void OnDestroy()
    {
        // Unsubscribe from button clicks
        if (_confirmButton != null)
            _confirmButton.onClick.RemoveListener(OnConfirmClicked);

        if (_cancelButton != null)
            _cancelButton.onClick.RemoveListener(OnCancelClicked);
    }

    /// <summary>
    /// Initialize the confirmation box with callbacks and optional text customization
    /// </summary>
    /// <param name="onConfirm">Callback when confirm button is clicked</param>
    /// <param name="onCancel">Callback when cancel button is clicked (optional)</param>
    /// <param name="message">Message text (optional)</param>
    /// <param name="confirmText">Confirm button text (optional, defaults to "Confirm")</param>
    /// <param name="cancelText">Cancel button text (optional, defaults to "Cancel")</param>
    public void Initialize(
        Action onConfirm,
        Action onCancel = null,
        string message = null,
        string confirmText = "Confirm",
        string cancelText = "Cancel")
    {
        _onConfirm = onConfirm;
        _onCancel = onCancel;

        if (_messageText != null && !string.IsNullOrEmpty(message))
            _messageText.text = message;

        if (_confirmButtonText != null)
            _confirmButtonText.text = confirmText;

        if (_cancelButtonText != null)
            _cancelButtonText.text = cancelText;
    }

    private void OnConfirmClicked()
    {
        _onConfirm?.Invoke();
        Destroy(gameObject);
    }

    private void OnCancelClicked()
    {
        _onCancel?.Invoke();
        Destroy(gameObject);
    }
}
