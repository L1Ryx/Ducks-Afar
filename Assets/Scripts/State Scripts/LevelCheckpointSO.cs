using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelCheckpoint", menuName = "Beats/LevelCheckpoint")]
public class LevelCheckpointSO : ScriptableObject
{
    [Header("Metadata")]
    [SerializeField] private string checkpointId = "GameplayStart";
    [TextArea] [SerializeField] private string description;

    [Header("Restart Setup")]
    [Tooltip("Raised before the beat resumes from this checkpoint.")]
    [SerializeField] private List<GameEvent> setupEvents = new();

    public string CheckpointId => checkpointId;
    public string Description => description;
    public IReadOnlyList<GameEvent> SetupEvents => setupEvents;

    public void ApplySetup()
    {
        foreach (var evt in setupEvents)
            evt?.Raise();
    }
}
