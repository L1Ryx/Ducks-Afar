using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrapper : MonoBehaviour
{
    [SerializeField] private string defaultFirstSceneName = "Title Screen";

    private void Start()
    {
        string targetScene =
            string.IsNullOrEmpty(AutoBootstrapRedirector.BootstrapHandoff.PendingSceneName)
                ? defaultFirstSceneName
                : AutoBootstrapRedirector.BootstrapHandoff.PendingSceneName;

        // Clear handoff so future loads are clean
        AutoBootstrapRedirector.BootstrapHandoff.PendingSceneName = null;

        if (Game.IsReady && Game.Ctx?.SceneLoader != null && Game.Ctx.SceneLoader.LoadScene(targetScene))
            return;

        SceneManager.LoadScene(targetScene);
    }
}
