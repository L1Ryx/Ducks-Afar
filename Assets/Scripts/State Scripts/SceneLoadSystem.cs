using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneLoadSystem
{
    private readonly GameContext ctx;

    public SceneLoadSystem(GameContext ctx)
    {
        this.ctx = ctx;
    }

    public bool CanLoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("Scene load failed: scene name is empty.");
            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"Scene load failed: scene '{sceneName}' is not in Build Settings or cannot be loaded.");
            return false;
        }

        return true;
    }

    public bool LoadScene(string sceneName)
    {
        if (!CanLoadScene(sceneName))
            return false;

        PrepareForSceneLoad();
        SceneManager.LoadScene(sceneName);
        return true;
    }

    public void ReloadActiveScene()
    {
        PrepareForSceneLoad();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void PrepareForSceneLoad()
    {
        ctx.Audio?.StopGlobalAmbience(immediate: false);
        ctx.LevelState?.Reset();
        ctx.InteractionLock?.ForceClear();
        ctx.Inventory?.Clear();
        ctx.Pause?.Resume();
    }
}
