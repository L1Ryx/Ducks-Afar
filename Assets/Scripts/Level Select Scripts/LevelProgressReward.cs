using System.Collections.Generic;
using UnityEngine;

public sealed class LevelProgressReward : MonoBehaviour
{
    [Header("Levels")]
    [SerializeField] private List<LevelDefinition> unlockLevels = new();
    [SerializeField] private List<LevelDefinition> completeLevels = new();

    [Header("Artifacts")]
    [SerializeField] private List<LevelArtifactDefinition> discoverArtifacts = new();

    [Header("Fallback Ids")]
    [SerializeField] private List<string> unlockLevelIds = new();
    [SerializeField] private List<string> completeLevelIds = new();
    [SerializeField] private List<string> discoverArtifactIds = new();

    [Header("Saving")]
    [SerializeField] private bool saveActiveSlotOnChange = true;

    public void Apply()
    {
        if (!Game.IsReady || Game.Ctx?.SaveState == null)
        {
            Debug.LogWarning($"{nameof(LevelProgressReward)}: SaveState is not ready.", this);
            return;
        }

        bool changed = false;

        foreach (LevelDefinition level in unlockLevels)
            changed |= UnlockLevel(level);

        foreach (string levelId in unlockLevelIds)
            changed |= UnlockLevel(levelId);

        foreach (LevelDefinition level in completeLevels)
            changed |= CompleteLevel(level);

        foreach (string levelId in completeLevelIds)
            changed |= CompleteLevel(levelId);

        foreach (LevelArtifactDefinition artifact in discoverArtifacts)
            changed |= DiscoverArtifact(artifact);

        foreach (string artifactId in discoverArtifactIds)
            changed |= DiscoverArtifact(artifactId);

        SaveIfChanged(changed);
    }

    public bool UnlockLevel(LevelDefinition level)
    {
        return level != null && UnlockLevel(level.LevelId);
    }

    public bool CompleteLevel(LevelDefinition level)
    {
        return level != null && CompleteLevel(level.LevelId);
    }

    public bool DiscoverArtifact(LevelArtifactDefinition artifact)
    {
        return artifact != null && DiscoverArtifact(artifact.ArtifactId);
    }

    public bool UnlockLevel(string levelId)
    {
        if (!CanWriteProgress(levelId))
            return false;

        return Game.Ctx.SaveState.UnlockLevel(levelId);
    }

    public bool CompleteLevel(string levelId)
    {
        if (!CanWriteProgress(levelId))
            return false;

        return Game.Ctx.SaveState.CompleteLevel(levelId);
    }

    public bool DiscoverArtifact(string artifactId)
    {
        if (!CanWriteProgress(artifactId))
            return false;

        return Game.Ctx.SaveState.DiscoverArtifact(artifactId);
    }

    private static bool CanWriteProgress(string id)
    {
        return Game.IsReady && Game.Ctx?.SaveState != null && !string.IsNullOrWhiteSpace(id);
    }

    private void SaveIfChanged(bool changed)
    {
        if (!changed || !saveActiveSlotOnChange || Game.Ctx?.Saves == null)
            return;

        Game.Ctx.Saves.SaveToActiveSlot();
    }
}
