using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SaveSystem
{
    public const int SlotCount = 3;
    public const string NoneCompanionId = "NONE";

    private readonly GameContext ctx;

    public SaveSystem(GameContext ctx)
    {
        this.ctx = ctx;
        EnsureSaveDirectoryExists();
    }

    public string SaveDirectoryPath => Path.Combine(Application.persistentDataPath, "Saves");

    public bool IsValidSlotIndex(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < SlotCount;
    }
    
    public bool SaveToActiveSlot(bool captureSceneCheckpoint = true)
    {
        if (!ctx.SaveState.HasActiveSlot)
        {
            Debug.LogWarning("SaveToActiveSlot failed: no active slot is currently bound.");
            return false;
        }

        return SaveToSlot(ctx.SaveState.ActiveSlotIndex, captureSceneCheckpoint);
    }

    public bool SaveCurrentGameToActiveSlot()
    {
        if (!ctx.SaveState.HasActiveSlot)
        {
            Debug.LogWarning("SaveCurrentGameToActiveSlot failed: no active slot is currently bound.");
            return false;
        }

        CaptureCurrentSceneAndPlayerPosition();
        return SaveToSlot(ctx.SaveState.ActiveSlotIndex, captureSceneCheckpoint: false);
    }

    public string GetSlotPath(int slotIndex)
    {
        return Path.Combine(SaveDirectoryPath, $"save_slot_{slotIndex}.json");
    }

    public bool SlotExists(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
            return false;

        return File.Exists(GetSlotPath(slotIndex));
    }

    public SaveSlotData ReadSlot(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            Debug.LogError($"ReadSlot failed: invalid slot index {slotIndex}");
            return SaveSlotData.CreateEmpty(slotIndex);
        }

        string path = GetSlotPath(slotIndex);

        if (!File.Exists(path))
            return SaveSlotData.CreateEmpty(slotIndex);

        try
        {
            string json = File.ReadAllText(path);
            SaveSlotData data = JsonUtility.FromJson<SaveSlotData>(json);

            if (data == null)
            {
                Debug.LogWarning($"ReadSlot: slot {slotIndex} deserialized to null. Returning empty slot.");
                return SaveSlotData.CreateEmpty(slotIndex);
            }

            SanitizeLoadedData(data, slotIndex);
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError($"ReadSlot failed for slot {slotIndex}: {ex}");
            return SaveSlotData.CreateEmpty(slotIndex);
        }
    }

    public bool SaveToSlot(int slotIndex, bool captureSceneCheckpoint = true)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            Debug.LogError($"SaveToSlot failed: invalid slot index {slotIndex}");
            return false;
        }

        if (captureSceneCheckpoint)
            CaptureCurrentSceneCheckpoint();

        SaveSlotData data = BuildDataFromRuntimeState(slotIndex);
        string path = GetSlotPath(slotIndex);

        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);

            Debug.Log(
                $"Saved slot {slotIndex} | " +
                $"time={data.timePlayedSeconds:F1}s | " +
                $"scene={data.sceneName} | " +
                $"location={data.location} | " +
                $"hasPos={data.hasPlayerPosition} | " +
                $"goldworms={data.goldworms} | " +
                $"companion={data.companionId} | " +
                $"unlocked={data.unlockedLevelIds.Count} | " +
                $"completed={data.completedLevelIds.Count} | " +
                $"artifacts={data.discoveredArtifactIds.Count} | " +
                $"world={data.worldStateIds.Count} | " +
                $"variants={data.sceneVariants.Count}");

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"SaveToSlot failed for slot {slotIndex}: {ex}");
            return false;
        }
    }

    public bool LoadFromSlot(int slotIndex)
    {
        return LoadFromSlot(slotIndex, false);
    }

    public bool LoadFromSlotAndEnterScene(int slotIndex)
    {
        return LoadFromSlot(slotIndex, true);
    }

    public bool LoadSlotDataAndEnterScene(SaveSlotData data)
    {
        if (data == null)
        {
            Debug.LogError("LoadSlotDataAndEnterScene failed: save data is null.");
            return false;
        }

        if (!data.hasData)
        {
            Debug.LogWarning($"LoadSlotDataAndEnterScene: slot {data.slotIndex} is empty.");
            return false;
        }

        if (!IsValidSlotIndex(data.slotIndex))
        {
            Debug.LogError($"LoadSlotDataAndEnterScene failed: invalid slot index {data.slotIndex}");
            return false;
        }

        return ctx.SceneLoader.LoadSceneDeferred(
            null,
            () => PrepareSlotDataForSceneLoad(data));
    }

    private bool LoadFromSlot(int slotIndex, bool enterSavedScene)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            Debug.LogError($"LoadFromSlot failed: invalid slot index {slotIndex}");
            return false;
        }

        if (enterSavedScene)
            return ctx.SceneLoader.LoadSceneDeferred(null, () => PrepareSlotDataForSceneLoad(slotIndex));

        SaveSlotData data = ReadSlot(slotIndex);
        if (!data.hasData)
        {
            Debug.LogWarning($"LoadFromSlot: slot {slotIndex} is empty.");
            return false;
        }

        ApplyLoadedSlotData(data, slotIndex);

        return true;
    }
    
    public void StartNewGameInSlot(int slotIndex, string startLocation)
    {
        StartNewGameInSlot(slotIndex, string.Empty, startLocation);
    }

    public void StartNewGameInSlot(int slotIndex, string startSceneName, string startLocation)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            Debug.LogError($"StartNewGameInSlot failed: invalid slot index {slotIndex}");
            return;
        }

        ctx.SaveState.ResetForNewGame(startSceneName, startLocation);
        ctx.SaveState.BindToSlot(slotIndex);

        SaveToSlot(slotIndex);

        Debug.Log($"Started new game in slot {slotIndex} at scene {startSceneName}, location {startLocation}");
    }

    public bool StartNewGameInSlotAndEnterScene(int slotIndex, string startSceneName, string startLocation)
    {
        if (string.IsNullOrWhiteSpace(startSceneName))
        {
            Debug.LogError("StartNewGameInSlotAndEnterScene failed: startSceneName is empty.");
            return false;
        }

        if (!ctx.SceneLoader.CanLoadScene(startSceneName))
            return false;

        if (!IsValidSlotIndex(slotIndex))
            return false;

        return ctx.SceneLoader.LoadScene(
            startSceneName,
            null,
            () => StartNewGameInSlot(slotIndex, startSceneName, startLocation));
    }

    private void ApplyLoadedSlotData(SaveSlotData data, int slotIndex)
    {
        ApplyDataToRuntimeState(data);
        ctx.SaveState.BindToSlot(slotIndex);

        Debug.Log(
            $"Loaded slot {slotIndex} | " +
            $"time={data.timePlayedSeconds:F1}s | " +
            $"scene={data.sceneName} | " +
            $"location={data.location} | " +
            $"companion={data.companionId}");
    }

    private string PrepareSlotDataForSceneLoad(int slotIndex)
    {
        SaveSlotData data = ReadSlot(slotIndex);
        return PrepareSlotDataForSceneLoad(data);
    }

    private string PrepareSlotDataForSceneLoad(SaveSlotData data)
    {
        if (data == null)
        {
            Debug.LogError("PrepareSlotDataForSceneLoad failed: save data is null.");
            return null;
        }

        if (!data.hasData)
        {
            Debug.LogWarning($"PrepareSlotDataForSceneLoad: slot {data.slotIndex} is empty.");
            return null;
        }

        if (!IsValidSlotIndex(data.slotIndex))
        {
            Debug.LogError($"PrepareSlotDataForSceneLoad failed: invalid slot index {data.slotIndex}");
            return null;
        }

        if (!CanLoadSavedScene(data))
            return null;

        ApplyLoadedSlotData(data, data.slotIndex);
        return data.sceneName;
    }

    public void DeleteSlot(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            Debug.LogError($"DeleteSlot failed: invalid slot index {slotIndex}");
            return;
        }

        string path = GetSlotPath(slotIndex);

        if (!File.Exists(path))
        {
            Debug.LogWarning($"DeleteSlot: slot {slotIndex} does not exist.");
            return;
        }

        try
        {
            File.Delete(path);
            Debug.Log($"Deleted slot {slotIndex}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"DeleteSlot failed for slot {slotIndex}: {ex}");
        }
    }

    public SaveSlotData[] ReadAllSlots()
    {
        SaveSlotData[] slots = new SaveSlotData[SlotCount];

        for (int i = 0; i < SlotCount; i++)
        {
            slots[i] = ReadSlot(i);
        }

        return slots;
    }

    public void EnsureSaveDirectoryExists()
    {
        Directory.CreateDirectory(SaveDirectoryPath);
    }

    private SaveSlotData BuildDataFromRuntimeState(int slotIndex)
    {
        SaveStateModel saveState = ctx.SaveState;

        return new SaveSlotData
        {
            hasData = true,
            slotIndex = slotIndex,
            timePlayedSeconds = saveState.TimePlayedSeconds,
            sceneName = GetSceneNameForSave(saveState),
            location = saveState.CurrentLocation ?? string.Empty,
            companionId = string.IsNullOrWhiteSpace(saveState.CurrentCompanionId)
                ? NoneCompanionId
                : saveState.CurrentCompanionId,
            hasPlayerPosition = saveState.HasSavedPlayerPosition,
            playerPositionSceneName = saveState.SavedPlayerPositionSceneName ?? string.Empty,
            playerPositionX = saveState.SavedPlayerPosition.x,
            playerPositionY = saveState.SavedPlayerPosition.y,
            playerPositionZ = saveState.SavedPlayerPosition.z,
            goldworms = saveState.Goldworms,
            hasCollectedGoldworms = saveState.HasCollectedGoldworms,
            unlockedLevelIds = BuildSortedList(saveState.UnlockedLevelIds),
            completedLevelIds = BuildSortedList(saveState.CompletedLevelIds),
            discoveredArtifactIds = BuildSortedList(saveState.DiscoveredArtifactIds),
            worldStateIds = BuildSortedList(saveState.WorldStateIds),
            sceneVariants = BuildSortedSceneVariantList(saveState.SceneVariantIds),
            lastSavedUtc = DateTime.UtcNow.ToString("o")
        };
    }

    private void ApplyDataToRuntimeState(SaveSlotData data)
    {
        SaveStateModel saveState = ctx.SaveState;

        saveState.SetPlayTime(data.timePlayedSeconds);
        saveState.CurrentSceneName = data.sceneName ?? string.Empty;
        saveState.CurrentLocation = data.location ?? string.Empty;
        saveState.CurrentCompanionId = string.IsNullOrWhiteSpace(data.companionId)
            ? NoneCompanionId
            : data.companionId;
        saveState.SetGoldworms(data.goldworms);
        saveState.SetHasCollectedGoldworms(
            data.hasCollectedGoldworms
            || data.goldworms > 0
            || ContainsGoldwormWorldState(data.worldStateIds));

        if (data.hasPlayerPosition)
        {
            saveState.SetSavedPlayerPosition(
                data.playerPositionSceneName,
                new Vector3(data.playerPositionX, data.playerPositionY, data.playerPositionZ));
        }
        else
        {
            saveState.ClearSavedPlayerPosition();
        }

        saveState.SetUnlockedLevels(data.unlockedLevelIds);
        saveState.SetCompletedLevels(data.completedLevelIds);
        saveState.SetDiscoveredArtifacts(data.discoveredArtifactIds);
        saveState.SetWorldStates(data.worldStateIds);
        saveState.SetSceneVariants(data.sceneVariants);
    }

    private void SanitizeLoadedData(SaveSlotData data, int slotIndex)
    {
        data.slotIndex = slotIndex;

        if (data.timePlayedSeconds < 0f)
            data.timePlayedSeconds = 0f;

        if (data.goldworms < 0)
            data.goldworms = 0;

        if (data.location == null)
            data.location = string.Empty;

        if (data.sceneName == null)
            data.sceneName = string.Empty;

        if (string.IsNullOrWhiteSpace(data.companionId))
            data.companionId = NoneCompanionId;

        if (data.playerPositionSceneName == null)
            data.playerPositionSceneName = string.Empty;

        data.unlockedLevelIds = SanitizeIdList(data.unlockedLevelIds);
        data.completedLevelIds = SanitizeIdList(data.completedLevelIds);
        data.discoveredArtifactIds = SanitizeIdList(data.discoveredArtifactIds);
        data.worldStateIds = SanitizeIdList(data.worldStateIds);
        data.sceneVariants = SanitizeSceneVariantList(data.sceneVariants);
    }

    private static bool ContainsGoldwormWorldState(List<string> worldStateIds)
    {
        if (worldStateIds == null)
            return false;

        foreach (string id in worldStateIds)
        {
            if (!string.IsNullOrWhiteSpace(id)
                && id.Trim().StartsWith("goldworm:", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetSceneNameForSave(SaveStateModel saveState)
    {
        if (!string.IsNullOrWhiteSpace(saveState.CurrentSceneName))
            return saveState.CurrentSceneName;

        Scene activeScene = SceneManager.GetActiveScene();
        return activeScene.IsValid() ? activeScene.name : string.Empty;
    }

    private bool CanLoadSavedScene(SaveSlotData data)
    {
        if (string.IsNullOrWhiteSpace(data.sceneName))
        {
            Debug.LogWarning($"LoadFromSlot: slot {data.slotIndex} has no saved sceneName.");
            return false;
        }

        return ctx.SceneLoader.CanLoadScene(data.sceneName);
    }

    private void CaptureCurrentSceneCheckpoint()
    {
        CheckpointSaver checkpoint = UnityEngine.Object.FindFirstObjectByType<CheckpointSaver>(
            FindObjectsInactive.Include);

        if (checkpoint == null)
            return;

        checkpoint.SetLocationOnly();
    }

    private void CaptureCurrentSceneAndPlayerPosition()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        string sceneName = activeScene.IsValid() ? activeScene.name : string.Empty;

        ctx.SaveState.CurrentSceneName = sceneName;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("SaveCurrentGameToActiveSlot: no GameObject tagged Player was found. Saving scene/world state without exact player position.");
            ctx.SaveState.ClearSavedPlayerPosition();
            return;
        }

        ctx.SaveState.SetSavedPlayerPosition(sceneName, player.transform.position);
    }

    private static List<string> BuildSortedList(IReadOnlyCollection<string> ids)
    {
        List<string> list = SanitizeIdList(ids);
        list.Sort(StringComparer.Ordinal);
        return list;
    }

    private static List<SceneVariantSaveData> BuildSortedSceneVariantList(
        IReadOnlyDictionary<string, string> sceneVariants)
    {
        List<SceneVariantSaveData> list = new List<SceneVariantSaveData>();

        if (sceneVariants == null)
            return list;

        foreach (KeyValuePair<string, string> pair in sceneVariants)
        {
            string sceneName = NormalizeId(pair.Key);
            string variantId = NormalizeId(pair.Value);
            if (sceneName.Length == 0 || variantId.Length == 0)
                continue;

            list.Add(new SceneVariantSaveData
            {
                sceneName = sceneName,
                variantId = variantId
            });
        }

        list.Sort((a, b) => string.Compare(a.sceneName, b.sceneName, StringComparison.Ordinal));
        return list;
    }

    private static List<string> SanitizeIdList(IEnumerable<string> ids)
    {
        List<string> list = new List<string>();

        if (ids == null)
            return list;

        HashSet<string> seen = new HashSet<string>();

        foreach (string id in ids)
        {
            if (string.IsNullOrWhiteSpace(id))
                continue;

            string normalized = id.Trim();
            if (seen.Add(normalized))
                list.Add(normalized);
        }

        return list;
    }

    private static List<SceneVariantSaveData> SanitizeSceneVariantList(
        IEnumerable<SceneVariantSaveData> sceneVariants)
    {
        Dictionary<string, string> sanitized = new Dictionary<string, string>();

        if (sceneVariants != null)
        {
            foreach (SceneVariantSaveData sceneVariant in sceneVariants)
            {
                if (sceneVariant == null)
                    continue;

                string sceneName = NormalizeId(sceneVariant.sceneName);
                string variantId = NormalizeId(sceneVariant.variantId);
                if (sceneName.Length == 0 || variantId.Length == 0)
                    continue;

                sanitized[sceneName] = variantId;
            }
        }

        return BuildSortedSceneVariantList(sanitized);
    }

    private static string NormalizeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }
}
