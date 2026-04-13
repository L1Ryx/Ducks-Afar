using System;

[Serializable]
public sealed class SaveSlotData
{
    public bool hasData;
    public int slotIndex;

    public float timePlayedSeconds;
    public string location;
    public string companionId;

    public string lastSavedUtc;

    public static SaveSlotData CreateEmpty(int slotIndex)
    {
        return new SaveSlotData
        {
            hasData = false,
            slotIndex = slotIndex,
            timePlayedSeconds = 0f,
            location = string.Empty,
            companionId = "NONE",
            lastSavedUtc = string.Empty
        };
    }
}