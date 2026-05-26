using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelBeat", menuName = "Beats/LevelBeat")]
public class LevelBeatSO : ScriptableObject
{
    [Header("Metadata")]
    [SerializeField] private string beatId;
    [TextArea] [SerializeField] private string description;

    public string Description => description;
    [Header("Beat Actions")]
    [Tooltip("Raised immediately when this beat becomes active (in order).")]
    [SerializeField] private List<GameEvent> onEnterEvents = new();

    [Header("Checkpoint")]
    [SerializeField] private LevelCheckpointSO checkpoint;
    [SerializeField] private bool markCheckpointOnEnter;

    [Header("Beat Progression")]
    [Tooltip("When this event is raised, the director advances to the next beat.")]
    [SerializeField] private GameEvent advanceEvent;

    
    public string BeatId => beatId;
    public IReadOnlyList<GameEvent> OnEnterEvents => onEnterEvents;
    public LevelCheckpointSO Checkpoint => checkpoint;
    public bool MarkCheckpointOnEnter => markCheckpointOnEnter;
    public GameEvent AdvanceEvent => advanceEvent;
}
