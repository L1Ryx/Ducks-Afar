using System.Collections.Generic;

public sealed class LevelCheckpointModel
{
    private readonly Dictionary<string, string> checkpointByScene = new();

    private string pendingRestartSceneName;

    public void MarkCheckpoint(string sceneName, string checkpointId)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || string.IsNullOrWhiteSpace(checkpointId))
            return;

        checkpointByScene[sceneName] = checkpointId;
    }

    public void PrepareRestart(string sceneName)
    {
        pendingRestartSceneName = string.IsNullOrWhiteSpace(sceneName) ? null : sceneName;
    }

    public bool TryConsumeRestartCheckpoint(string sceneName, out string checkpointId)
    {
        checkpointId = null;

        if (string.IsNullOrWhiteSpace(sceneName) || pendingRestartSceneName != sceneName)
            return false;

        pendingRestartSceneName = null;

        if (!checkpointByScene.TryGetValue(sceneName, out var savedCheckpointId))
            return false;

        checkpointId = savedCheckpointId;
        return true;
    }
}
