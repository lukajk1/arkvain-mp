using UnityEngine;

/// <summary>
/// View-only recoil component that applies visual camera rotation.
/// The actual recoil that affects aim is server-validated in PlayerShooter.
/// This is purely cosmetic feedback for the local player.
/// </summary>
public class Recoil : MonoBehaviour
{
    private Vector3 currentRotation;
    private Vector3 targetRotation;

    //[SerializeField] private Transform targetedTransformForRecoil;
    [SerializeField] private float snappiness = 10f;

    private void Update()
    {
        currentRotation = Vector3.Slerp(currentRotation, targetRotation, snappiness * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(currentRotation);
    }

    /// <summary>
    /// Sets the visual recoil offset from the validated state.
    /// Called by PlayerShooter.UpdateView() with server-validated recoil.
    /// </summary>
    public void SetRecoilOffset(Vector3 offset)
    {
        targetRotation = offset;
    }
}
