using UnityEngine;
using UnityEngine.Video;

public class CutsceneScript : MonoBehaviour
{
    [SerializeField]
    private GameEvent onVideoEnd;

    private VideoPlayer videoPlayer;

    private void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
    }

    private void OnEnable()
    {
        videoPlayer.loopPointReached += HandleVideoEnd;
    }

    private void OnDisable()
    {
        videoPlayer.loopPointReached -= HandleVideoEnd;
    }
    
    public void PlayVideo()
    {
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
        onVideoEnd?.Raise();
    }
}
