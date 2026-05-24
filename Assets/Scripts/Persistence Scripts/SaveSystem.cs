using System;
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
    
    public void SaveToActiveSlot()
    {
        if (!ctx.SaveState.HasActiveSlot)
        {
            Debug.LogWarning("SaveToActiveSlot failed: no active slot is currently bound.");
            return;
        }

        SaveToSlot(ctx.SaveState.ActiveSlotIndex);
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

    public void SaveToSlot(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            Debug.LogError($"SaveToSlot failed: invalid slot index {slotIndex}");
            return;
        }

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
                $"companion={data.companionId}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"SaveToSlot failed for slot {slotIndex}: {ex}");
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
    }

    private void SanitizeLoadedData(SaveSlotData data, int slotIndex)
    {
        data.slotIndex = slotIndex;

        if (data.timePlayedSeconds < 0f)
            data.timePlayedSeconds = 0f;

        if (data.location == null)
            data.location = string.Empty;

        if (data.sceneName == null)
            data.sceneName = string.Empty;

        if (string.IsNullOrWhiteSpace(data.companionId))
            data.companionId = NoneCompanionId;
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
}
