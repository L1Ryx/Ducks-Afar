using System.Collections.Generic;
using UnityEngine;

public sealed class SaveStateModel
{
    private readonly HashSet<string> unlockedLevelIds = new();
    private readonly HashSet<string> completedLevelIds = new();
    private readonly HashSet<string> discoveredArtifactIds = new();

    public float TimePlayedSeconds { get; private set; }
    public string CurrentSceneName { get; set; }
    public string CurrentLocation { get; set; }
    public string CurrentCompanionId { get; set; }

    public bool HasActiveSlot { get; private set; }
    public int ActiveSlotIndex { get; private set; }

    public SaveStateModel()
    {
        TimePlayedSeconds = 0f;
        CurrentSceneName = string.Empty;
        CurrentLocation = string.Empty;
        CurrentCompanionId = SaveSystem.NoneCompanionId;
        HasActiveSlot = false;
        ActiveSlotIndex = -1;
    }

    public IReadOnlyCollection<string> UnlockedLevelIds => unlockedLevelIds;
    public IReadOnlyCollection<string> CompletedLevelIds => completedLevelIds;
    public IReadOnlyCollection<string> DiscoveredArtifactIds => discoveredArtifactIds;

    public void AddPlayTime(float deltaTime)
    {
        TimePlayedSeconds += Mathf.Max(0f, deltaTime);
    }

    public void SetPlayTime(float seconds)
    {
        TimePlayedSeconds = Mathf.Max(0f, seconds);
    }

    public void BindToSlot(int slotIndex)
    {
        HasActiveSlot = true;
        ActiveSlotIndex = slotIndex;
    }

    public void ClearActiveSlot()
    {
        HasActiveSlot = false;
        ActiveSlotIndex = -1;
    }

    public void ResetForNewGame(string startLocation)
    {
        ResetForNewGame(string.Empty, startLocation);
    }

    public void ResetForNewGame(string startSceneName, string startLocation)
    {
        TimePlayedSeconds = 0f;
        CurrentSceneName = startSceneName ?? string.Empty;
        CurrentLocation = startLocation ?? string.Empty;
        CurrentCompanionId = SaveSystem.NoneCompanionId;
        unlockedLevelIds.Clear();
        completedLevelIds.Clear();
        discoveredArtifactIds.Clear();
    }

    public bool IsLevelUnlocked(string levelId)
    {
        return ContainsId(unlockedLevelIds, levelId);
    }

    public bool IsLevelCompleted(string levelId)
    {
        return ContainsId(completedLevelIds, levelId);
    }

    public bool IsArtifactDiscovered(string artifactId)
    {
        return ContainsId(discoveredArtifactIds, artifactId);
    }

    public bool UnlockLevel(string levelId)
    {
        return AddId(unlockedLevelIds, levelId);
    }

    public bool LockLevel(string levelId)
    {
        return RemoveId(unlockedLevelIds, levelId);
    }

    public bool CompleteLevel(string levelId)
    {
        if (!AddId(completedLevelIds, levelId))
            return false;

        AddId(unlockedLevelIds, levelId);
        return true;
    }

    public bool UncompleteLevel(string levelId)
    {
        return RemoveId(completedLevelIds, levelId);
    }

    public bool DiscoverArtifact(string artifactId)
    {
        return AddId(discoveredArtifactIds, artifactId);
    }

    public bool ForgetArtifact(string artifactId)
    {
        return RemoveId(discoveredArtifactIds, artifactId);
    }

    public void SetUnlockedLevels(IEnumerable<string> levelIds)
    {
        ReplaceSet(unlockedLevelIds, levelIds);
    }

    public void SetCompletedLevels(IEnumerable<string> levelIds)
    {
        ReplaceSet(completedLevelIds, levelIds);

        foreach (string levelId in completedLevelIds)
        {
            unlockedLevelIds.Add(levelId);
        }
    }

    public void SetDiscoveredArtifacts(IEnumerable<string> artifactIds)
    {
        ReplaceSet(discoveredArtifactIds, artifactIds);
    }

    public void ClearUnlockedLevels()
    {
        unlockedLevelIds.Clear();
    }

    public void ClearCompletedLevels()
    {
        completedLevelIds.Clear();
    }

    public void ClearDiscoveredArtifacts()
    {
        discoveredArtifactIds.Clear();
    }

    private static bool ContainsId(HashSet<string> ids, string id)
    {
        string normalized = NormalizeId(id);
        return normalized.Length > 0 && ids.Contains(normalized);
    }

    private static bool AddId(HashSet<string> ids, string id)
    {
        string normalized = NormalizeId(id);
        return normalized.Length > 0 && ids.Add(normalized);
    }

    private static bool RemoveId(HashSet<string> ids, string id)
    {
        string normalized = NormalizeId(id);
        return normalized.Length > 0 && ids.Remove(normalized);
    }

    private static void ReplaceSet(HashSet<string> target, IEnumerable<string> source)
    {
        target.Clear();

        if (source == null)
            return;

        foreach (string id in source)
        {
            AddId(target, id);
        }
    }

    private static string NormalizeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }
}
