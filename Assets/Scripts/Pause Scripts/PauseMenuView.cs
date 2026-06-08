using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public sealed class PauseMenuView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup root;
    [SerializeField] private GameObject firstSelectedObject;
    [SerializeField] private bool bringToFrontOnShow = true;

    [Header("Navigation")]
    [SerializeField] private string titleSceneName = "Title Screen";

    [Header("Events")]
    [SerializeField] private UnityEvent onShown;
    [SerializeField] private UnityEvent onHidden;
    [SerializeField] private UnityEvent onSettingsRequested;
    [SerializeField] private UnityEvent onExitRequested;

    private bool subscribed;

    private void Reset()
    {
        root = GetComponentInChildren<CanvasGroup>(true);
    }

    private void Awake()
    {
        SetVisible(false);
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
        SetVisible(Game.IsReady && Game.Ctx?.Pause?.IsPaused == true);
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void TrySubscribe()
    {
        if (subscribed || !Game.IsReady || Game.Ctx?.Pause == null)
            return;

        Game.Ctx.Pause.OnPauseChanged += HandlePauseChanged;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
            return;

        if (Game.IsReady && Game.Ctx?.Pause != null)
            Game.Ctx.Pause.OnPauseChanged -= HandlePauseChanged;

        subscribed = false;
    }

    private void HandlePauseChanged(bool paused)
    {
        SetVisible(paused);

        if (paused)
            onShown?.Invoke();
        else
            onHidden?.Invoke();
    }

    private void SetVisible(bool visible)
    {
        if (root == null)
            return;

        root.alpha = visible ? 1f : 0f;
        root.interactable = visible;
        root.blocksRaycasts = visible;

        if (visible && bringToFrontOnShow)
            root.transform.SetAsLastSibling();

        if (firstSelectedObject != null)
            EventSystem.current?.SetSelectedGameObject(visible ? firstSelectedObject : null);
    }

    public void Resume()
    {
        Game.Ctx?.Pause?.Resume();
    }

    public void ShowPanel()
    {
        SetVisible(true);
    }

    public void HidePanel()
    {
        SetVisible(false);
    }

    public void RequestSettings()
    {
        onSettingsRequested?.Invoke();
    }

    public void RequestExit()
    {
        onExitRequested?.Invoke();
    }

    public void ReturnToTitleScreen()
    {
        if (Game.IsReady && Game.Ctx?.SceneLoader != null)
        {
            Game.Ctx.SceneLoader.LoadScene(
                titleSceneName,
                null,
                () => Game.Ctx.Saves?.SaveToActiveSlot());
            return;
        }

        SceneManager.LoadScene(titleSceneName);
    }
}
