using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Dialogue Encounter")]
public sealed class DialogueEncounter : ScriptableObject
{
    public enum TextRevealMode
    {
        Typewriter,
        Instant
    }

    public enum LineKind
    {
        Text,
        Examine
    }

    [Serializable]
    public sealed class Line
    {
        [Header("Line Kind")]
        public LineKind kind = LineKind.Text;

        [Header("Who speaks")]
        public DialogueCharacter speaker;

        [Header("Text")]
        [TextArea] public string text;

        [Header("Examine")]
        public Sprite examineImage;
        [Min(1f)] public float examineWidth = 1000f;

        [Header("Audio (optional)")]
        [Tooltip("Optional cue to play for this line. If null, the speaker's defaultLineCue may be used.")]
        public AudioCue lineCue;

        [Header("Events (optional)")]
        [Tooltip("Raised immediately after this line finishes displaying (or immediately if you skip typing).")]
        public GameEvent afterLineEvent;
    }

    [Header("Lines (played in order)")]
    public List<Line> lines = new List<Line>();

    [Header("Presentation")]
    public TextRevealMode textRevealMode = TextRevealMode.Typewriter;
    public bool fadePanel;
    [Min(0f)] public float fadeInSeconds = 0.12f;
    [Min(0f)] public float fadeOutSeconds = 0.12f;
    public bool fadeBetweenLines;
    [Min(0f)] public float lineFadeOutSeconds = 0.08f;
    [Min(0f)] public float lineFadeInSeconds = 0.08f;

    [Header("Glitched Text")]
    public bool enableGlitchedText = true;
    [Tooltip("Typing this marker in dialogue text makes that character slot rapidly scramble at runtime.")]
    public string glitchMarker = "§";
    public string glitchCharacterPool = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!?@#$%&*+-=<>/\\[]{}";
    [Min(1f)] public float glitchRefreshesPerSecond = 24f;
    public bool lockGlitchedCharacterWidth = true;
    [Tooltip("TMP <mspace> width used for each scrambled character slot. Try values like 0.55em, 0.65em, or 16.")]
    public string glitchedCharacterWidth = "0.65em";
    [Tooltip("When width locking is on, this avoids skinny punctuation that makes the visible text appear to grow/shrink.")]
    public bool preferVisuallyWideGlitchCharacters = true;
    public string visuallyWideGlitchCharacterPool = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789MW#@%&$";

    [Header("Encounter flow")]
    [Tooltip("Optional: next encounter to start automatically after this one ends.")]
    public DialogueEncounter nextEncounter;

    [Header("Encounter events")]
    [Tooltip("Raised once when the encounter finishes (after the last line).")]
    public GameEvent onEncounterEnd;
}
