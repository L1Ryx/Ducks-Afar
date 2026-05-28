using System.IO;
using IngameDebugConsole;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DevSceneCommands : MonoBehaviour
{
#if UNITY_EDITOR
    private void Awake()
    {
        DebugLogConsole.AddCommand<string>("/scene", "Load scene by name. Use quotes for spaces, or underscores instead of spaces.", LoadScene);
        DebugLogConsole.AddCommand<string>("/scene_load", "Load scene by name. Alias for /scene.", LoadScene);
        DebugLogConsole.AddCommand("/scene_reload", "Reload the active scene through SceneLoadSystem.", ReloadScene);
        DebugLogConsole.AddCommand("/scene_title", "Load the Title Screen scene.", LoadTitleScene);
        DebugLogConsole.AddCommand("/scene_list", "List scenes included in Build Settings.", ListScenes);
    }

    private static void LoadScene(string requestedSceneName)
    {
        if (!TryGetSceneLoader(out SceneLoadSystem loader))
            return;

        string sceneName = ResolveBuildSceneName(requestedSceneName);
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"Scene command failed: no Build Settings scene matched '{requestedSceneName}'.");
            return;
        }

        loader.LoadScene(sceneName);
    }

    private static void ReloadScene()
    {
        if (TryGetSceneLoader(out SceneLoadSystem loader))
            loader.ReloadActiveScene();
    }

    private static void LoadTitleScene()
    {
        LoadScene("Title Screen");
    }

    private static void ListScenes()
    {
        int count = SceneManager.sceneCountInBuildSettings;
        if (count <= 0)
        {
            Debug.Log("No scenes are currently listed in Build Settings.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            Debug.Log($"{i}: {Path.GetFileNameWithoutExtension(path)} ({path})");
        }
    }

    private static bool TryGetSceneLoader(out SceneLoadSystem loader)
    {
        loader = null;

        if (!Game.IsReady || Game.Ctx?.SceneLoader == null)
        {
            Debug.LogWarning("Scene command failed: GameContext/SceneLoader is not ready.");
            return false;
        }

        loader = Game.Ctx.SceneLoader;
        return true;
    }

    private static string ResolveBuildSceneName(string requestedSceneName)
    {
        if (string.IsNullOrWhiteSpace(requestedSceneName))
            return string.Empty;

        string trimmed = requestedSceneName.Trim();
        string spaceVariant = trimmed.Replace("_", " ");

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = Path.GetFileNameWithoutExtension(path);

            if (Matches(sceneName, trimmed) || Matches(sceneName, spaceVariant))
                return sceneName;
        }

        return string.Empty;
    }

    private static bool Matches(string sceneName, string requestedSceneName)
    {
        return string.Equals(sceneName, requestedSceneName, System.StringComparison.OrdinalIgnoreCase);
    }
#endif
}
