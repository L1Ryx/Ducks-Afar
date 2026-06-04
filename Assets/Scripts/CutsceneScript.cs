using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class CutsceneScript : MonoBehaviour
{
    [SerializeField]
    private GameEvent onVideoEnd;

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
    }

    private void OnEnable()
    {
        videoPlayer.loopPointReached += HandleVideoEnd;
    }

    private void OnDisable()
    {
        videoPlayer.loopPointReached -= HandleVideoEnd;

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

        // Optional safety: ensure we start from the beginning
        videoPlayer.time = 0;

        // If the video is already prepared, play immediately
        if (videoPlayer.isPrepared)
        {
            videoPlayer.Play();
        }
        else
        {
            // Prepare first, then play
            videoPlayer.prepareCompleted += HandlePreparedAndPlay;
            videoPlayer.Prepare();
        }
    }

    private void HandlePreparedAndPlay(VideoPlayer source)
    {
        source.prepareCompleted -= HandlePreparedAndPlay;
        source.Play();
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
