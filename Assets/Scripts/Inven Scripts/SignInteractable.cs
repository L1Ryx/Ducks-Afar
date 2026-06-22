using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SignInteractable : MonoBehaviour, IInteractable
{
    [Header("Dialogue")]
    [SerializeField] private List<DialogueEncounter> encounters = new List<DialogueEncounter>();
    [SerializeField] private bool loopLastEncounter = true;

    [Header("Optional Audio")]
    [SerializeField] private AudioCue onInteractCue;

    private int currentEncounterIndex;

    public void Interact(GameObject interactor)
    {
        if (!Game.IsReady || Game.Ctx == null)
            return;

        if (Game.Ctx.Dialogue == null)
        {
            Debug.LogError($"{name}: DialogueRunner not bound in GameContext.", this);
            return;
        }

        DialogueEncounter encounter = GetCurrentEncounter();
        if (encounter == null)
        {
            Debug.LogWarning($"{name}: No sign dialogue encounter assigned.", this);
            return;
        }

        if (onInteractCue != null)
            Game.Ctx.Audio.PlayCueGlobal(onInteractCue);

        Game.Ctx.Dialogue.StartDialogue(encounter);
        AdvanceEncounterIndex();
    }

    private DialogueEncounter GetCurrentEncounter()
    {
        if (encounters == null || encounters.Count == 0)
            return null;

        if (currentEncounterIndex >= encounters.Count)
            currentEncounterIndex = encounters.Count - 1;

        return encounters[currentEncounterIndex];
    }

    private void AdvanceEncounterIndex()
    {
        if (encounters == null || encounters.Count == 0)
            return;

        if (currentEncounterIndex < encounters.Count - 1)
        {
            currentEncounterIndex++;
            return;
        }

        if (loopLastEncounter)
            currentEncounterIndex = encounters.Count - 1;
    }
}
