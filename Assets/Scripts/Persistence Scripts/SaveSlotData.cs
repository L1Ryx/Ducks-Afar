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
    public bool hasPlayerPosition;
    public string playerPositionSceneName;
    public float playerPositionX;
    public float playerPositionY;
    public float playerPositionZ;
    public int goldworms;
    public bool hasCollectedGoldworms;

    public List<string> unlockedLevelIds = new();
    public List<string> completedLevelIds = new();
    public List<string> discoveredArtifactIds = new();
    public List<string> worldStateIds = new();
    public List<SceneVariantSaveData> sceneVariants = new();

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
            hasPlayerPosition = false,
            playerPositionSceneName = string.Empty,
            playerPositionX = 0f,
            playerPositionY = 0f,
            playerPositionZ = 0f,
            goldworms = 0,
            hasCollectedGoldworms = false,
            unlockedLevelIds = new List<string>(),
            completedLevelIds = new List<string>(),
            discoveredArtifactIds = new List<string>(),
            worldStateIds = new List<string>(),
            sceneVariants = new List<SceneVariantSaveData>(),
            lastSavedUtc = string.Empty
        };
    }
}

[Serializable]
public sealed class SceneVariantSaveData
{
    public string sceneName;
    public string variantId;
}
