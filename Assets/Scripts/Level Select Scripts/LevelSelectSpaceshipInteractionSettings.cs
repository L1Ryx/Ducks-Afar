using UnityEngine;

[CreateAssetMenu(fileName = "LevelSelectSpaceshipInteractionSettings", menuName = "Level Select/Spaceship Interaction Settings")]
public sealed class LevelSelectSpaceshipInteractionSettings : ScriptableObject
{
    [SerializeField] private bool spaceshipsInteractable = true;

    public bool SpaceshipsInteractable => spaceshipsInteractable;
}
