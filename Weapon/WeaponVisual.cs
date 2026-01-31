using UnityEngine;

/// <summary>
/// Base class for weapon visual components.
/// Handles showing/hiding the weapon viewmodel and provides hooks for visual effects.
/// </summary>
public abstract class WeaponVisual : MonoBehaviour
{
    [Header("Viewmodel")]
    [SerializeField] protected GameObject _viewmodel;

    /// <summary>
    /// Shows the weapon viewmodel.
    /// </summary>
    public virtual void Show()
    {
        if (_viewmodel != null)
        {
            _viewmodel.SetActive(true);
        }
    }

    /// <summary>
    /// Hides the weapon viewmodel.
    /// </summary>
    public virtual void Hide()
    {
        if (_viewmodel != null)
        {
            _viewmodel.SetActive(false);
        }
    }
}
