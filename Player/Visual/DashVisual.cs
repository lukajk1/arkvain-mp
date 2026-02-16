using UnityEngine;

public class DashVisual : MonoBehaviour
{
    [SerializeField] private DashAbility _dashAbility;
    [SerializeField] private ParticleSystem _dashParticles;
    [SerializeField] private float _particleSystemPlaybackSpeed = 1.5f;

    private bool _subscribed;

    private void Awake()
    {
        if (_dashParticles != null)
        {
            _dashParticles = Instantiate(_dashParticles);
            _dashParticles.transform.SetParent(transform.root, worldPositionStays: true);
            _dashParticles.Stop();
            _dashParticles.Clear();
            var main = _dashParticles.main;
            main.simulationSpeed = _particleSystemPlaybackSpeed;

            VFXPoolManager.Instance.RegisterPrefab(_dashParticles.gameObject);
        }
    }

    private void Update()
    {
        if (!_subscribed && _dashAbility != null && _dashAbility._onDash != null)
        {
            _dashAbility._onDash.AddListener(OnDash);
            _subscribed = true;
        }
    }

    private void OnDisable()
    {
        if (_subscribed && _dashAbility != null && _dashAbility._onDash != null)
        {
            _dashAbility._onDash.RemoveListener(OnDash);
            _subscribed = false;
        }
    }

    private void OnDash(Vector3 origin)
    {
        if (_dashParticles == null || VFXPoolManager.Instance == null) return;
        VFXPoolManager.Instance.Spawn(_dashParticles.gameObject, origin, Quaternion.identity);
    }
}
