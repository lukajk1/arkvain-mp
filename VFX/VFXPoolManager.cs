using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Centralized VFX pooling system.
/// Each unique prefab gets its own dedicated pool.
/// Weapon visuals and other components register their VFX prefabs at startup.
/// </summary>
public class VFXPoolManager : MonoBehaviour
{
    public static VFXPoolManager Instance { get; private set; }

    [Header("Pool Settings")]
    [SerializeField] private int _defaultInitialCapacity = 5;
    [SerializeField] private int _defaultMaxSize = 30;

    // Dictionary maps prefab to its dedicated pool
    private Dictionary<GameObject, ObjectPool<GameObject>> _pools = new Dictionary<GameObject, ObjectPool<GameObject>>();

    // Track spawned objects so we can return them to the correct pool
    private Dictionary<GameObject, GameObject> _spawnedToPrefab = new Dictionary<GameObject, GameObject>();

    // Per-prefab simulation speeds
    private Dictionary<GameObject, float> _simSpeeds = new Dictionary<GameObject, float>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Register a prefab and create a pool for it if it doesn't exist.
    /// Called by weapon visuals and other components during initialization.
    /// </summary>
    public void RegisterPrefab(GameObject prefab, int initialCapacity = -1, int maxSize = -1, float simSpeed = 1f)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[VFXPoolManager] Attempted to register null prefab.");
            return;
        }

        if (_pools.ContainsKey(prefab))
            return; // Already registered

        _simSpeeds[prefab] = simSpeed;

        int capacity = initialCapacity > 0 ? initialCapacity : _defaultInitialCapacity;
        int max = maxSize > 0 ? maxSize : _defaultMaxSize;

        var pool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(prefab),
            actionOnGet: (obj) =>
            {
                obj.SetActive(true);
                _spawnedToPrefab[obj] = prefab;

                // Reset all particle systems in hierarchy
                float speed = _simSpeeds.TryGetValue(prefab, out float s) ? s : 1f;
                ParticleSystem[] particleSystems = obj.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in particleSystems)
                {
                    ps.Clear(); // Clear existing particles
                    ps.time = 0f; // Reset simulation time
                    var main = ps.main;
                    main.simulationSpeed = speed;
                }

                // Reset all trail renderers in hierarchy
                TrailRenderer[] trailRenderers = obj.GetComponentsInChildren<TrailRenderer>();
                foreach (var trail in trailRenderers)
                {
                    trail.Clear(); // Clear trail positions to prevent snapping
                    trail.emitting = true; // Enable emission for use
                }
            },
            actionOnRelease: (obj) =>
            {
                // Stop and clear all particle systems before deactivating
                ParticleSystem[] particleSystems = obj.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in particleSystems)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }

                // Stop all trail renderers from emitting
                TrailRenderer[] trailRenderers = obj.GetComponentsInChildren<TrailRenderer>();
                foreach (var trail in trailRenderers)
                {
                    trail.emitting = false; // Disable emission when returning to pool
                }

                obj.SetActive(false);
                _spawnedToPrefab.Remove(obj);
            },
            actionOnDestroy: (obj) => Destroy(obj),
            collectionCheck: false,
            defaultCapacity: capacity,
            maxSize: max
        );

        _pools.Add(prefab, pool);
        //Debug.Log($"[VFXPoolManager] Registered pool for prefab: {prefab.name} (capacity: {capacity}, max: {max})");
    }

    /// <summary>
    /// Spawn a VFX from the pool for this prefab.
    /// Auto-returns to pool when ParticleSystem finishes playing.
    /// </summary>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[VFXPoolManager] Attempted to spawn null prefab.");
            return null;
        }

        if (!_pools.ContainsKey(prefab))
        {
            Debug.LogWarning($"[VFXPoolManager] Prefab {prefab.name} not registered. Registering now with default settings.");
            RegisterPrefab(prefab);
        }

        GameObject obj = _pools[prefab].Get();
        obj.transform.SetPositionAndRotation(position, rotation);

        // Auto-return based on ParticleSystem duration
        if (obj.TryGetComponent<ParticleSystem>(out var ps))
        {
            ps.Play();
            StartCoroutine(ReturnWhenStopped(obj, prefab));
        }
        else
        {
            Debug.LogWarning($"[VFXPoolManager] Spawned object {obj.name} has no ParticleSystem. It will not auto-return to pool.");
        }

        return obj;
    }

    /// <summary>
    /// Manually return an object to its pool.
    /// Usually not needed as ParticleSystems auto-return when finished.
    /// </summary>
    public void Return(GameObject obj)
    {
        if (obj == null) return;

        if (_spawnedToPrefab.TryGetValue(obj, out GameObject prefab))
        {
            if (_pools.ContainsKey(prefab))
            {
                _pools[prefab].Release(obj);
            }
        }
        else
        {
            Debug.LogWarning($"[VFXPoolManager] Attempted to return object {obj.name} that wasn't spawned from pool.");
        }
    }

    private IEnumerator ReturnWhenStopped(GameObject obj, GameObject prefab)
    {
        // Get the root particle system (the "master" controller)
        var rootPS = obj.GetComponent<ParticleSystem>();

        if (rootPS != null)
        {
            // Wait until the root particle system finishes playing
            // This automatically waits for all child particle systems as well
            yield return new WaitWhile(() => rootPS.isPlaying);

            if (_pools.ContainsKey(prefab))
            {
                _pools[prefab].Release(obj);
            }
        }
        else
        {
            // If no root particle system, check all children
            ParticleSystem[] allPS = obj.GetComponentsInChildren<ParticleSystem>();
            if (allPS.Length > 0)
            {
                // Wait until ALL particle systems finish
                yield return new WaitWhile(() =>
                {
                    foreach (var ps in allPS)
                    {
                        if (ps.isPlaying) return true;
                    }
                    return false;
                });

                if (_pools.ContainsKey(prefab))
                {
                    _pools[prefab].Release(obj);
                }
            }
            else
            {
                Debug.LogWarning($"[VFXPoolManager] No ParticleSystems found on {obj.name}. Cannot auto-return.");
            }
        }
    }
}
