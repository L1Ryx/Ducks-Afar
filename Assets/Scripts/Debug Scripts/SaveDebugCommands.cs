using System;
using System.Collections.Generic;
using IngameDebugConsole;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        DebugLogConsole.AddCommand<int>("/save_enter", "Load slot into runtime state and enter its saved scene", SaveEnter);
        DebugLogConsole.AddCommand<int>("/save_delete", "Delete a save slot", SaveDelete);

        DebugLogConsole.AddCommand<string>("/save_set_scene", "Set current saved scene name", SaveSetScene);
        DebugLogConsole.AddCommand<string>("/save_set_location", "Set current location", SaveSetLocation);
        DebugLogConsole.AddCommand<string>("/save_set_companion", "Set current companion id", SaveSetCompanion);
        DebugLogConsole.AddCommand<float>("/save_set_time", "Set playtime in seconds", SaveSetTime);

        DebugLogConsole.AddCommand("/save_reset", "Reset runtime save state", SaveResetCurrent);
        
        DebugLogConsole.AddCommand<int>("/save_fill", "Fill slot with test data", SaveFill);
        DebugLogConsole.AddCommand("/save_open", "Open save directory", SaveOpenFolder);
        
        DebugLogConsole.AddCommand("/save_active", "Prints the currently active save slot", SaveActive);
        DebugLogConsole.AddCommand("/save_write_active", "Save to the active slot", SaveWriteActive);
        DebugLogConsole.AddCommand<int>("/save_bind", "Bind current session to slot index", SaveBind);

        DebugLogConsole.AddCommand<string>("/save_unlock_level", "Add a level id to unlocked levels", SaveUnlockLevel);
        DebugLogConsole.AddCommand<string>("/save_lock_level", "Remove a level id from unlocked levels", SaveLockLevel);
        DebugLogConsole.AddCommand<string>("/save_complete_level", "Add a level id to completed levels", SaveCompleteLevel);
        DebugLogConsole.AddCommand<string>("/save_uncomplete_level", "Remove a level id from completed levels", SaveUncompleteLevel);
        DebugLogConsole.AddCommand<string>("/save_discover_artifact", "Add an artifact id to discovered artifacts", SaveDiscoverArtifact);
        DebugLogConsole.AddCommand<string>("/save_forget_artifact", "Remove an artifact id from discovered artifacts", SaveForgetArtifact);

        DebugLogConsole.AddCommand<string>("/save_set_unlocked", "Replace unlocked level ids. Separate ids with comma, semicolon, or pipe", SaveSetUnlocked);
        DebugLogConsole.AddCommand<string>("/save_set_completed", "Replace completed level ids. Separate ids with comma, semicolon, or pipe", SaveSetCompleted);
        DebugLogConsole.AddCommand<string>("/save_set_artifacts", "Replace discovered artifact ids. Separate ids with comma, semicolon, or pipe", SaveSetArtifacts);
        DebugLogConsole.AddCommand("/save_clear_progress_lists", "Clear unlocked, completed, and artifact progress lists", SaveClearProgressLists);
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
                $"scene={s.sceneName} | " +
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
            $"scene={s.CurrentSceneName} | " +
            $"loc={s.CurrentLocation} | " +
            $"comp={s.CurrentCompanionId} | " +
            $"unlocked=[{FormatIds(s.UnlockedLevelIds)}] | " +
            $"completed=[{FormatIds(s.CompletedLevelIds)}] | " +
            $"artifacts=[{FormatIds(s.DiscoveredArtifactIds)}]");
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

    private static void SaveEnter(int slotIndex)
    {
        bool success = Game.Ctx.Saves.LoadFromSlotAndEnterScene(slotIndex);

        if (!success)
        {
            Debug.LogWarning($"Load and enter failed for slot {slotIndex}");
        }
    }
    
    private static void SaveDelete(int slotIndex)
    {
        Game.Ctx.Saves.DeleteSlot(slotIndex);
    }

    private static void SaveSetScene(string sceneName)
    {
        Game.Ctx.SaveState.CurrentSceneName = sceneName;
        Debug.Log($"Set scene -> {sceneName}");
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

        ctx.SaveState.CurrentSceneName = SceneManager.GetActiveScene().name;
        ctx.SaveState.CurrentLocation = $"TestZone_{slot}";
        ctx.SaveState.CurrentCompanionId = $"Companion_{slot}";
        ctx.SaveState.SetPlayTime(UnityEngine.Random.Range(10f, 500f));
        ctx.SaveState.SetUnlockedLevels(new[] { "debug_planet_intro", $"debug_level_{slot}" });
        ctx.SaveState.SetCompletedLevels(new[] { "debug_planet_intro" });
        ctx.SaveState.SetDiscoveredArtifacts(new[] { $"debug_artifact_{slot}_a" });

        ctx.Saves.SaveToSlot(slot);

        Debug.Log($"Filled slot {slot} with test data");
    }
    
    private static void SaveOpenFolder()
    {
        string path = Game.Ctx.Saves.SaveDirectoryPath;
        Debug.Log($"Opening: {path}");
        Application.OpenURL("file://" + path);
    }

    private static void SaveUnlockLevel(string levelId)
    {
        bool changed = Game.Ctx.SaveState.UnlockLevel(levelId);
        Debug.Log($"{(changed ? "Unlocked" : "Already unlocked or invalid")}: {levelId}");
    }

    private static void SaveLockLevel(string levelId)
    {
        bool changed = Game.Ctx.SaveState.LockLevel(levelId);
        Debug.Log($"{(changed ? "Locked" : "Was not unlocked or invalid")}: {levelId}");
    }

    private static void SaveCompleteLevel(string levelId)
    {
        bool changed = Game.Ctx.SaveState.CompleteLevel(levelId);
        Debug.Log($"{(changed ? "Completed" : "Already completed or invalid")}: {levelId}");
    }

    private static void SaveUncompleteLevel(string levelId)
    {
        bool changed = Game.Ctx.SaveState.UncompleteLevel(levelId);
        Debug.Log($"{(changed ? "Marked incomplete" : "Was not completed or invalid")}: {levelId}");
    }

    private static void SaveDiscoverArtifact(string artifactId)
    {
        bool changed = Game.Ctx.SaveState.DiscoverArtifact(artifactId);
        Debug.Log($"{(changed ? "Discovered" : "Already discovered or invalid")}: {artifactId}");
    }

    private static void SaveForgetArtifact(string artifactId)
    {
        bool changed = Game.Ctx.SaveState.ForgetArtifact(artifactId);
        Debug.Log($"{(changed ? "Forgot" : "Was not discovered or invalid")}: {artifactId}");
    }

    private static void SaveSetUnlocked(string rawIds)
    {
        Game.Ctx.SaveState.SetUnlockedLevels(ParseIds(rawIds));
        Debug.Log($"Unlocked levels -> [{FormatIds(Game.Ctx.SaveState.UnlockedLevelIds)}]");
    }

    private static void SaveSetCompleted(string rawIds)
    {
        Game.Ctx.SaveState.SetCompletedLevels(ParseIds(rawIds));
        Debug.Log($"Completed levels -> [{FormatIds(Game.Ctx.SaveState.CompletedLevelIds)}]");
    }

    private static void SaveSetArtifacts(string rawIds)
    {
        Game.Ctx.SaveState.SetDiscoveredArtifacts(ParseIds(rawIds));
        Debug.Log($"Discovered artifacts -> [{FormatIds(Game.Ctx.SaveState.DiscoveredArtifactIds)}]");
    }

    private static void SaveClearProgressLists()
    {
        Game.Ctx.SaveState.ClearUnlockedLevels();
        Game.Ctx.SaveState.ClearCompletedLevels();
        Game.Ctx.SaveState.ClearDiscoveredArtifacts();
        Debug.Log("Cleared unlocked levels, completed levels, and discovered artifacts.");
    }

    private static IEnumerable<string> ParseIds(string rawIds)
    {
        if (string.IsNullOrWhiteSpace(rawIds))
            yield break;

        string[] parts = rawIds.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string part in parts)
        {
            string id = part.Trim();

            if (id.Length > 0)
                yield return id;
        }
    }

    private static string FormatIds(IEnumerable<string> ids)
    {
        if (ids == null)
            return string.Empty;

        List<string> list = new List<string>();

        foreach (string id in ids)
        {
            if (!string.IsNullOrWhiteSpace(id))
                list.Add(id.Trim());
        }

        list.Sort(StringComparer.Ordinal);
        return string.Join(", ", list);
    }
}
