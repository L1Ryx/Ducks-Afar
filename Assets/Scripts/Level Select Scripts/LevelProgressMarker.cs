using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class LevelProgressMarker : MonoBehaviour
{
    [Header("Level")]
    [SerializeField] private LevelDefinition level;
    [SerializeField] private string fallbackLevelId;

    [Header("Start")]
    [SerializeField] private bool unlockCurrentLevelOnStart = true;

    [Header("Completion")]
    [SerializeField] private bool markCompletedWhenLevelStateCompletes = true;
    [SerializeField] private List<LevelDefinition> unlockLevelsOnCompletion = new();

    [Header("Saving")]
    [SerializeField] private bool saveActiveSlotOnChange = false;

    private string LevelId => level != null ? level.LevelId : fallbackLevelId;

    private IEnumerator Start()
    {
        while (!Game.IsReady || Game.Ctx?.SaveState == null)
            yield return null;

        if (unlockCurrentLevelOnStart)
            UnlockLevel(LevelId);

        if (markCompletedWhenLevelStateCompletes && Game.Ctx.LevelState != null)
            Game.Ctx.LevelState.OnLevelCompleted += HandleLevelCompleted;
    }

    private void OnDestroy()
    {
        if (Game.IsReady && Game.Ctx?.LevelState != null)
            Game.Ctx.LevelState.OnLevelCompleted -= HandleLevelCompleted;
    }

    public void UnlockLevel(string levelId)
    {
        if (!CanWriteProgress(levelId))
            return;

        bool changed = Game.Ctx.SaveState.UnlockLevel(levelId);
        SaveIfChanged(changed);
    }

    public void CompleteLevel(string levelId)
    {
        if (!CanWriteProgress(levelId))
            return;

        bool changed = Game.Ctx.SaveState.CompleteLevel(levelId);
        SaveIfChanged(changed);
    }

    public void DiscoverArtifact(string artifactId)
    {
        if (!CanWriteProgress(artifactId))
            return;

        bool changed = Game.Ctx.SaveState.DiscoverArtifact(artifactId);
        SaveIfChanged(changed);
    }

    private void HandleLevelCompleted()
    {
        bool changed = false;

        if (CanWriteProgress(LevelId))
            changed |= Game.Ctx.SaveState.CompleteLevel(LevelId);

        foreach (LevelDefinition unlockedLevel in unlockLevelsOnCompletion)
        {
            if (unlockedLevel == null || string.IsNullOrWhiteSpace(unlockedLevel.LevelId))
                continue;

            changed |= Game.Ctx.SaveState.UnlockLevel(unlockedLevel.LevelId);
        }

        SaveIfChanged(changed);
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
