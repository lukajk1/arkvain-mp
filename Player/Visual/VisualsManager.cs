using UnityEngine;
using PurrNet.Prediction;

public class VisualsManager : StatelessPredictedIdentity
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

    protected override void OnDestroy()
    {
        base.OnDestroy();

        // Clear camera reference if we registered it
        if (isOwner && ClientGame._mainCamera == _mainCamera)
        {
            ClientGame._mainCamera = null;
            Debug.Log("[VisualsManager] Cleared main camera reference on destroy");
        }
    }

    protected override void LateAwake()
    {
        base.LateAwake();

        Debug.Log($"[VisualsManager] LateAwake - isOwner: {isOwner}, Owner: {owner}, ClientGame.Instance: {ClientGame.Instance != null}, MainCamera: {_mainCamera != null}, FirstPersonCamera: {_firstPersonCamera != null}");

        if (isOwner)
        {
            Debug.Log("[VisualsManager] This is the owner, setting up local player visuals");

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
            Debug.Log("[VisualsManager] This is NOT the owner, destroying camera");

            if (_weaponViewmodel != null) _weaponViewmodel.enabled = false;

            // Destroy camera for non-owners to prevent settings system from modifying it
            if (_firstPersonCamera != null)
            {
                Destroy(_firstPersonCamera.gameObject);
                Debug.Log("[VisualsManager] Destroyed camera for non-owner");
            }
        }
    }
}
