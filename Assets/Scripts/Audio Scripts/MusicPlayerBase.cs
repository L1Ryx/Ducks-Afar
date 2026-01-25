using System.Collections;
using UnityEngine;

/// <summary>
/// Prefab-friendly controller for global music ownership.
/// Base class provides Play/Stop lifecycle and start-delay readiness gating.
/// Concrete subclasses may add additional controls (e.g., Wwise State switching).
/// </summary>
public abstract class MusicPlayerBase : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] protected AudioCue musicCue;

    [SerializeField] private float startDelay = 0.05f;

    [Header("Behavior")]
    [Tooltip("If true, Stop() is called automatically when this object is disabled/destroyed.")]
    [SerializeField] private bool stopOnDisable = true;

    [Tooltip("If true, Play() is called automatically on Start().")]
    [SerializeField] private bool playOnStart = false;

    private bool _hasRequestedPlay;
    private Coroutine _playRoutine;

    protected virtual void Start()
    {
        if (playOnStart)
            Play();
    }

    protected virtual void OnDisable()
    {
        if (!stopOnDisable)
            return;

        // Only stop if we previously started (prevents accidental Stop calls).
        if (_hasRequestedPlay)
            Stop();
    }

    /// <summary>
    /// Starts this music as the global music via AudioStateModel ownership.
    /// Safe to call multiple times (AudioStateModel should be idempotent).
    /// </summary>
    public virtual void Play()
    {
        _hasRequestedPlay = true;

        if (_playRoutine != null)
            StopCoroutine(_playRoutine);

        _playRoutine = StartCoroutine(PlayWhenReady());
    }

    private IEnumerator PlayWhenReady()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        if (!Game.IsReady)
        {
            Debug.LogWarning($"{GetType().Name}: GameContext not ready. Cannot play music.", this);
            yield break;
        }

        if (musicCue == null || !musicCue.HasPlayEvent)
        {
            Debug.LogWarning($"{GetType().Name}: No valid AudioCue assigned.", this);
            yield break;
        }

        // Allow subclasses to set defaults (e.g., key state) before playback if desired.
        BeforePlay();

        // Requires AudioStateModel to expose SetGlobalMusic(AudioCue).
        Game.Ctx.Audio.SetGlobalMusic(musicCue);
    }

    /// <summary>
    /// Hook for subclasses (e.g., set default Wwise states before playing).
    /// </summary>
    protected virtual void BeforePlay() { }

    /// <summary>
    /// Stops the currently owned global music via AudioStateModel.
    /// </summary>
    public virtual void Stop()
    {
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        if (!Game.IsReady)
        {
            Debug.LogWarning($"{GetType().Name}: GameContext not ready. Cannot stop music.", this);
            return;
        }

        // Requires AudioStateModel to expose StopGlobalMusic(bool immediate).
        Game.Ctx.Audio.StopGlobalMusic(immediate: false);
        _hasRequestedPlay = false;
    }
}
