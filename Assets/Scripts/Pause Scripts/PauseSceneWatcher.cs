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

        var settings = Object.FindFirstObjectByType<ScenePauseSettings>(FindObjectsInactive.Include);
        if (settings != null)
        {
            Game.Ctx.Pause.SetScenePausable(settings.IsPausable, settings.Reason);
            return;
        }

        Game.Ctx.Pause.SetScenePausable(
            defaultPausableWhenNoMarkerFound,
            "No ScenePauseSettings marker found in the active scene."
        );
    }
}
