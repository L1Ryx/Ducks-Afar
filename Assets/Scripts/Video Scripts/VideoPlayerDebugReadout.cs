using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class VideoPlayerDebugReadout : MonoBehaviour
{
    private VideoPlayer videoPlayer;
    private float nextLogTime;

    private void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.errorReceived += HandleError;
        videoPlayer.prepareCompleted += HandlePrepared;
        videoPlayer.started += HandleStarted;
        videoPlayer.loopPointReached += HandleEnded;
    }

    private void OnDestroy()
    {
        if (videoPlayer == null)
            return;

        videoPlayer.errorReceived -= HandleError;
        videoPlayer.prepareCompleted -= HandlePrepared;
        videoPlayer.started -= HandleStarted;
        videoPlayer.loopPointReached -= HandleEnded;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextLogTime)
            return;

        nextLogTime = Time.unscaledTime + 0.5f;

        Debug.Log(
            $"[VideoDebug] clip={videoPlayer.clip?.name}, " +
            $"isPrepared={videoPlayer.isPrepared}, isPlaying={videoPlayer.isPlaying}, " +
            $"time={videoPlayer.time:0.000}, frame={videoPlayer.frame}, " +
            $"frameCount={videoPlayer.frameCount}, frameRate={videoPlayer.frameRate:0.##}, " +
            $"timeUpdateMode={videoPlayer.timeUpdateMode}, " +
            $"audioMode={videoPlayer.audioOutputMode}, audioTracks={videoPlayer.controlledAudioTrackCount}",
            this);
    }

    private void HandlePrepared(VideoPlayer source)
    {
        Debug.Log($"[VideoDebug] Prepared {source.clip?.name}", this);
    }

    private void HandleStarted(VideoPlayer source)
    {
        Debug.Log($"[VideoDebug] Started {source.clip?.name}", this);
    }

    private void HandleEnded(VideoPlayer source)
    {
        Debug.Log($"[VideoDebug] Ended {source.clip?.name}", this);
    }

    private void HandleError(VideoPlayer source, string message)
    {
        Debug.LogError($"[VideoDebug] Error on {source.clip?.name}: {message}", this);
    }
}