using UnityEngine;

[CreateAssetMenu(fileName = "New Recoil Profile", menuName = "Weapon/Recoil Profile")]
public class RecoilProfile : ScriptableObject
{
    [Header("Recoil Force (in degrees)")]
    [Tooltip("Vertical recoil (pitch) - typically negative to push view upward")]
    public float recoilX = -15f;

    [Tooltip("Horizontal recoil variation (yaw)")]
    public float recoilY = 3f;

    [Tooltip("Roll recoil variation")]
    public float recoilZ = 2f;

    [Header("Recoil Recovery")]
    [Tooltip("Speed at which recoil returns to center (lower = slower recovery, higher = faster)")]
    public float recoverySpeed = 2f;

    [Header("Recoil Pattern")]
    [Tooltip("Seed for deterministic Perlin noise pattern - change for different recoil patterns")]
    public float noiseSeed = 12.345f;
}
