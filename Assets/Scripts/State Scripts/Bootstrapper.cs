using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrapper : MonoBehaviour
{
    [SerializeField] private string defaultFirstSceneName = "Studio Splash";
    [SerializeField] private SceneLoadPresentation initialLoadPresentation = SceneLoadPresentation.SilentBlack;
    [SerializeField] private string initialLoadingMessage;

    private void Start()
    {
        string targetScene =
            string.IsNullOrEmpty(AutoBootstrapRedirector.BootstrapHandoff.PendingSceneName)
                ? defaultFirstSceneName
                : AutoBootstrapRedirector.BootstrapHandoff.PendingSceneName;

        // Clear handoff so future loads are clean
        AutoBootstrapRedirector.BootstrapHandoff.PendingSceneName = null;

        if (Game.IsReady &&
            Game.Ctx?.SceneLoader != null &&
            Game.Ctx.SceneLoader.LoadScene(targetScene, initialLoadingMessage, initialLoadPresentation))
        {
            return;
        }

        SceneManager.LoadScene(targetScene);
    }
}
