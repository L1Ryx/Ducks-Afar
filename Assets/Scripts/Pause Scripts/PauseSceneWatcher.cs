using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PauseSceneWatcher : MonoBehaviour
{
    [SerializeField] private bool defaultPausableWhenNoMarkerFound = false;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        StartCoroutine(ApplyAfterSceneObjectsWake());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(ApplyAfterSceneObjectsWake());
    }

    private IEnumerator ApplyAfterSceneObjectsWake()
    {
        yield return null;
        ApplyCurrentSceneSettings();
    }

    public void ApplyCurrentSceneSettings()
    {
        if (!Game.IsReady || Game.Ctx?.Pause == null)
            return;

        var settings = FindActiveSceneSettings();
        if (settings != null)
        {
            settings.Apply();
            return;
        }

        Game.Ctx.Pause.SetScenePausable(
            defaultPausableWhenNoMarkerFound,
            "No SceneSettings marker found in the active scene."
        );
        ApplySceneRestartAllowed(false);
    }

    private static void ApplySceneRestartAllowed(bool allowRestart)
    {
        if (Game.Ctx != null && Game.Ctx.TryGetComponent(out HoldToResetController holdToReset))
            holdToReset.SetSceneRestartAllowed(allowRestart);
    }

    private static SceneSettings FindActiveSceneSettings()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        SceneSettings[] settings = Object.FindObjectsByType<SceneSettings>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (SceneSettings candidate in settings)
        {
            if (candidate != null && candidate.gameObject.scene == activeScene)
                return candidate;
        }

        return null;
    }
}
