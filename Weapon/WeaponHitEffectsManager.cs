using UnityEngine;

/// <summary>
/// Centralized manager for weapon hit effects (particles).
/// All weapons use the same hit body and hit wall particle effects.
/// Lives in the scene as a singleton and integrates with VFXPoolManager.
/// </summary>
public class WeaponHitEffectsManager : MonoBehaviour
{
    public static WeaponHitEffectsManager Instance { get; private set; }

    [Header("Shared Hit Effect Prefabs")]
    [SerializeField] private GameObject _hitBodyParticles;
    [SerializeField] private GameObject _hitWallParticles;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Register prefabs with VFXPoolManager
        if (VFXPoolManager.Instance != null)
        {
            if (_hitBodyParticles != null)
                VFXPoolManager.Instance.RegisterPrefab(_hitBodyParticles);
            if (_hitWallParticles != null)
                VFXPoolManager.Instance.RegisterPrefab(_hitWallParticles);
        }
    }

    /// <summary>
    /// Plays the appropriate hit effect based on what was hit.
    /// Call this from weapon visual OnHit methods.
    /// </summary>
    /// <param name="hitInfo">Information about what was hit</param>
    /// <param name="isOwner">Whether the weapon belongs to the local player</param>
    public static void PlayHitEffect(HitInfo hitInfo, bool isOwner)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[WeaponHitEffectsManager] Instance is null. Make sure the manager exists in the scene.");
            return;
        }

        Instance.PlayHitEffectInternal(hitInfo, isOwner);
    }

    private void PlayHitEffectInternal(HitInfo hitInfo, bool isOwner)
    {
        if (VFXPoolManager.Instance == null) return;

        if (hitInfo.hitPlayer)
        {
            // Blood effects only for attacker
            if (isOwner && _hitBodyParticles != null)
            {
                VFXPoolManager.Instance.Spawn(_hitBodyParticles, hitInfo.position, Quaternion.identity);
            }
        }
        else
        {
            // Wall/environment effects for everyone, but only if close enough to local player
            if (Camera.main != null && _hitWallParticles != null)
            {
                float distanceSqr = (Camera.main.transform.position - hitInfo.position).sqrMagnitude;
                if (distanceSqr < ClientsideGameManager.maxVFXDistance * ClientsideGameManager.maxVFXDistance)
                {
                    // Orient the particle effect so its Z+ axis aligns with the surface normal
                    Quaternion rotation = Quaternion.LookRotation(hitInfo.surfaceNormal);
                    VFXPoolManager.Instance.Spawn(_hitWallParticles, hitInfo.position, rotation);
                }
            }
        }
    }
}
