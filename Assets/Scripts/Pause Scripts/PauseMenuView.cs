using UnityEngine;
using UnityEngine.Events;

public sealed class PauseMenuView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup root;
    [SerializeField] private GameObject firstSelectedObject;

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

        if (firstSelectedObject != null)
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(visible ? firstSelectedObject : null);
    }

    public void Resume()
    {
        Game.Ctx?.Pause?.Resume();
    }

    public void RequestSettings()
    {
        onSettingsRequested?.Invoke();
    }

    public void RequestExit()
    {
        onExitRequested?.Invoke();
    }
}
