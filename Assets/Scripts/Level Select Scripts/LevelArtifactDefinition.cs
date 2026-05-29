using UnityEngine;

[CreateAssetMenu(fileName = "LevelArtifact", menuName = "Level Select/Artifact")]
public sealed class LevelArtifactDefinition : ScriptableObject
{
    [SerializeField] private string artifactId;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;

    public string ArtifactId => artifactId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? artifactId : displayName;
    public Sprite Icon => icon;
}
