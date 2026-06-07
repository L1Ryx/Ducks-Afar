using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(50)]
public sealed class MenuButtonAudioSystem : MonoBehaviour
{
    [SerializeField] private ProjectAudioConfig configOverride;
    [SerializeField, Min(0.1f)] private float refreshInterval = 0.5f;
    [SerializeField] private bool includeInactiveButtons = true;

    private float nextRefreshTime;

    private ProjectAudioConfig Config => configOverride != null ? configOverride : ProjectAudio.Config;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        RefreshButtons();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime)
            return;

        RefreshButtons();
    }

    public bool CanPlayFor(Button button)
    {
        return button != null && button.IsActive() && button.IsInteractable();
    }

    public void PlayHoverBegin(GameObject emitter)
    {
        ProjectAudio.PlayOn(Config != null ? Config.HoverBeginCue : null, emitter);
    }

    public void PlayHoverEnd(GameObject emitter)
    {
        ProjectAudio.PlayOn(Config != null ? Config.HoverEndCue : null, emitter);
    }

    public void PlaySelection(GameObject emitter)
    {
        ProjectAudio.PlayOn(Config != null ? Config.ButtonClickCue : null, emitter);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        nextRefreshTime = Time.unscaledTime + refreshInterval;

        var inactiveMode = includeInactiveButtons
            ? FindObjectsInactive.Include
            : FindObjectsInactive.Exclude;

        Button[] buttons = Object.FindObjectsByType<Button>(inactiveMode, FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            var target = button.GetComponent<MenuButtonAudioTarget>();
            if (target == null)
                target = button.gameObject.AddComponent<MenuButtonAudioTarget>();

            target.Initialize(this, button);
        }
    }
}
