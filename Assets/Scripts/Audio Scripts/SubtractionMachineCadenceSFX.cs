using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AdditionMachine))]
public sealed class SubtractionMachineCadenceSfx : MonoBehaviour
{
    [Header("Wwise Routing (Global 2D)")]
    [SerializeField] private AudioRtpc notesCountRtpc;

    [Header("Cadence Cues (ii–V–I)")]
    [SerializeField] private AudioCue cueA_F9; // first placement when both empty
    [SerializeField] private AudioCue cueB_G9;  // second placement when other already filled
    [SerializeField] private AudioCue cueC_Am9;  // output pickup

    [Header("Events")] [SerializeField] private UnityEvent onSwitchToF;
    [SerializeField] private UnityEvent onSwitchToG;
    [SerializeField] private UnityEvent onSwitchToAm;

    private AdditionMachine _machine;

    private void Awake()
    {
        _machine = GetComponent<AdditionMachine>();
    }

    public void PlayPlacement(AdditionSlot placingSlot, int packSize)
    {
        if (!Game.IsReady) return;

        int notes = ClampNotes(packSize);
        SetNotesRtpc(notes);

        // Decide A vs B based on whether both inputs were empty before this placement.
        bool wasBothEmpty = _machine != null && _machine.WereBothInputsEmptyBeforePlacing(placingSlot);
        var cue = wasBothEmpty ? cueA_F9 : cueB_G9;
        if (cue != null && cue == cueA_F9)
        {
            onSwitchToF?.Invoke();
        } else if (cue != null && cue == cueB_G9)
        {
            onSwitchToG?.Invoke();
        }

        Game.Ctx.Audio.PlayCueGlobal(cue);
    }

    public void PlayOutputPickup(int sum)
    {
        if (!Game.IsReady) return;

        int notes = ClampNotes(sum);
        SetNotesRtpc(notes);

        onSwitchToAm?.Invoke();
        Game.Ctx.Audio.PlayCueGlobal(cueC_Am9);
    }

    private int ClampNotes(int raw)
    {
        // Your spec: 1..5, 5 is catch-all.
        if (raw <= 1) return 1;
        if (raw >= 5) return 5;
        return raw;
    }

    private void SetNotesRtpc(int notes)
    {
        if (notesCountRtpc == null || !notesCountRtpc.IsValid) return;
        Game.Ctx.Audio.SetGlobalRtpc(notesCountRtpc, notes);
    }
}