using UnityEngine;

public sealed class PauseTimeScaleDriver : MonoBehaviour
{
    [Header("Time Stop")]
    [SerializeField] private float pausedTimeScale = 0f;
    [SerializeField] private bool restorePreviousTimeScaleOnResume = true;
    [SerializeField] private float fallbackResumeTimeScale = 1f;

    private bool subscribed;
    private float previousTimeScale = 1f;

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
        Apply(Game.IsReady && Game.Ctx?.Pause?.IsPaused == true);
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (Time.timeScale == pausedTimeScale)
            Time.timeScale = restorePreviousTimeScaleOnResume ? previousTimeScale : fallbackResumeTimeScale;
    }

    private void TrySubscribe()
    {
        if (subscribed || !Game.IsReady || Game.Ctx?.Pause == null)
            return;

        Game.Ctx.Pause.OnPauseChanged += Apply;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
            return;

        if (Game.IsReady && Game.Ctx?.Pause != null)
            Game.Ctx.Pause.OnPauseChanged -= Apply;

        subscribed = false;
    }

    private void Apply(bool paused)
    {
        if (paused)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = pausedTimeScale;
            return;
        }

        Time.timeScale = restorePreviousTimeScaleOnResume ? previousTimeScale : fallbackResumeTimeScale;
    }
}
