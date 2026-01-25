using UnityEngine;

/// <summary>
/// Puzzle music player that can switch musical key by setting a Wwise State.
/// Exposes SwitchToX() functions for UnityEvent wiring.
/// </summary>
public sealed class PuzzleMusicPlayer : MusicPlayerBase
{
    [Header("Puzzle Key Switching")]
    [SerializeField] private AudioStateGroup puzzleKeyGroup;

    [SerializeField] private AudioStateValue keyAm;
    [SerializeField] private AudioStateValue keyC;
    [SerializeField] private AudioStateValue keyDm;
    [SerializeField] private AudioStateValue keyF;
    [SerializeField] private AudioStateValue keyG;

    [Tooltip("If true, applies the default key before music starts.")]
    [SerializeField] private bool applyDefaultKeyOnPlay = true;

    protected override void BeforePlay()
    {
        if (!applyDefaultKeyOnPlay)
            return;

        // Prefer explicit Am if provided; otherwise group default.
        if (keyAm != null)
            SetKey(keyAm);
        else
            ApplyDefaultKey();
    }

    private void ApplyDefaultKey()
    {
        if (!Game.IsReady)
            return;

        if (puzzleKeyGroup == null)
        {
            Debug.LogWarning($"{nameof(PuzzleMusicPlayer)}: No puzzleKeyGroup assigned.", this);
            return;
        }

        // Requires AudioStateModel to support applying default or setting group+value.
        Game.Ctx.Audio.ApplyDefaultState(puzzleKeyGroup);
    }

    private void SetKey(AudioStateValue value)
    {
        if (!Game.IsReady)
        {
            Debug.LogWarning($"{nameof(PuzzleMusicPlayer)}: GameContext not ready. Cannot set key.", this);
            return;
        }

        if (puzzleKeyGroup == null)
        {
            Debug.LogWarning($"{nameof(PuzzleMusicPlayer)}: No puzzleKeyGroup assigned.", this);
            return;
        }

        if (value == null)
        {
            Debug.LogWarning($"{nameof(PuzzleMusicPlayer)}: Key value is null.", this);
            return;
        }

        Game.Ctx.Audio.SetState(puzzleKeyGroup, value);
    }

    // UnityEvent-friendly methods:
    public void SwitchToAm() => SetKey(keyAm);
    public void SwitchToC()  => SetKey(keyC);
    public void SwitchToDm() => SetKey(keyDm);
    public void SwitchToF()  => SetKey(keyF);
    public void SwitchToG()  => SetKey(keyG);
}
