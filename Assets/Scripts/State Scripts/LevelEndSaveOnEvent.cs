using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class LevelEndSaveOnEvent : MonoBehaviour
{
    [Header("Events")]
    [Tooltip("When this event is raised, the active save slot is written.")]
    [SerializeField] private GameEvent triggerEvent;
    [Tooltip("Raised after the active save slot is successfully written.")]
    [SerializeField] private GameEvent savedEvent;

    [Header("Saving")]
    [SerializeField] private bool saveActiveSlotOnTrigger = true;
    [Tooltip("If true, captures the current scene checkpoint before saving. Leave off for level-end progress saves.")]
    [SerializeField] private bool captureSceneCheckpointOnSave = false;
    [SerializeField] private bool logSkippedSaves = true;

    private void OnEnable()
    {
        triggerEvent?.RegisterRuntimeListener(SaveActiveSlot);
    }

    private void OnDisable()
    {
        triggerEvent?.UnregisterRuntimeListener(SaveActiveSlot);
    }

    public void SaveActiveSlot()
    {
        if (!saveActiveSlotOnTrigger)
            return;

        if (!Game.IsReady || Game.Ctx?.Saves == null || Game.Ctx.SaveState == null)
        {
            LogSkipped("GameContext is not ready.");
            return;
        }

        if (!Game.Ctx.SaveState.HasActiveSlot)
        {
            LogSkipped("no active save slot is loaded.");
            return;
        }

        if (!Game.Ctx.Saves.SaveToActiveSlot(captureSceneCheckpointOnSave))
            return;

        if (savedEvent != null)
        {
            savedEvent.Raise();
        }
        else
        {
            Debug.LogWarning($"{nameof(LevelEndSaveOnEvent)} saved successfully, but no savedEvent is assigned.", this);
        }
    }

    private void LogSkipped(string reason)
    {
        if (logSkippedSaves)
            Debug.LogWarning($"{nameof(LevelEndSaveOnEvent)} skipped level-end save because {reason}", this);
    }

#if UNITY_EDITOR
    private void Reset()
    {
        AssignEditorDefaults();
    }

    private void OnValidate()
    {
        AssignEditorDefaults();
    }

    private void AssignEditorDefaults()
    {
        if (triggerEvent == null)
            triggerEvent = AssetDatabase.LoadAssetAtPath<GameEvent>("Assets/SOs/Events/OnLevelCurtainFullyBlack.asset");

        if (savedEvent == null)
            savedEvent = AssetDatabase.LoadAssetAtPath<GameEvent>("Assets/SOs/Events/OnGameSaved.asset");
    }
#endif
}
