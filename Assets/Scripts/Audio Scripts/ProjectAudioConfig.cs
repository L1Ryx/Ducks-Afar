using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Project Audio Config", fileName = "ProjectAudioConfig")]
public sealed class ProjectAudioConfig : ScriptableObject
{
    private const string ResourcePath = "ProjectAudioConfig";

    private static ProjectAudioConfig cachedInstance;

    [Header("Telescope")]
    [SerializeField] private AudioCue telescopeOpenCue;
    [SerializeField] private AudioCue telescopeCloseCue;

    [Header("Menu UI")]
    [SerializeField] private AudioCue hoverBeginCue;
    [SerializeField] private AudioCue hoverEndCue;
    [SerializeField] private AudioCue woodenClickCue;

    [Header("Restart")]
    [SerializeField] private AudioCue restartSuccessCue;
    [SerializeField] private AudioRtpc restartProgressRtpc;

    public static ProjectAudioConfig Instance
    {
        get
        {
            if (cachedInstance == null)
                cachedInstance = Resources.Load<ProjectAudioConfig>(ResourcePath);

            return cachedInstance;
        }
    }

    public AudioCue TelescopeOpenCue => telescopeOpenCue;
    public AudioCue TelescopeCloseCue => telescopeCloseCue;
    public AudioCue HoverBeginCue => hoverBeginCue;
    public AudioCue HoverEndCue => hoverEndCue;
    public AudioCue WoodenClickCue => woodenClickCue;
    public AudioCue RestartSuccessCue => restartSuccessCue;
    public AudioRtpc RestartProgressRtpc => restartProgressRtpc;
}
