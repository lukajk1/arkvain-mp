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

    protected override void LateAwake()
    {
        base.LateAwake();

        if (isOwner)
        {
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
            }
            ClientGame.Instance.RegisterMainCamera(_mainCamera);
        }
        else
        {
            if (_weaponViewmodel != null) _weaponViewmodel.enabled = false;

            // Disable camera for non-owners (this also disables child cameras)
            if (_firstPersonCamera != null)
            {
                _firstPersonCamera.gameObject.SetActive(false);
            }
        }
    }
}
