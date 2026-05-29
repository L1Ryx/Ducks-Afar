using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class SceneLoadSystem
{
    private readonly GameContext ctx;
    private LoadingScreenView loadingScreen;

    public SceneLoadSystem(GameContext ctx)
    {
        this.ctx = ctx;
    }

    public bool IsLoading { get; private set; }

    public IEnumerator WaitUntilLoadComplete()
    {
        while (IsLoading)
            yield return null;
    }

    public void PrewarmLoadingScreen()
    {
        GetLoadingScreen().HideImmediate();
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
#if UNITY_EDITOR
            if (IsSceneEnabledInEditorBuildSettings(sceneName))
                return true;
#endif

            Debug.LogError(
                $"Scene load failed: scene '{sceneName}' is not in Build Settings or cannot be loaded.");
            return false;
        }

        return true;
    }

    public bool LoadScene(string sceneName)
    {
        return LoadScene(sceneName, null);
    }

    public bool LoadScene(string sceneName, string loadingMessage)
    {
        return LoadScene(sceneName, loadingMessage, null);
    }

    public bool LoadScene(string sceneName, string loadingMessage, Action beforeSceneLoad)
    {
        if (!CanLoadScene(sceneName))
            return false;

        return StartSceneLoad(
            loadingMessage,
            () =>
            {
                beforeSceneLoad?.Invoke();
                return sceneName;
            });
    }

    public bool LoadSceneDeferred(string loadingMessage, Func<string> resolveSceneName)
    {
        if (resolveSceneName == null)
        {
            Debug.LogError("Scene load failed: no deferred scene resolver was provided.");
            return false;
        }

        return StartSceneLoad(loadingMessage, resolveSceneName);
    }

    private bool StartSceneLoad(string loadingMessage, Func<string> resolveSceneName)
    {
        if (IsLoading)
        {
            Debug.LogWarning("Scene load ignored: already loading a scene.");
            return false;
        }

        ctx.StartCoroutine(LoadSceneRoutine(loadingMessage, resolveSceneName));
        return true;
    }

    public void ReloadActiveScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        ctx.LevelCheckpoints?.PrepareRestart(sceneName);
        LoadScene(sceneName);
    }

    public void PrepareForSceneLoad()
    {
        ctx.Dialogue?.CancelDialogue();
        ctx.Audio?.StopGlobalAmbience(immediate: false);
        ctx.LevelState?.Reset();
        ctx.InteractionLock?.ForceClear();
        ctx.Inventory?.Clear();
        ctx.Pause?.Resume();
    }

    private IEnumerator LoadSceneRoutine(
        string loadingMessage,
        Func<string> resolveSceneName)
    {
        IsLoading = true;

        LoadingScreenView screen = GetLoadingScreen();
        yield return screen.Show(loadingMessage);
        yield return null;

        float visibleStartTime = Time.unscaledTime;

        string sceneName = null;
        Exception preLoadException = null;

        try
        {
            sceneName = resolveSceneName.Invoke();
        }
        catch (Exception ex)
        {
            preLoadException = ex;
        }

        if (preLoadException != null)
        {
            Debug.LogError($"Scene load failed during pre-load work: {preLoadException}");
            yield return screen.Hide();
            IsLoading = false;
            yield break;
        }

        if (!CanLoadScene(sceneName))
        {
            yield return screen.Hide();
            IsLoading = false;
            yield break;
        }

        PrepareForSceneLoad();

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation == null)
        {
            Debug.LogError($"Scene load failed: Unity could not start loading scene '{sceneName}'.");
            yield return screen.Hide();
            IsLoading = false;
            yield break;
        }

        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
            yield return null;

        yield return WaitForMinimumVisibleTime(screen, visibleStartTime);

        operation.allowSceneActivation = true;

        while (!operation.isDone)
            yield return null;

        if (screen.PostLoadHoldDuration > 0f)
            yield return WaitForUnscaledSeconds(screen.PostLoadHoldDuration);

        yield return null;
        yield return screen.Hide();

        IsLoading = false;
    }

    private LoadingScreenView GetLoadingScreen()
    {
        if (loadingScreen != null)
            return loadingScreen;

        loadingScreen = ctx.GetComponentInChildren<LoadingScreenView>(true);

        if (loadingScreen == null)
            loadingScreen = ctx.gameObject.AddComponent<LoadingScreenView>();

        loadingScreen.EnsureInitialized();
        return loadingScreen;
    }

    private static IEnumerator WaitForMinimumVisibleTime(
        LoadingScreenView screen,
        float visibleStartTime)
    {
        float remaining = screen.MinimumVisibleDuration - (Time.unscaledTime - visibleStartTime);

        if (remaining > 0f)
            yield return WaitForUnscaledSeconds(remaining);
    }

    private static IEnumerator WaitForUnscaledSeconds(float seconds)
    {
        float endTime = Time.unscaledTime + seconds;

        while (Time.unscaledTime < endTime)
            yield return null;
    }

#if UNITY_EDITOR
    private static bool IsSceneEnabledInEditorBuildSettings(string sceneName)
    {
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled)
                continue;

            string buildSceneName = System.IO.Path.GetFileNameWithoutExtension(scene.path);
            if (string.Equals(buildSceneName, sceneName, StringComparison.Ordinal) ||
                string.Equals(scene.path, sceneName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
#endif
}
