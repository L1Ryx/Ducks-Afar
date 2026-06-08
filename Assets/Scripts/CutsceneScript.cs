using System.Collections;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class CutsceneScript : MonoBehaviour
{
    [SerializeField]
    private GameEvent onVideoEnd;

    [Header("Playback")]
    [SerializeField] private bool useUnscaledVideoTime = true;

    [Header("End Hold")]
    [SerializeField, Min(0f)] private float endBlackHoldSeconds = 0f;
    [SerializeField] private bool hideVideoDuringEndHold = true;
    [SerializeField] private bool useUnscaledTimeForEndHold;

    private VideoPlayer videoPlayer;
    private Coroutine endHoldRoutine;
    private float initialTargetCameraAlpha = 1f;

    private void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        initialTargetCameraAlpha = videoPlayer.targetCameraAlpha;
        ApplyPlaybackSettings();
    }

    private void OnEnable()
    {
        videoPlayer.loopPointReached += HandleVideoEnd;
        videoPlayer.errorReceived += HandleVideoError;
    }

    private void OnDisable()
    {
        videoPlayer.loopPointReached -= HandleVideoEnd;
        videoPlayer.prepareCompleted -= HandlePreparedAndPlay;
        videoPlayer.errorReceived -= HandleVideoError;

        if (endHoldRoutine != null)
        {
            StopCoroutine(endHoldRoutine);
            endHoldRoutine = null;
        }
    }
    
    public void PlayVideo()
    {
        if (endHoldRoutine != null)
        {
            StopCoroutine(endHoldRoutine);
            endHoldRoutine = null;
        }

        videoPlayer.targetCameraAlpha = initialTargetCameraAlpha;
        ApplyPlaybackSettings();

        // Seek after Prepare on Windows. Seeking before preparation can leave some
        // decoders showing the first frame without advancing.
        videoPlayer.prepareCompleted -= HandlePreparedAndPlay;

        if (videoPlayer.isPrepared)
        {
            SeekToStart(videoPlayer);
            videoPlayer.Play();
        }
        else
        {
            videoPlayer.prepareCompleted += HandlePreparedAndPlay;
            videoPlayer.Prepare();
        }
    }

    private void HandlePreparedAndPlay(VideoPlayer source)
    {
        source.prepareCompleted -= HandlePreparedAndPlay;
        SeekToStart(source);
        source.Play();
    }

    private void ApplyPlaybackSettings()
    {
        if (!useUnscaledVideoTime)
            return;

        videoPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
    }

    private static void SeekToStart(VideoPlayer source)
    {
        source.frame = 0;
        source.time = 0;
    }

    private static void HandleVideoError(VideoPlayer source, string message)
    {
        Debug.LogError($"Cutscene video failed to play '{source.clip?.name}': {message}", source);
    }

    private void HandleVideoEnd(VideoPlayer source)
    {
        if (endHoldRoutine != null)
            StopCoroutine(endHoldRoutine);

        endHoldRoutine = StartCoroutine(VideoEndRoutine());
    }

    private IEnumerator VideoEndRoutine()
    {
        if (hideVideoDuringEndHold)
            videoPlayer.targetCameraAlpha = 0f;

        if (endBlackHoldSeconds > 0f)
        {
            if (useUnscaledTimeForEndHold)
                yield return new WaitForSecondsRealtime(endBlackHoldSeconds);
            else
                yield return new WaitForSeconds(endBlackHoldSeconds);
        }

        endHoldRoutine = null;
        onVideoEnd?.Raise();
    }
}
