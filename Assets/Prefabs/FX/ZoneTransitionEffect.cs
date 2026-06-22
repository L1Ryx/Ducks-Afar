using UnityEngine;

public sealed class ZoneTransitionEffect : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private GameEvent triggerEvent;

    [Header("Effects")]
    [SerializeField] private PostProcessBurstHandler postProcessBurst;
    [SerializeField] private AudioCue audioCue;

    [Header("Audio")]
    [SerializeField] private bool playAudioOnThisEmitter;

    private void Reset()
    {
        postProcessBurst = GetComponent<PostProcessBurstHandler>();
    }

    private void Awake()
    {
        if (postProcessBurst == null)
            postProcessBurst = GetComponent<PostProcessBurstHandler>();
    }

    private void OnEnable()
    {
        triggerEvent?.RegisterRuntimeListener(Play);
    }

    private void OnDisable()
    {
        triggerEvent?.UnregisterRuntimeListener(Play);
    }

    public void Play()
    {
        if (postProcessBurst != null)
            postProcessBurst.Burst();

        if (playAudioOnThisEmitter)
            ProjectAudio.PlayOn(audioCue, gameObject);
        else
            ProjectAudio.PlayGlobal(audioCue);
    }

    public void ResetToIdle()
    {
        if (postProcessBurst != null)
            postProcessBurst.ResetToIdle();
    }
}
