using UnityEngine;
using PurrNet.Prediction;

public class HideLocalPlayerMesh : StatelessPredictedIdentity
{
    [SerializeField] private SkinnedMeshRenderer[] _skinnedMeshRenderers;
    [SerializeField] private MeshRenderer[] _meshRenderers;

    [SerializeField] private MeshRenderer _weaponViewmodel;
    [SerializeField] private MeshRenderer _weaponDiegetic;

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
        }

        else
        {
            if (_weaponViewmodel != null) _weaponViewmodel.enabled = false;
        }
    }
}
