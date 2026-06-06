using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneFlowSystem
{
    private readonly GameContext ctx;

    public SceneFlowSystem(GameContext ctx, SceneFlowPresetSO activePreset)
    {
        this.ctx = ctx;
        ActivePreset = activePreset;
    }

    public SceneFlowPresetSO ActivePreset { get; private set; }

    public void SetPreset(SceneFlowPresetSO preset)
    {
        ActivePreset = preset;
    }

    public bool LoadNextScene()
    {
        return LoadNextScene(null);
    }

    public bool LoadNextScene(string fallbackSceneName)
    {
        string activeSceneName = SceneManager.GetActiveScene().name;
        return LoadNextSceneFrom(activeSceneName, fallbackSceneName);
    }

    public bool LoadNextSceneFrom(string fromSceneName, string fallbackSceneName = null)
    {
        if (ctx?.SceneLoader == null)
        {
            Debug.LogWarning("Scene flow advance failed: SceneLoader is not ready.");
            return false;
        }

        if (ctx.SceneLoader.IsLoading)
        {
            Debug.LogWarning("Scene flow advance ignored: already loading a scene.");
            return false;
        }

        if (TryResolveNextScene(
                fromSceneName,
                fallbackSceneName,
                out string nextSceneName,
                out string loadingMessage,
                out SceneLoadPresentation loadPresentation))
        {
            return ctx.SceneLoader.LoadScene(nextSceneName, loadingMessage, loadPresentation);
        }

        return false;
    }

    private bool TryResolveNextScene(
        string fromSceneName,
        string fallbackSceneName,
        out string nextSceneName,
        out string loadingMessage,
        out SceneLoadPresentation loadPresentation)
    {
        nextSceneName = null;
        loadingMessage = null;
        loadPresentation = SceneLoadPresentation.LoadingScreen;

        if (ActivePreset != null && ActivePreset.TryGetTransition(fromSceneName, out SceneFlowTransition transition))
        {
            nextSceneName = transition.ToSceneName;
            loadingMessage = transition.LoadingMessage;
            loadPresentation = transition.LoadPresentation;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(fallbackSceneName))
        {
            nextSceneName = fallbackSceneName;
            return true;
        }

        if (ActivePreset == null)
        {
            Debug.LogWarning(
                $"Scene flow advance failed: no active scene flow preset is assigned for scene '{fromSceneName}'.");
        }
        else
        {
            Debug.LogWarning(
                $"Scene flow advance failed: preset '{ActivePreset.name}' has no transition from scene '{fromSceneName}'.");
        }

        return false;
    }
}
