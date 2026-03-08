using UnityEngine;

[CreateAssetMenu(fileName = "NewMapDefinition", menuName = "Arkvain/Map Definition")]
public class MapDefinition : ScriptableObject
{
    [Header("Steam Metadata")]
    [Tooltip("This must match the 'map' string stored in the Steam Lobby metadata.")]
    public string internalName;

    [Header("Display Info")]
    public string displayName;
    public Sprite thumbnail;
    [TextArea] public string description;

    [Header("Loading Info")]
    [Tooltip("The exact name of the Unity Scene to load additively.")]
    public string sceneName;

    [Header("Gameplay Settings")]
    public bool supportsTDM = true;
    public bool supportsFFA = true;
}
