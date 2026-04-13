using System;
using System.IO;
using UnityEngine;

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

        SaveSlotData data = BuildDataFromRuntimeState(slotIndex);
        string path = GetSlotPath(slotIndex);

        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);

            Debug.Log(
                $"Saved slot {slotIndex} | " +
                $"time={data.timePlayedSeconds:F1}s | " +
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
        if (!IsValidSlotIndex(slotIndex))
        {
            Debug.LogError($"LoadFromSlot failed: invalid slot index {slotIndex}");
            return false;
        }

        SaveSlotData data = ReadSlot(slotIndex);

        if (!data.hasData)
        {
            Debug.LogWarning($"LoadFromSlot: slot {slotIndex} is empty.");
            return false;
        }

        ApplyDataToRuntimeState(data);

        Debug.Log(
            $"Loaded slot {slotIndex} | " +
            $"time={data.timePlayedSeconds:F1}s | " +
            $"location={data.location} | " +
            $"companion={data.companionId}");

        return true;
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

        if (string.IsNullOrWhiteSpace(data.companionId))
            data.companionId = NoneCompanionId;
    }
}