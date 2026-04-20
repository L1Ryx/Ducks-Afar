using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class TitleCardLine
{
    public TMP_Text text;

    [Tooltip("Optional event invoked when this line finishes typing (before delay).")]
    public UnityEvent onLineCompleted;
    
    [Tooltip("If >= 0, overrides the default delay after this line finishes typing.")]
    [Min(-1f)]
    public float delayAfterLineOverride = -1f;
}


/// <summary>
/// Sequentially reveals a set of TMP_Text lines with a typing effect, then fades them out together.
/// Attach to the title card panel parent (which should have a CanvasGroup).
/// </summary>
[DisallowMultipleComponent]
public class TitleCardSequencer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("Optional explicit order. If empty, all TMP_Text children will be used in hierarchy order.")]
    [SerializeField] private List<TitleCardLine> lines = new List<TitleCardLine>();

    [Header("Timing")]
    [Tooltip("Seconds to wait after a line finishes typing before starting the next line.")]
    [Min(0f)][SerializeField] private float delayAfterLine = 0.35f;
    [Tooltip("Seconds to wait before the first line starts typing.")]
    [Min(0f)][SerializeField] private float delayBeforeFirstLine = 0f;

    [Tooltip("Characters per second.")]
    [Min(0.1f)][SerializeField] private float typeSpeedCps = 35f;

    [Tooltip("Seconds to wait after the final line finishes typing before fading out.")]
    [Min(0f)][SerializeField] private float delayAfterLastLine = 0.75f;

    [Header("Fade Out")]
    [Tooltip("Fade-out speed in alpha per second. (e.g., 2 = fade out in ~0.5s)")]
    [Min(0.01f)][SerializeField] private float fadeOutRate = 2f;
    [Tooltip("If true, fades the text/panel out at the end of the sequence.")]
    [SerializeField] private bool fadeOutAtEnd = true;

    [Header("Options")]
    [Tooltip("Use unscaled time (ignores Time.timeScale), useful for title cards.")]
    [SerializeField] private bool useUnscaledTime = true;

    [Tooltip("If true, disables the panel GameObject at the end.")]
    [SerializeField] private bool disableOnFinish = true;

    [SerializeField] private bool canOnlyBeStartedOnce = true;
    private bool hasStarted = false;
    
    [Header("Typing Randomness")]
    [Tooltip("0 = perfectly uniform typing. 0.25 = +/-25% jitter per character.")]
    [Range(0f, 1f)]
    [SerializeField] private float perCharTimeJitter = 0.15f;

    [Tooltip("Minimum multiplier applied to base secondsPerChar after jitter.")]
    [Min(0.01f)]
    [SerializeField] private float minCharTimeMultiplier = 0.4f;

    [Tooltip("Maximum multiplier applied to base secondsPerChar after jitter.")]
    [Min(0.01f)]
    [SerializeField] private float maxCharTimeMultiplier = 2.0f;

    [Header("Optional Human Pauses")]
    [Tooltip("If enabled, adds small extra delays after spaces/punctuation.")]
    [SerializeField] private bool enableHumanPauses = false;

    [Tooltip("Extra delay (seconds) after a space character.")]
    [Min(0f)]
    [SerializeField] private float extraDelayAfterSpace = 0.015f;

    [Tooltip("Extra delay (seconds) after punctuation like .,!?:;")]
    [Min(0f)]
    [SerializeField] private float extraDelayAfterPunctuation = 0.05f;
    
    [Header("Events")] [SerializeField] UnityEvent onTitleCardFadeOutStarted;

    [Header("Audio")] [SerializeField] private AudioCue ac;

    private readonly Dictionary<TMP_Text, string> _fullTexts = new();
    private Coroutine _sequenceCo;

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        CacheLinesAndTexts();
        HardResetVisuals();
    }

    /// <summary>
    /// Starts the title card reveal + fade sequence.
    /// Safe to call multiple times; it will restart the sequence.
    /// </summary>
    public void StartSequence()
    {
        if (canOnlyBeStartedOnce && hasStarted)
        {
            return;
        }

        hasStarted = true;
        CacheLinesAndTexts();

        if (_sequenceCo != null)
            StopCoroutine(_sequenceCo);

        _sequenceCo = StartCoroutine(SequenceRoutine());
    }

    /// <summary>
    /// Immediately stops any running sequence and hides the title card.
    /// </summary>
    public void StopAndHide()
    {
        if (_sequenceCo != null)
        {
            StopCoroutine(_sequenceCo);
            _sequenceCo = null;
        }

        HardResetVisuals();

        if (disableOnFinish)
            gameObject.SetActive(false);
    }

    private void CacheLinesAndTexts()
    {
        if (lines == null)
            lines = new List<TitleCardLine>();

        if (lines.Count == 0)
        {
            var found = GetComponentsInChildren<TMP_Text>(includeInactive: true);
            foreach (var t in found)
            {
                lines.Add(new TitleCardLine { text = t, delayAfterLineOverride = -1f });
            }
        }

        _fullTexts.Clear();
        foreach (var line in lines)
        {
            if (line.text == null) continue;
            _fullTexts[line.text] = line.text.text ?? string.Empty;
        }
    }

    private void HardResetVisuals()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        // Ensure texts are ready for maxVisibleCharacters typing.
        foreach (var kvp in _fullTexts)
        {
            var t = kvp.Key;
            if (t == null) continue;

            t.text = kvp.Value;
            t.ForceMeshUpdate();
            t.maxVisibleCharacters = 0;
        }
    }

    private IEnumerator SequenceRoutine()
    {
        // Show panel
        if (disableOnFinish && !gameObject.activeSelf)
            gameObject.SetActive(true);

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
        
        // delay before first line
        if (delayBeforeFirstLine > 0f)
            yield return StartCoroutine(WaitRoutine(delayBeforeFirstLine));

        // Type each line sequentially
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var t = line.text;
            if (t == null) continue;

            t.text = _fullTexts.TryGetValue(t, out var full) ? full : (t.text ?? string.Empty);
            t.ForceMeshUpdate();
            t.maxVisibleCharacters = 0;

            int totalChars = t.textInfo.characterCount;
            if (totalChars == 0 && t.text.Length > 0)
            {
                yield return null;
                t.ForceMeshUpdate();
                totalChars = t.textInfo.characterCount;
            }

            yield return TypeTextRoutine(t, totalChars);
            
            line.onLineCompleted?.Invoke();

            bool isLastLine = (i == lines.Count - 1);

            float delay =
                isLastLine
                    ? delayAfterLastLine
                    : (line.delayAfterLineOverride >= 0f
                        ? line.delayAfterLineOverride
                        : delayAfterLine);

            yield return StartCoroutine(WaitRoutine(delay));

        }

        if (fadeOutAtEnd)
            yield return FadeOutRoutine();

        if (disableOnFinish)
            gameObject.SetActive(false);

        _sequenceCo = null;
    }

    private IEnumerator TypeTextRoutine(TMP_Text t, int totalChars)
    {
        if (totalChars <= 0)
        {
            t.maxVisibleCharacters = 0;
            yield break;
        }

        float baseSecondsPerChar = 1f / Mathf.Max(0.1f, typeSpeedCps);
        int visible = 0;

        // Make sure textInfo is current and stable.
        t.ForceMeshUpdate();

        while (visible < totalChars)
        {
            visible++;
            t.maxVisibleCharacters = visible;

            // Character that just became visible (by TMP character index)
            char c = '\0';
            if (t.textInfo != null && t.textInfo.characterCount >= visible)
            {
                var charInfo = t.textInfo.characterInfo[visible - 1];
                c = charInfo.character;
            }

            // Play typing audio
            Game.Ctx.Audio.PlayCueGlobal(ac);

            float delay = GetHumanCharDelay(baseSecondsPerChar, c);
            yield return StartCoroutine(WaitRoutine(delay));
        }
    }

    private float GetHumanCharDelay(float baseSecondsPerChar, char c)
    {
        // Jitter: multiplier in [1-jitter, 1+jitter], then clamped to min/max multipliers.
        float jitter = Mathf.Clamp01(perCharTimeJitter);
        float multiplier = 1f;

        if (jitter > 0f)
        {
            float raw = Random.Range(1f - jitter, 1f + jitter);
            multiplier = Mathf.Clamp(raw, minCharTimeMultiplier, maxCharTimeMultiplier);
        }

        float delay = baseSecondsPerChar * multiplier;

        if (enableHumanPauses)
        {
            if (c == ' ')
                delay += extraDelayAfterSpace;
            else if (IsPunctuation(c))
                delay += extraDelayAfterPunctuation;
        }

        return Mathf.Max(0f, delay);
    }

    private static bool IsPunctuation(char c)
    {
        // keeping simple and predictable, I might change this later
        return c == '.' || c == ',' || c == '!' || c == '?' || c == ':' || c == ';';
    }


    private IEnumerator FadeOutRoutine()
    {
        if (canvasGroup == null)
            yield break;

        onTitleCardFadeOutStarted?.Invoke();
        float alpha = canvasGroup.alpha;
        while (alpha > 0f)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            alpha -= fadeOutRate * dt;
            canvasGroup.alpha = Mathf.Max(0f, alpha);
            yield return null;
        }
    }

    private IEnumerator WaitRoutine(float seconds)
    {
        if (seconds <= 0f)
            yield break;

        if (useUnscaledTime)
            yield return new WaitForSecondsRealtime(seconds);
        else
            yield return new WaitForSeconds(seconds);
    }
}
