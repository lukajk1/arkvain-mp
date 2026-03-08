using UnityEngine;

[CreateAssetMenu(fileName = "NewMapDefinition", menuName = "Arkvain/Map Definition")]
public class MapDefinition : ScriptableObject
{
    public string InternalName => sceneAsset != null ? sceneAsset.name : string.Empty;

    [Header("Display Info")]
    public string displayName;
    public Sprite thumbnail;
    [TextArea] public string description;

    [Header("Loading Info")]
    [Tooltip("The SceneNameHolder containing the name of the Unity Scene to load additively.")]
    public SceneNameHolder sceneAsset;

    public string SceneName => sceneAsset != null ? sceneAsset.sceneName : string.Empty;

    [Header("Gameplay Settings")]
    public bool supportsTDM = true;
    public bool supportsFFA = true;
}
