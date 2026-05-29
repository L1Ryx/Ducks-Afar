using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "Level Select/Level")]
public sealed class LevelDefinition : ScriptableObject
{
    [SerializeField] private string levelId;
    [SerializeField] private string displayName;
    [TextArea] [SerializeField] private string description;
    [SerializeField] private string sceneName;
    [SerializeField] private List<LevelArtifactDefinition> artifacts = new();

    public string LevelId => levelId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? levelId : displayName;
    public string Description => description;
    public string SceneName => sceneName;
    public IReadOnlyList<LevelArtifactDefinition> Artifacts => artifacts;
}
