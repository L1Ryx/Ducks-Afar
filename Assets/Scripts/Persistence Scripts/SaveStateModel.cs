using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SaveStateModel
{
    private readonly HashSet<string> unlockedLevelIds = new();
    private readonly HashSet<string> completedLevelIds = new();
    private readonly HashSet<string> discoveredArtifactIds = new();
    private readonly HashSet<string> worldStateIds = new();
    private readonly Dictionary<string, string> sceneVariantIds = new();

    public float TimePlayedSeconds { get; private set; }
    public string CurrentSceneName { get; set; }
    public string CurrentLocation { get; set; }
    public string CurrentCompanionId { get; set; }
    public bool HasSavedPlayerPosition { get; private set; }
    public string SavedPlayerPositionSceneName { get; private set; }
    public Vector3 SavedPlayerPosition { get; private set; }
    public int Goldworms { get; private set; }
    public bool HasCollectedGoldworms { get; private set; }

    public bool HasActiveSlot { get; private set; }
    public int ActiveSlotIndex { get; private set; }
    public event Action<int> GoldwormsChanged;
    public event Action<bool> GoldwormCollectionStateChanged;

    public SaveStateModel()
    {
        TimePlayedSeconds = 0f;
        CurrentSceneName = string.Empty;
        CurrentLocation = string.Empty;
        CurrentCompanionId = SaveSystem.NoneCompanionId;
        HasSavedPlayerPosition = false;
        SavedPlayerPositionSceneName = string.Empty;
        SavedPlayerPosition = Vector3.zero;
        Goldworms = 0;
        HasCollectedGoldworms = false;
        HasActiveSlot = false;
        ActiveSlotIndex = -1;
    }

    public IReadOnlyCollection<string> UnlockedLevelIds => unlockedLevelIds;
    public IReadOnlyCollection<string> CompletedLevelIds => completedLevelIds;
    public IReadOnlyCollection<string> DiscoveredArtifactIds => discoveredArtifactIds;
    public IReadOnlyCollection<string> WorldStateIds => worldStateIds;
    public IReadOnlyDictionary<string, string> SceneVariantIds => sceneVariantIds;

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
        SetGoldworms(0);
        SetHasCollectedGoldworms(false);
        ClearSavedPlayerPosition();
        unlockedLevelIds.Clear();
        completedLevelIds.Clear();
        discoveredArtifactIds.Clear();
        worldStateIds.Clear();
        sceneVariantIds.Clear();
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

    public bool HasWorldState(string worldStateId)
    {
        return ContainsId(worldStateIds, worldStateId);
    }

    public void SetGoldworms(int amount)
    {
        int normalizedAmount = Mathf.Max(0, amount);
        if (Goldworms == normalizedAmount)
            return;

        Goldworms = normalizedAmount;
        GoldwormsChanged?.Invoke(Goldworms);
    }

    public void AddGoldworms(int amount)
    {
        if (amount <= 0)
            return;

        SetGoldworms(Goldworms + amount);
    }

    public void MarkGoldwormsCollected()
    {
        SetHasCollectedGoldworms(true);
    }

    public void SetHasCollectedGoldworms(bool hasCollected)
    {
        if (HasCollectedGoldworms == hasCollected)
            return;

        HasCollectedGoldworms = hasCollected;
        GoldwormCollectionStateChanged?.Invoke(HasCollectedGoldworms);
    }

    public void SetSavedPlayerPosition(string sceneName, Vector3 position)
    {
        HasSavedPlayerPosition = true;
        SavedPlayerPositionSceneName = string.IsNullOrWhiteSpace(sceneName) ? string.Empty : sceneName.Trim();
        SavedPlayerPosition = position;
    }

    public void ClearSavedPlayerPosition()
    {
        HasSavedPlayerPosition = false;
        SavedPlayerPositionSceneName = string.Empty;
        SavedPlayerPosition = Vector3.zero;
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

    public bool SetWorldState(string worldStateId)
    {
        return AddId(worldStateIds, worldStateId);
    }

    public bool ClearWorldState(string worldStateId)
    {
        return RemoveId(worldStateIds, worldStateId);
    }

    public bool TryGetSceneVariant(string sceneName, out string variantId)
    {
        string normalizedSceneName = NormalizeId(sceneName);
        if (normalizedSceneName.Length == 0)
        {
            variantId = string.Empty;
            return false;
        }

        return sceneVariantIds.TryGetValue(normalizedSceneName, out variantId)
            && !string.IsNullOrWhiteSpace(variantId);
    }

    public bool SetSceneVariant(string sceneName, string variantId)
    {
        string normalizedSceneName = NormalizeId(sceneName);
        string normalizedVariantId = NormalizeId(variantId);

        if (normalizedSceneName.Length == 0 || normalizedVariantId.Length == 0)
            return false;

        if (sceneVariantIds.TryGetValue(normalizedSceneName, out string current)
            && current == normalizedVariantId)
        {
            return false;
        }

        sceneVariantIds[normalizedSceneName] = normalizedVariantId;
        return true;
    }

    public bool ClearSceneVariant(string sceneName)
    {
        string normalizedSceneName = NormalizeId(sceneName);
        return normalizedSceneName.Length > 0 && sceneVariantIds.Remove(normalizedSceneName);
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

    public void SetWorldStates(IEnumerable<string> worldStateIds)
    {
        ReplaceSet(this.worldStateIds, worldStateIds);
    }

    public void SetSceneVariants(IEnumerable<SceneVariantSaveData> sceneVariants)
    {
        sceneVariantIds.Clear();

        if (sceneVariants == null)
            return;

        foreach (SceneVariantSaveData sceneVariant in sceneVariants)
        {
            if (sceneVariant == null)
                continue;

            SetSceneVariant(sceneVariant.sceneName, sceneVariant.variantId);
        }
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

    public void ClearWorldStates()
    {
        worldStateIds.Clear();
    }

    public void ClearSceneVariants()
    {
        sceneVariantIds.Clear();
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
