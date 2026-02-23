using UnityEngine;
using Animancer;
using PurrNet.Prediction;

/// <summary>
/// Non-generic base class for weapon visuals.
/// Provides common functionality for showing/hiding viewmodels.
/// </summary>
public abstract class WeaponVisualBase : MonoBehaviour
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
            Debug.Log($"[{GetType().Name}] Showing viewmodel: {_viewmodel.name}");
            _viewmodel.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"[{GetType().Name}] Cannot show viewmodel - _viewmodel reference is null!");
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

/// <summary>
/// Generic base class for weapon visual components.
/// Handles showing/hiding the weapon viewmodel and provides common animation/audio functionality.
/// Automatically subscribes to weapon logic events.
/// </summary>
public abstract class WeaponVisual<TLogic> : WeaponVisualBase where TLogic : IWeaponLogic
{
    [Header("References")]
    [SerializeField] protected TLogic _weaponLogic;

    [Header("Animation (Optional)")]
    [SerializeField] protected AnimancerComponent _animancer;
    [SerializeField] protected AnimationClip _equipClip;
    [SerializeField] protected AnimationClip _idleClip;
    [SerializeField] protected AnimationClip _shootClip;
    [SerializeField] protected AnimationClip _reloadClip;

    [Header("Animation Settings")]
    [SerializeField] protected float _defaultFadeDuration = 0.25f;
    [SerializeField] protected float _shootFadeDuration = 0.1f;

    protected AnimancerState _currentState;

    protected virtual void OnEnable()
    {
        if (_weaponLogic == null)
        {
            UnityEngine.Debug.LogError($"[{GetType().Name}] Weapon logic reference is null!");
            return;
        }

        // Subscribe to common events
        _weaponLogic.OnShoot += OnShoot;
        _weaponLogic.OnHit += OnHit;
        _weaponLogic.OnEquipped += OnEquipped;
        _weaponLogic.OnHolstered += OnHolstered;

        // Subscribe to reload events if available (optional)
        if (_weaponLogic is IReloadableWeaponLogic reloadableLogic)
        {
            reloadableLogic.onReload += OnReload;
            reloadableLogic.onReloadComplete += OnReloadComplete;
        }
    }

    protected virtual void OnDisable()
    {
        if (_weaponLogic == null) return;

        // Unsubscribe from common events
        _weaponLogic.OnShoot -= OnShoot;
        _weaponLogic.OnHit -= OnHit;
        _weaponLogic.OnEquipped -= OnEquipped;
        _weaponLogic.OnHolstered -= OnHolstered;

        // Unsubscribe from reload events if available
        if (_weaponLogic is IReloadableWeaponLogic reloadableLogic)
        {
            reloadableLogic.onReload -= OnReload;
            reloadableLogic.onReloadComplete -= OnReloadComplete;
        }
    }

    /// <summary>
    /// Plays the equip animation, then transitions to idle.
    /// Override to add custom equip behavior.
    /// </summary>
    protected virtual void PlayEquipAnimation()
    {
        if (_animancer != null && _equipClip != null)
        {
            var equipState = _animancer.Play(_equipClip, _defaultFadeDuration);
            equipState.Events(this).OnEnd = PlayIdleAnimation;
        }
        else
        {
            PlayIdleAnimation();
        }
    }

    /// <summary>
    /// Plays the idle animation in a loop.
    /// Override to add custom idle behavior.
    /// </summary>
    protected virtual void PlayIdleAnimation()
    {
        if (_animancer != null && _idleClip != null)
        {
            _currentState = _animancer.Play(_idleClip, _defaultFadeDuration);
        }
    }

    /// <summary>
    /// Plays the shoot animation, then returns to idle.
    /// Override to add custom shoot behavior.
    /// </summary>
    protected virtual void PlayShootAnimation()
    {
        if (_animancer != null && _shootClip != null)
        {
            var shootState = _animancer.Play(_shootClip, _shootFadeDuration);

            // Set up end event to return to idle
            shootState.Events(this).OnEnd = PlayIdleAnimation;
        }
    }

    /// <summary>
    /// Plays the reload animation, then returns to idle.
    /// Override to add custom reload behavior.
    /// </summary>
    protected virtual void PlayReloadAnimation()
    {
        if (_animancer != null && _reloadClip != null)
        {
            var reloadState = _animancer.Play(_reloadClip, _defaultFadeDuration);

            // Set up end event to return to idle
            reloadState.Events(this).OnEnd = PlayIdleAnimation;
        }
    }

    /// <summary>
    /// Stops all animations.
    /// Called when weapon is holstered.
    /// </summary>
    protected virtual void StopAllAnimations()
    {
        if (_animancer != null)
        {
            _animancer.Stop();
        }
    }

    // ===== Virtual event handlers - override in derived classes to customize =====

    /// <summary>
    /// Called when the weapon shoots. Override to add custom shoot behavior.
    /// </summary>
    protected virtual void OnShoot(Vector3 fireDirection)
    {
        PlayShootAnimation();
    }

    /// <summary>
    /// Called when the weapon hits something. Override to add custom hit behavior.
    /// </summary>
    protected virtual void OnHit(HitInfo hitInfo)
    {
        // Default: no behavior. Override in derived class.
    }

    /// <summary>
    /// Called when the weapon is equipped. Override to add custom equip behavior.
    /// </summary>
    protected virtual void OnEquipped()
    {
        PlayEquipAnimation();
    }

    /// <summary>
    /// Called when the weapon is holstered. Override to add custom holster behavior.
    /// </summary>
    protected virtual void OnHolstered()
    {
        StopAllAnimations();
    }

    /// <summary>
    /// Called when the weapon starts reloading. Override to add custom reload behavior.
    /// </summary>
    protected virtual void OnReload()
    {
        PlayReloadAnimation();
    }

    /// <summary>
    /// Called when the weapon finishes reloading. Override to add custom reload complete behavior.
    /// </summary>
    protected virtual void OnReloadComplete()
    {
        // Default: no behavior. Override in derived class.
    }
}
