using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class DialogueRunner : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Image nextIndicator;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Name Box")]
    [SerializeField] private RectTransform actualNameBoxRect;
    [SerializeField] private float nameBoxRightPadding = 28f;
    [SerializeField] private float nameBoxMaxWidth = 1200f;

    [Header("Typing")]
    [SerializeField] private float charactersPerSecond = 40f;

    [Header("Input")]
    [SerializeField] private InputActionReference advanceAction;
    
    [Header("Dialogue Events")]
    [SerializeField] private UnityEvent OnDialogueStarted;

    [SerializeField] private UnityEvent OnDialogueFinished;
        
    [Header("Typing – Punctuation Pauses")]
    [SerializeField] private bool enablePunctuationPauses = true;

    [SerializeField] private float commaPause = 0.05f;
    [SerializeField] private float sentencePause = 0.15f;

    private bool currentLineCompleted;
    private bool suppressAdvanceUntilInputReleased;

    private DialogueEncounter currentEncounter;
    private int currentLineIndex;
    private Coroutine typingRoutine;
    private Coroutine panelFadeRoutine;
    private Coroutine lineTransitionRoutine;
    private Coroutine glitchRoutine;
    private bool isTyping;
    private bool isExamining;
    private bool isTransitioningLine;
    private string displayedRawText = string.Empty;

    public bool IsRunning { get; private set; }

    // ===== Public API =====

    private void OnEnable()
    {
        if (advanceAction == null || advanceAction.action == null)
            return;

        advanceAction.action.Enable();
        advanceAction.action.performed += HandleAdvancePerformed;
    }

    private void OnDisable()
    {
        if (advanceAction == null || advanceAction.action == null)
            return;

        advanceAction.action.performed -= HandleAdvancePerformed;
        advanceAction.action.Disable();
    }

    private void SetVisible(bool visible, bool allowFade = false)
    {
        if (allowFade && currentEncounter != null && currentEncounter.fadePanel)
        {
            float duration = visible ? currentEncounter.fadeInSeconds : currentEncounter.fadeOutSeconds;
            FadeVisible(visible, duration);
            return;
        }

        SetVisibleInstant(visible);
    }

    private void SetVisibleInstant(bool visible)
    {
        if (panelFadeRoutine != null)
        {
            StopCoroutine(panelFadeRoutine);
            panelFadeRoutine = null;
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private void FadeVisible(bool visible, float duration)
    {
        if (panelFadeRoutine != null)
        {
            StopCoroutine(panelFadeRoutine);
            panelFadeRoutine = null;
        }

        if (duration <= 0f || canvasGroup == null)
        {
            SetVisibleInstant(visible);
            return;
        }

        panelFadeRoutine = StartCoroutine(FadeVisibleRoutine(visible, duration));
    }

    private IEnumerator FadeVisibleRoutine(bool visible, float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float targetAlpha = visible ? 1f : 0f;
        float elapsed = 0f;

        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
        panelFadeRoutine = null;
    }

    public void StartDialogue(DialogueEncounter encounter)
    {
        if (encounter == null || encounter.lines.Count == 0)
            return;

        StopAllCoroutines();
        typingRoutine = null;
        panelFadeRoutine = null;
        lineTransitionRoutine = null;
        glitchRoutine = null;
        isTransitioningLine = false;

        currentEncounter = encounter;
        currentLineIndex = 0;
        IsRunning = true;

        if (encounter.fadePanel && canvasGroup != null)
            canvasGroup.alpha = 0f;

        SetVisible(true, allowFade: true);
        OnDialogueStarted?.Invoke();
        
        suppressAdvanceUntilInputReleased = true;

        ShowLine(currentEncounter.lines[currentLineIndex], preservePanelVisibility: encounter.fadePanel);
    }

    public void CancelDialogue()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        if (panelFadeRoutine != null)
        {
            StopCoroutine(panelFadeRoutine);
            panelFadeRoutine = null;
        }

        if (lineTransitionRoutine != null)
        {
            StopCoroutine(lineTransitionRoutine);
            lineTransitionRoutine = null;
        }

        StopGlitchedText();

        currentEncounter = null;
        currentLineIndex = 0;
        currentLineCompleted = false;
        suppressAdvanceUntilInputReleased = false;
        isTyping = false;
        isExamining = false;
        isTransitioningLine = false;
        IsRunning = false;

        if (Game.IsReady && Game.Ctx?.ExaminePanel != null)
            Game.Ctx.ExaminePanel.Hide(invokeCallback: false);

        if (portraitImage != null)
            portraitImage.sprite = null;

        if (nameText != null)
        {
            nameText.text = string.Empty;
            ResizeNameBoxToText();
        }

        SetDialogueText(string.Empty);

        if (nextIndicator != null)
            nextIndicator.gameObject.SetActive(false);

        SetVisibleInstant(false);
    }
    
    private void EndEncounter()
    {
        IsRunning = false;
        isExamining = false;
        nextIndicator.gameObject.SetActive(false);

        currentEncounter.onEncounterEnd?.Raise();
        OnDialogueFinished?.Invoke();

        SetVisible(false, allowFade: true);

        if (currentEncounter.nextEncounter != null)
            StartDialogue(currentEncounter.nextEncounter);
    }
    
    private void Update()
    {
        if (!IsRunning)
            return;

        if (PauseUtility.IsPaused)
            return;

        if (suppressAdvanceUntilInputReleased)
        {
            if (!IsAdvancePressed())
                suppressAdvanceUntilInputReleased = false;

            return;
        }
    }

    private void HandleAdvancePerformed(InputAction.CallbackContext context)
    {
        if (!IsRunning || PauseUtility.IsPaused || suppressAdvanceUntilInputReleased)
            return;

        Advance();
    }

    private bool IsAdvancePressed()
    {
        if (HasAdvanceAction())
            return advanceAction.action.IsPressed();

        return false;
    }

    private bool HasAdvanceAction()
    {
        return advanceAction != null && advanceAction.action != null;
    }

    public void Advance()
    {
        if (!IsRunning)
            return;

        if (isExamining || isTransitioningLine)
            return;

        if (isTyping)
        {
            FinishTypingInstantly();
            return;
        }

        AdvanceToNextLine();
    }

    private void FinishTypingInstantly()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        var line = currentEncounter.lines[currentLineIndex];
        SetDialogueText(line.text);

        isTyping = false;
        nextIndicator.gameObject.SetActive(true);

        MarkLineCompleted(line);
    }

    // ===== Internal flow =====

    private void ShowLine(DialogueEncounter.Line line, bool preservePanelVisibility = false)
    {
        currentLineCompleted = false;
        isExamining = false;

        if (line.kind == DialogueEncounter.LineKind.Examine)
        {
            ShowExamineLine(line);
            return;
        }

        if (!preservePanelVisibility)
            SetVisible(true, allowFade: currentEncounter != null && currentEncounter.fadePanel);

        // UI setup
        bool hasSpeaker = line.speaker != null;
        SetSpeakerChromeVisible(hasSpeaker);

        if (hasSpeaker)
        {
            if (portraitImage != null)
                portraitImage.sprite = line.speaker.portrait;

            if (nameText != null)
                nameText.text = line.speaker.displayName;
        }
        else
        {
            if (portraitImage != null)
                portraitImage.sprite = null;

            if (nameText != null)
                nameText.text = string.Empty;
        }

        ResizeNameBoxToText();
        SetDialogueText(string.Empty);
        nextIndicator.gameObject.SetActive(false);

        // Optional line-start audio
        if (line.lineCue != null)
            Game.Ctx.Audio.PlayCueGlobal(line.lineCue);
        else if (line.speaker != null && line.speaker.lineStartCue != null)
            Game.Ctx.Audio.PlayCueGlobal(line.speaker.lineStartCue);

        if (ShouldRevealInstantly())
            ShowLineInstantly(line);
        else
            typingRoutine = StartCoroutine(TypeLine(line));
    }

    private bool ShouldRevealInstantly()
    {
        return currentEncounter != null
            && currentEncounter.textRevealMode == DialogueEncounter.TextRevealMode.Instant;
    }

    private void ShowLineInstantly(DialogueEncounter.Line line)
    {
        isTyping = false;
        SetDialogueText(line.text);
        nextIndicator.gameObject.SetActive(true);
        MarkLineCompleted(line);
    }

    private void ShowExamineLine(DialogueEncounter.Line line)
    {
        isTyping = false;
        isExamining = true;

        if (nextIndicator != null)
            nextIndicator.gameObject.SetActive(false);

        SetVisible(false);

        ExaminePanelView examinePanel = Game.IsReady ? Game.Ctx?.ExaminePanel : null;
        if (examinePanel == null)
        {
            Debug.LogWarning($"{nameof(DialogueRunner)} could not show examine line because no ExaminePanelView is bound in GameContext.", this);
            CompleteExamineLine(line);
            return;
        }

        examinePanel.Show(
            line.examineImage,
            line.examineWidth,
            () => CompleteExamineLine(line));
    }

    private void CompleteExamineLine(DialogueEncounter.Line line)
    {
        if (!IsRunning)
            return;

        isExamining = false;
        MarkLineCompleted(line);
        AdvanceToNextLine();
    }

    private IEnumerator TypeLine(DialogueEncounter.Line line)
    {
        isTyping = true;

        float baseDelay = 1f / charactersPerSecond;
        string text = line.text;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            AppendDialogueCharacter(c);

            // Per-character audio
            if (line.speaker != null && line.speaker.typingCue != null)
                Game.Ctx.Audio.PlayCueGlobal(line.speaker.typingCue);

            // Base typing delay
            float delay = baseDelay;

            // Add punctuation pause
            delay += GetPunctuationDelay(c);

            yield return new WaitForSeconds(delay);
        }

        isTyping = false;
        nextIndicator.gameObject.SetActive(true);

        MarkLineCompleted(line);
        typingRoutine = null;
    }

    private void SetDialogueText(string rawText)
    {
        displayedRawText = rawText ?? string.Empty;
        RenderDialogueText();
        RefreshGlitchedText();
    }

    private void AppendDialogueCharacter(char c)
    {
        displayedRawText += c;
        RenderDialogueText();
        RefreshGlitchedText();
    }

    private void RefreshGlitchedText()
    {
        if (glitchRoutine != null)
        {
            StopCoroutine(glitchRoutine);
            glitchRoutine = null;
        }

        if (ShouldRenderGlitchedText())
            glitchRoutine = StartCoroutine(GlitchTextRoutine());
    }

    private void StopGlitchedText()
    {
        if (glitchRoutine != null)
        {
            StopCoroutine(glitchRoutine);
            glitchRoutine = null;
        }

        displayedRawText = string.Empty;
    }

    private IEnumerator GlitchTextRoutine()
    {
        while (IsRunning && ShouldRenderGlitchedText())
        {
            RenderDialogueText();

            float refreshesPerSecond = currentEncounter != null
                ? Mathf.Max(1f, currentEncounter.glitchRefreshesPerSecond)
                : 24f;

            yield return new WaitForSecondsRealtime(1f / refreshesPerSecond);
        }

        glitchRoutine = null;
    }

    private void RenderDialogueText()
    {
        if (dialogueText == null)
            return;

        if (!ShouldRenderGlitchedText())
        {
            dialogueText.text = displayedRawText;
            return;
        }

        char marker = GetGlitchMarker();
        string pool = GetGlitchCharacterPool();
        char[] characters = displayedRawText.ToCharArray();

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == marker)
                characters[i] = pool[Random.Range(0, pool.Length)];
        }

        dialogueText.text = new string(characters);
    }

    private bool ShouldRenderGlitchedText()
    {
        if (currentEncounter == null || !currentEncounter.enableGlitchedText)
            return false;

        if (string.IsNullOrEmpty(displayedRawText))
            return false;

        string marker = currentEncounter.glitchMarker;
        if (string.IsNullOrEmpty(marker))
            return false;

        return displayedRawText.IndexOf(marker[0]) >= 0
            && !string.IsNullOrEmpty(GetGlitchCharacterPool());
    }

    private char GetGlitchMarker()
    {
        string marker = currentEncounter != null ? currentEncounter.glitchMarker : null;
        return string.IsNullOrEmpty(marker) ? '§' : marker[0];
    }

    private string GetGlitchCharacterPool()
    {
        const string fallbackPool = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!?@#$%&*+-=<>/\\[]{}";
        if (currentEncounter == null || string.IsNullOrEmpty(currentEncounter.glitchCharacterPool))
            return fallbackPool;

        return currentEncounter.glitchCharacterPool;
    }


    private float GetPunctuationDelay(char c)
    {
        if (!enablePunctuationPauses)
            return 0f;

        switch (c)
        {
            case ',':
            case ';':
            case ':':
                return commaPause;

            case '.':
            case '!':
            case '?':
                return sentencePause;

            default:
                return 0f;
        }
    }
    
    private void MarkLineCompleted(DialogueEncounter.Line line)
    {
        if (currentLineCompleted)
            return;

        currentLineCompleted = true;
        line.afterLineEvent?.Raise();
    }

    private void AdvanceToNextLine()
    {
        currentLineIndex++;

        if (currentLineIndex >= currentEncounter.lines.Count)
        {
            EndEncounter();
            return;
        }

        DialogueEncounter.Line nextLine = currentEncounter.lines[currentLineIndex];
        if (ShouldFadeBetweenLines(nextLine))
            lineTransitionRoutine = StartCoroutine(TransitionToLine(nextLine));
        else
            ShowLine(nextLine);
    }

    private bool ShouldFadeBetweenLines(DialogueEncounter.Line nextLine)
    {
        return currentEncounter != null
            && currentEncounter.fadeBetweenLines
            && canvasGroup != null
            && nextLine != null
            && nextLine.kind == DialogueEncounter.LineKind.Text;
    }

    private IEnumerator TransitionToLine(DialogueEncounter.Line line)
    {
        isTransitioningLine = true;

        if (nextIndicator != null)
            nextIndicator.gameObject.SetActive(false);

        yield return FadeCanvasAlphaTo(0f, currentEncounter.lineFadeOutSeconds);

        ShowLine(line, preservePanelVisibility: true);

        yield return FadeCanvasAlphaTo(1f, currentEncounter.lineFadeInSeconds);

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        isTransitioningLine = false;
        lineTransitionRoutine = null;
    }

    private IEnumerator FadeCanvasAlphaTo(float targetAlpha, float duration)
    {
        if (canvasGroup == null)
            yield break;

        if (panelFadeRoutine != null)
        {
            StopCoroutine(panelFadeRoutine);
            panelFadeRoutine = null;
        }

        if (duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
            yield break;
        }

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    private void ResizeNameBoxToText()
    {
        if (nameText == null)
            return;

        RectTransform boxRect = ResolveActualNameBoxRect();
        RectTransform textRect = nameText.rectTransform;
        RectTransform parentRect = boxRect != null ? boxRect.parent as RectTransform : null;
        if (boxRect == null || textRect == null || parentRect == null)
            return;

        nameText.ForceMeshUpdate();
        float textLeft = GetRectLeftInParent(textRect, parentRect);
        float textWidth = Mathf.Ceil(Mathf.Max(0f, nameText.GetPreferredValues(
            nameText.text,
            Mathf.Infinity,
            Mathf.Infinity).x));
        float textRight = textLeft + textWidth;
        float boxLeft = GetRectLeftInParent(boxRect, parentRect);
        float targetWidth = Mathf.Min(
            textRight - boxLeft + nameBoxRightPadding,
            nameBoxMaxWidth);

        boxRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(0f, targetWidth));
    }

    private RectTransform ResolveActualNameBoxRect()
    {
        if (actualNameBoxRect != null)
            return actualNameBoxRect;

        RectTransform textRect = nameText != null ? nameText.rectTransform : null;
        Transform nameBox = textRect != null ? textRect.parent : null;
        Transform actualBox = nameBox != null ? nameBox.Find("Actual Name Box") : null;
        return actualBox as RectTransform;
    }

    private void SetSpeakerChromeVisible(bool visible)
    {
        if (portraitImage != null)
            portraitImage.gameObject.SetActive(visible);

        Transform nameBox = ResolveNameBoxRoot();
        if (nameBox != null)
            nameBox.gameObject.SetActive(visible);
    }

    private Transform ResolveNameBoxRoot()
    {
        if (nameText != null && nameText.transform.parent != null)
            return nameText.transform.parent;

        RectTransform boxRect = ResolveActualNameBoxRect();
        return boxRect != null ? boxRect.parent : null;
    }

    private static float GetRectLeftInParent(RectTransform rect, RectTransform parent)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        float left = float.PositiveInfinity;
        for (int i = 0; i < corners.Length; i++)
        {
            float x = parent.InverseTransformPoint(corners[i]).x;
            if (x < left)
                left = x;
        }

        return left;
    }
}
