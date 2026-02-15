using UnityEngine;
using PurrNet;
using PurrNet.Prediction;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerVisualsManager : StatelessPredictedIdentity
{
    [SerializeField] private SkinnedMeshRenderer[] _skinnedMeshRenderers;
    [SerializeField] private MeshRenderer[] _meshRenderers;

    [SerializeField] private MeshRenderer _weaponViewmodel;
    [SerializeField] private MeshRenderer _weaponDiegetic;

    [Header("Canvas")]
    [SerializeField] private Canvas _healthCanvas;

    [Header("Camera")]
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private FirstPersonCamera _firstPersonCamera;
    [SerializeField] private GameObject _deadPlayerPrefab;

    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private PlayerManualMovement _playerMovement;

    [Header("Ability")]
    [SerializeField] private MonoBehaviour _abilityLogic;
    public IAbility _ability;

    [SerializeField] private bool showVelocity;

    [Header("Sound")]
    [SerializeField] private AudioClip _onJumpClip;
    [SerializeField] private AudioClip _onLandClip;
    [SerializeField] private List<AudioClip> _footstepClips;
    [SerializeField] private float _footstepInterval = 2.2f;

    private float _footstepDistance;

    private void OnEnable()
    {
        if (_playerHealth != null)
            _playerHealth.OnDeath += OnPlayerDeath;
    }

    private void OnDisable()
    {
        if (_playerHealth != null)
            _playerHealth.OnDeath -= OnPlayerDeath;
        if (_playerMovement != null)
        {
            _playerMovement._onJump.RemoveListener(OnJump);
            _playerMovement._onLand.RemoveListener(OnLand);
        }
    }

    private void OnPlayerDeath(PlayerID? playerId)
    {
        // should only fire if this instance is the local player--logic below this only pertains to this case
        if (!isOwner) return;
        //Debug.Log("WWW this shoudl run on one client");
        if (_deadPlayerPrefab != null)
            Instantiate(_deadPlayerPrefab, transform.position + Vector3.up, transform.rotation);
    }

    private void OnJump()
    {
        //Debug.Log("jump called");
        if (_onJumpClip != null)
            SoundManager.Play(new SoundData(_onJumpClip, varyPitch: false, varyVolume: false));
    }

    private void OnLand()
    {
        _footstepDistance = 0f;
        if (_onLandClip != null)
            SoundManager.Play(new SoundData(_onLandClip, varyPitch: false, varyVolume: false));
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        // Clear camera reference if we registered it
        if (isOwner && ClientGame._mainCamera == _mainCamera)
        {
            ClientGame._mainCamera = null;
            //Debug.Log("[VisualsManager] Cleared main camera reference on destroy");
        }
    }

    private void Start()
    {
        _ability = _abilityLogic as IAbility;
        if (_ability == null)
            HUDManager.Instance?.HideAbilityUI();
        else
        {
            HUDManager.Instance?.SetAbilityBindingName(InputManager.Instance.Player.UseAbility.GetBindingDisplayString());
        }
    }

    protected override void LateAwake()
    {
        base.LateAwake();

        if (_playerMovement != null)
        {
            _playerMovement._onJump.AddListener(OnJump);
            _playerMovement._onLand.AddListener(OnLand);
        }

        Debug.Log($"[VisualsManager] LateAwake - isOwner: {isOwner}, Owner: {owner}, ClientGame.Instance: {ClientGame.Instance != null}, MainCamera: {_mainCamera != null}, FirstPersonCamera: {_firstPersonCamera != null}");

        if (isOwner)
        {
            //Debug.Log("[VisualsManager] This is the owner, setting up local player visuals");

            // Disable mesh renderers for local player
            foreach (var renderer in _skinnedMeshRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }
            if (_weaponDiegetic != null) _weaponDiegetic.enabled = false;

            // disable health canvas
            _healthCanvas.gameObject.SetActive(false);

            // Initialize and register camera
            if (_firstPersonCamera != null)
            {
                _firstPersonCamera.Init();
                Debug.Log("[VisualsManager] FirstPersonCamera initialized");
            }
            else
            {
                Debug.LogError("[VisualsManager] _firstPersonCamera is null!");
            }

            if (_mainCamera != null)
            {
                if (ClientGame.Instance != null)
                {
                    ClientGame.Instance.RegisterMainCamera(_mainCamera);
                    Debug.Log($"[VisualsManager] Registered main camera: {_mainCamera.name}, Current ClientGame._mainCamera: {ClientGame._mainCamera?.name ?? "null"}");
                }
                else
                {
                    Debug.LogError("[VisualsManager] ClientGame.Instance is null!");
                }
            }
            else
            {
                Debug.LogError("[VisualsManager] _mainCamera is null!");
            }
        }
        else
        {
            //Debug.Log("[VisualsManager] This is NOT the owner, destroying camera");

            if (_weaponViewmodel != null) _weaponViewmodel.enabled = false;

            // Destroy camera for non-owners to prevent settings system from modifying it
            if (_firstPersonCamera != null)
            {
                Destroy(_firstPersonCamera.gameObject);
                Debug.Log("[VisualsManager] Destroyed camera for non-owner");
            }
        }

        // if screen was gray from previous death, reset it
        ScreenspaceEffectManager.SetGrayscale(false);
    }

    private void Update()
    {
        if (!isOwner) return;

        if (showVelocity && _playerMovement != null)
        {
            var velocity = _playerMovement._rigidbody.linearVelocity;
            HUDManager.Instance?.SetVelocityReadout(velocity);
        }

        if (_ability != null)
            HUDManager.Instance?.SetAbilityCooldown(_ability.CooldownNormalized, _ability.CooldownRemaining);

        if (_playerMovement != null && _footstepClips.Count > 0
            && _playerMovement.CurrentMovementState == PlayerManualMovement.MovementState.Grounded)
        {
            var vel = _playerMovement._rigidbody.linearVelocity;
            _footstepDistance += new Vector3(vel.x, 0f, vel.z).magnitude * Time.deltaTime;

            if (_footstepDistance >= _footstepInterval)
            {
                _footstepDistance = 0f;
                var clip = _footstepClips[Random.Range(0, _footstepClips.Count)];
                SoundManager.Play(new SoundData(clip, varyPitch: false, varyVolume: false));
            }
        }
    }
}
