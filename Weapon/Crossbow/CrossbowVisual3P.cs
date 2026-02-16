using UnityEngine;

/// <summary>
/// Handles 3rd person visual feedback for the crossbow.
/// Responsible for muzzle flash, bolt projectile, and environment hit effects.
/// </summary>
public class CrossbowVisual3P : MonoBehaviour
{
    [SerializeField] private CrossbowLogic _weaponLogic;

    [Header("Shoot Effects")]
    [SerializeField] private ParticleSystem _muzzleFlashParticles;
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private Transform diegeticMuzzlePosition;
    [SerializeField] private float _bulletMaxDistance = 100f;
    [SerializeField] private float _bulletSpeed = 100f;

    [SerializeField] private ParticleSystem _envHitParticles;

    private Coroutine _activeBulletCoroutine;
    private GameObject _activeBulletObject;

    private void Awake()
    {
        if (_envHitParticles != null)
            VFXPoolManager.Instance.RegisterPrefab(_envHitParticles.gameObject);

        if (_bulletPrefab != null)
            VFXPoolManager.Instance.RegisterPrefab(_bulletPrefab, initialCapacity: 30, maxSize: 50);

        if (_muzzleFlashParticles != null && diegeticMuzzlePosition != null)
        {
            _muzzleFlashParticles = Instantiate(_muzzleFlashParticles);
            _muzzleFlashParticles.transform.position = diegeticMuzzlePosition.position;
            _muzzleFlashParticles.transform.rotation = diegeticMuzzlePosition.rotation;
            _muzzleFlashParticles.Stop();
            _muzzleFlashParticles.Clear();
        }
    }

    private void OnEnable()
    {
        if (_weaponLogic == null)
        {
            Debug.LogError($"[CrossbowVisual3P] Weapon logic reference is null!");
            return;
        }

        _weaponLogic.OnShoot += OnShoot;
        _weaponLogic.OnHit += OnHit;
    }

    private void OnDisable()
    {
        if (_weaponLogic == null) return;

        _weaponLogic.OnShoot -= OnShoot;
        _weaponLogic.OnHit -= OnHit;
    }

    private void OnShoot(Vector3 fireDirection)
    {
        if (_muzzleFlashParticles != null)
        {
            _muzzleFlashParticles.transform.position = diegeticMuzzlePosition.position;
            _muzzleFlashParticles.transform.rotation = diegeticMuzzlePosition.rotation;
            _muzzleFlashParticles.Clear();
            _muzzleFlashParticles.Play(true);
            _muzzleFlashParticles.Emit(10);
        }

        if (_bulletPrefab != null && diegeticMuzzlePosition != null && VFXPoolManager.Instance != null)
        {
            Vector3 startPos = diegeticMuzzlePosition.position;
            Vector3 endPos = startPos + fireDirection * _bulletMaxDistance;
            Quaternion rotation = Quaternion.LookRotation(fireDirection);
            GameObject bulletObj = VFXPoolManager.Instance.Spawn(_bulletPrefab, startPos, rotation);

            if (bulletObj != null)
            {
                _activeBulletObject = bulletObj;
                _activeBulletCoroutine = StartCoroutine(AnimateBulletToHit(bulletObj, startPos, endPos));
            }
        }
    }

    private void OnHit(HitInfo hitInfo)
    {
        if (VFXPoolManager.Instance != null && _envHitParticles != null && !hitInfo.hitPlayer)
        {
            Quaternion rotation = Quaternion.LookRotation(hitInfo.surfaceNormal);
            VFXPoolManager.Instance.Spawn(_envHitParticles.gameObject, hitInfo.position, rotation);
        }

    }

    private System.Collections.IEnumerator AnimateBulletToHit(GameObject bulletObj, Vector3 startPos, Vector3 endPos)
    {
        float distance = Vector3.Distance(startPos, endPos);
        float duration = distance / _bulletSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            bulletObj.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        bulletObj.transform.position = endPos;

        if (VFXPoolManager.Instance != null)
            VFXPoolManager.Instance.Return(bulletObj);

        _activeBulletCoroutine = null;
        _activeBulletObject = null;
    }
}
