using IngameDebugConsole;
using UnityEngine;

public class SaveDebugCommands : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        RegisterDebugCommands();
    }

    private void RegisterDebugCommands()
    {
        DebugLogConsole.AddCommand("/save_path", "Prints save directory path", SavePath);
        DebugLogConsole.AddCommand("/save_list", "Lists all save slots", SaveList);
        DebugLogConsole.AddCommand("/save_dump", "Prints current runtime save state", SaveDumpCurrent);

        DebugLogConsole.AddCommand<int>("/save_write", "Save current state to slot index", SaveWrite);
        DebugLogConsole.AddCommand<int>("/save_load", "Load slot into runtime state", SaveLoad);
        DebugLogConsole.AddCommand<int>("/save_delete", "Delete a save slot", SaveDelete);

        DebugLogConsole.AddCommand<string>("/save_set_location", "Set current location", SaveSetLocation);
        DebugLogConsole.AddCommand<string>("/save_set_companion", "Set current companion id", SaveSetCompanion);
        DebugLogConsole.AddCommand<float>("/save_set_time", "Set playtime in seconds", SaveSetTime);

        DebugLogConsole.AddCommand("/save_reset", "Reset runtime save state", SaveResetCurrent);
        
        DebugLogConsole.AddCommand<int>("/save_fill", "Fill slot with test data", SaveFill);
        DebugLogConsole.AddCommand("/save_open", "Open save directory", SaveOpenFolder);
        
        DebugLogConsole.AddCommand("/save_active", "Prints the currently active save slot", SaveActive);
        DebugLogConsole.AddCommand("/save_write_active", "Save to the active slot", SaveWriteActive);
        DebugLogConsole.AddCommand<int>("/save_bind", "Bind current session to slot index", SaveBind);
    }
    
    private static void SaveActive()
    {
        var s = Game.Ctx.SaveState;

        if (!s.HasActiveSlot)
        {
            Debug.Log("No active save slot is currently bound.");
            return;
        }

        Debug.Log($"Active save slot: {s.ActiveSlotIndex}");
    }
    
    private static void SaveWriteActive()
    {
        Game.Ctx.Saves.SaveToActiveSlot();
    }
    
    private static void SaveBind(int slotIndex)
    {
        if (!Game.Ctx.Saves.IsValidSlotIndex(slotIndex))
        {
            Debug.LogWarning($"Invalid slot index: {slotIndex}");
            return;
        }

        Game.Ctx.SaveState.BindToSlot(slotIndex);
        Debug.Log($"Bound current session to slot {slotIndex}");
    }
    
    private static void SavePath()
    {
        Debug.Log($"Persistent Path: {Application.persistentDataPath}");
        Debug.Log($"Save Dir: {Game.Ctx.Saves.SaveDirectoryPath}");
    }
    
    private static void SaveList()
    {
        var slots = Game.Ctx.Saves.ReadAllSlots();

        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];

            if (!s.hasData)
            {
                Debug.Log($"Slot {i}: EMPTY");
                continue;
            }

            Debug.Log(
                $"Slot {i} | " +
                $"time={s.timePlayedSeconds:F1}s | " +
                $"loc={s.location} | " +
                $"comp={s.companionId} | " +
                $"saved={s.lastSavedUtc}");
        }
    }
    
    private static void SaveDumpCurrent()
    {
        var s = Game.Ctx.SaveState;

        Debug.Log(
            $"Current SaveState | " +
            $"time={s.TimePlayedSeconds:F1}s | " +
            $"loc={s.CurrentLocation} | " +
            $"comp={s.CurrentCompanionId}");
    }
    
    private static void SaveWrite(int slotIndex)
    {
        Game.Ctx.Saves.SaveToSlot(slotIndex);
    }
    
    private static void SaveLoad(int slotIndex)
    {
        bool success = Game.Ctx.Saves.LoadFromSlot(slotIndex);

        if (!success)
        {
            Debug.LogWarning($"Load failed for slot {slotIndex}");
        }
    }
    
    private static void SaveDelete(int slotIndex)
    {
        Game.Ctx.Saves.DeleteSlot(slotIndex);
    }
    
    private static void SaveSetLocation(string location)
    {
        Game.Ctx.SaveState.CurrentLocation = location;
        Debug.Log($"Set location -> {location}");
    }
    
    private static void SaveSetCompanion(string companionId)
    {
        Game.Ctx.SaveState.CurrentCompanionId = companionId;
        Debug.Log($"Set companion -> {companionId}");
    }
    
    private static void SaveSetTime(float seconds)
    {
        Game.Ctx.SaveState.SetPlayTime(seconds);
        Debug.Log($"Set playtime -> {seconds:F1}s");
    }
    
    private static void SaveResetCurrent()
    {
        Game.Ctx.SaveState.ResetForNewGame(string.Empty);
        Debug.Log("Reset SaveState to defaults");
    }
    
    private static void SaveFill(int slot)
    {
        var ctx = Game.Ctx;

        ctx.SaveState.CurrentLocation = $"TestZone_{slot}";
        ctx.SaveState.CurrentCompanionId = $"Companion_{slot}";
        ctx.SaveState.SetPlayTime(UnityEngine.Random.Range(10f, 500f));

        ctx.Saves.SaveToSlot(slot);

        Debug.Log($"Filled slot {slot} with test data");
    }
    
    private static void SaveOpenFolder()
    {
        string path = Game.Ctx.Saves.SaveDirectoryPath;
        Debug.Log($"Opening: {path}");
        Application.OpenURL("file://" + path);
    }
}
