using System;
using System.Collections.Generic;

[Serializable]
public sealed class SaveSlotData
{
    public bool hasData;
    public int slotIndex;

    public float timePlayedSeconds;
    public string sceneName;
    public string location;
    public string companionId;

    public List<string> unlockedLevelIds = new();
    public List<string> completedLevelIds = new();
    public List<string> discoveredArtifactIds = new();

    public string lastSavedUtc;

    public static SaveSlotData CreateEmpty(int slotIndex)
    {
        return new SaveSlotData
        {
            hasData = false,
            slotIndex = slotIndex,
            timePlayedSeconds = 0f,
            sceneName = string.Empty,
            location = string.Empty,
            companionId = "NONE",
            unlockedLevelIds = new List<string>(),
            completedLevelIds = new List<string>(),
            discoveredArtifactIds = new List<string>(),
            lastSavedUtc = string.Empty
        };
    }
}
