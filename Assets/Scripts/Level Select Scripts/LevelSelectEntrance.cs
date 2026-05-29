using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LevelSelectEntrance : MonoBehaviour
{
    [SerializeField] private string levelSelectSceneName = "Level Select";
    [SerializeField] private string loadingMessage = "Opening map...";

    public void EnterLevelSelect()
    {
        if (string.IsNullOrWhiteSpace(levelSelectSceneName))
        {
            Debug.LogWarning($"{nameof(LevelSelectEntrance)}: levelSelectSceneName is empty.", this);
            return;
        }

        if (Game.IsReady && Game.Ctx?.SceneLoader != null)
        {
            Game.Ctx.SceneLoader.LoadScene(levelSelectSceneName, loadingMessage);
            return;
        }

        SceneManager.LoadScene(levelSelectSceneName);
    }
}
