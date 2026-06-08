using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class DialogueRunner : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Image nextIndicator;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Typing")]
    [SerializeField] private float charactersPerSecond = 40f;
    
    [Header("Dialogue Events")]
    [SerializeField] private UnityEvent OnDialogueStarted;

    [SerializeField] private UnityEvent OnDialogueFinished;
        
    [Header("Typing – Punctuation Pauses")]
    [SerializeField] private bool enablePunctuationPauses = true;

    [SerializeField] private float commaPause = 0.05f;
    [SerializeField] private float sentencePause = 0.15f;

    private bool currentLineCompleted;
    private bool suppressAdvanceUntilMouseUp;
    private bool suppressAdvanceUntilSpaceBarUp;


    private DialogueEncounter currentEncounter;
    private int currentLineIndex;
    private Coroutine typingRoutine;
    private bool isTyping;

    public bool IsRunning { get; private set; }

    // ===== Public API =====

    private void SetVisible(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
    public void StartDialogue(DialogueEncounter encounter)
    {
        if (encounter == null || encounter.lines.Count == 0)
            return;

        StopAllCoroutines();

        currentEncounter = encounter;
        currentLineIndex = 0;
        IsRunning = true;

        SetVisible(true);
        OnDialogueStarted?.Invoke();
        
        suppressAdvanceUntilMouseUp = true;
        suppressAdvanceUntilSpaceBarUp = true;

        ShowLine(currentEncounter.lines[currentLineIndex]);
    }

    public void CancelDialogue()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        currentEncounter = null;
        currentLineIndex = 0;
        currentLineCompleted = false;
        suppressAdvanceUntilMouseUp = false;
        suppressAdvanceUntilSpaceBarUp = false;
        isTyping = false;
        IsRunning = false;

        if (portraitImage != null)
            portraitImage.sprite = null;

        if (nameText != null)
            nameText.text = string.Empty;

        if (dialogueText != null)
            dialogueText.text = string.Empty;

        if (nextIndicator != null)
            nextIndicator.gameObject.SetActive(false);

        SetVisible(false);
    }
    
    private void EndEncounter()
    {
        IsRunning = false;
        nextIndicator.gameObject.SetActive(false);

        currentEncounter.onEncounterEnd?.Raise();
        OnDialogueFinished?.Invoke();

        SetVisible(false);

        if (currentEncounter.nextEncounter != null)
            StartDialogue(currentEncounter.nextEncounter);
    }
    
    private void Update()
    {
        if (!IsRunning)
            return;

        if (PauseUtility.IsPaused)
            return;

        // if (suppressAdvanceUntilMouseUp)
        // {
        //     if (!Input.GetMouseButton(0))
        //         suppressAdvanceUntilMouseUp = false;

        //     return;
        // }

        if (suppressAdvanceUntilMouseUp)
        {
            if (!Input.GetMouseButton(0))
                suppressAdvanceUntilMouseUp = false;
            return;
        }
        if (suppressAdvanceUntilSpaceBarUp)
        {
            if(!Input.GetKeyDown(KeyCode.Space))
                suppressAdvanceUntilSpaceBarUp = false;
            return;
        }
        
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)){ // CHANGE LATER TO NEW INPUT SYSTEM !!!
            Advance();
        }
        // if (Input.GetMouseButtonDown(0)) // CHANGE LATER TO NEW INPUT SYSTEM !!!
        //     Advance();

    }



    public void Advance()
    {
        if (!IsRunning)
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
        dialogueText.text = line.text;

        isTyping = false;
        nextIndicator.gameObject.SetActive(true);

        // Raise after-line event once
        line.afterLineEvent?.Raise();
    }

    // ===== Internal flow =====

    private void ShowLine(DialogueEncounter.Line line)
    {
        currentLineCompleted = false;
        
        // UI setup
        portraitImage.sprite = line.speaker.portrait;
        nameText.text = line.speaker.displayName;
        dialogueText.text = string.Empty;
        nextIndicator.gameObject.SetActive(false);

        // Optional line-start audio
        if (line.lineCue != null)
            Game.Ctx.Audio.PlayCueGlobal(line.lineCue);
        else if (line.speaker.lineStartCue != null)
            Game.Ctx.Audio.PlayCueGlobal(line.speaker.lineStartCue);

        typingRoutine = StartCoroutine(TypeLine(line));
    }

    private IEnumerator TypeLine(DialogueEncounter.Line line)
    {
        isTyping = true;

        float baseDelay = 1f / charactersPerSecond;
        string text = line.text;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            dialogueText.text += c;

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

        ShowLine(currentEncounter.lines[currentLineIndex]);
    }
}
