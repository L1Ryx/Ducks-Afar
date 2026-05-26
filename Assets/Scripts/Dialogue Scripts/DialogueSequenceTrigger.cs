using System.Collections.Generic;
using UnityEngine;

public sealed class DialogueSequenceTrigger : MonoBehaviour
{
    [Header("Dialogue Sequence")]
    [SerializeField] private List<DialogueEncounter> encounters = new List<DialogueEncounter>();

    [Tooltip("If true, this trigger becomes inert after the last encounter is used.")]
    [SerializeField] private bool stopAfterLastEncounter = true;

    [Tooltip("If false and stopAfterLastEncounter is also false, the trigger will keep replaying the last encounter.")]
    [SerializeField] private bool loopLastEncounter = true;

    [Header("Optional Audio")]
    [SerializeField] private AudioCue onTriggerCue;

    [Header("Debug")]
    [SerializeField] private int currentEncounterIndex = 0;
    [SerializeField] private bool isExhausted = false;

    /// <summary>
    /// Call this from your SO-based GameEventListener, cutscene controller, timeline signal, etc.
    /// </summary>
    public void Trigger()
    {
        if (!Game.IsReady || Game.Ctx == null)
            return;

        if (isExhausted)
            return;

        if (Game.Ctx.Dialogue == null)
        {
            Debug.LogError($"{name}: DialogueRunner not bound in GameContext.");
            return;
        }

        if (encounters == null || encounters.Count == 0)
        {
            Debug.LogWarning($"{name}: No dialogue encounters assigned.");
            return;
        }

        // If we've exhausted the list...
        if (currentEncounterIndex >= encounters.Count)
        {
            if (stopAfterLastEncounter)
            {
                isExhausted = true;
                return;
            }

            // Otherwise, clamp to last
            currentEncounterIndex = encounters.Count - 1;
        }

        var encounter = encounters[currentEncounterIndex];
        if (encounter == null)
        {
            Debug.LogWarning($"{name}: Encounter at index {currentEncounterIndex} is null.");
            return;
        }

        if (onTriggerCue != null)
            Game.Ctx.Audio.PlayCueGlobal(onTriggerCue);

        Game.Ctx.Dialogue.StartDialogue(encounter);

        AdvanceEncounterIndex();
    }

    public void SkipNextEncounter()
    {
        if (isExhausted)
            return;

        if (encounters == null || encounters.Count == 0)
            return;

        AdvanceEncounterIndex();
    }

    // Optional helper APIs (nice for debugging or bespoke logic)
    public void ResetSequence()
    {
        currentEncounterIndex = 0;
        isExhausted = false;
    }

    public void SetEncounterIndex(int index)
    {
        currentEncounterIndex = Mathf.Clamp(index, 0, Mathf.Max(0, encounters.Count - 1));
        isExhausted = false;
    }

    private void AdvanceEncounterIndex()
    {
        if (currentEncounterIndex < encounters.Count - 1)
        {
            currentEncounterIndex++;
            return;
        }

        if (stopAfterLastEncounter)
        {
            isExhausted = true;
            currentEncounterIndex++;
            return;
        }

        if (loopLastEncounter)
        {
            currentEncounterIndex = encounters.Count - 1;
            return;
        }

        // Stay at the last encounter by default to avoid out-of-range.
        currentEncounterIndex = encounters.Count - 1;
    }
}
