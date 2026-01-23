using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerMovementTutorial : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("CanvasGroup on the WS Canvas -> Panel object.")]
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("Fade Defaults")]
    [Min(0f)][SerializeField] private float fadeInDuration = 0.25f;
    [Min(0f)][SerializeField] private float fadeOutDuration = 0.25f;
    [Min(0f)][SerializeField] private float fadeInDelay = 0f;
    [Min(0f)][SerializeField] private float fadeOutDelay = 0f;

    [Tooltip("If true, panel blocks raycasts while visible.")]
    [SerializeField] private bool blockRaycastsWhenVisible = true;

    [Tooltip("If true, panel is interactable while visible.")]
    [SerializeField] private bool interactableWhenVisible = true;

    [Header("Optional")]
    [Tooltip("If set, evaluates alpha over normalized time (0..1). If null, uses linear.")]
    [SerializeField] private AnimationCurve fadeCurve;

    private Coroutine fadeRoutine;

    private void Reset()
    {
        // Best-effort auto-wire if you add this component in-editor.
        if (panelCanvasGroup == null)
            panelCanvasGroup = GetComponentInChildren<CanvasGroup>(includeInactive: true);
    }

    private void Start()
    {
        if (panelCanvasGroup == null)
        {
            Debug.LogError($"{nameof(PlayerMovementTutorial)}: No CanvasGroup assigned.", this);
            enabled = false;
            return;
        }

        // Per your requirement: force default alpha to 0 in Start().
        panelCanvasGroup.alpha = 0f;
        ApplyInteractionState(isVisible: false);
    }

    /// <summary>
    /// Public API: fade panel in using serialized default parameters.
    /// </summary>
    public void FadeInPanel()
    {
        FadeTo(
            targetAlpha: 1f,
            duration: fadeInDuration,
            delay: fadeInDelay
        );
    }

    /// <summary>
    /// Public API: begin fading panel out using serialized default parameters.
    /// </summary>
    public void StartFadingOutPanel()
    {
        FadeTo(
            targetAlpha: 0f,
            duration: fadeOutDuration,
            delay: fadeOutDelay
        );
    }

    /// <summary>
    /// Public API: fade panel in with explicit parameters.
    /// </summary>
    public void FadeInPanel(float duration, float delay = 0f)
    {
        FadeTo(1f, duration, delay);
    }

    /// <summary>
    /// Public API: fade panel out with explicit parameters.
    /// </summary>
    public void StartFadingOutPanel(float duration, float delay = 0f)
    {
        FadeTo(0f, duration, delay);
    }

    private void FadeTo(float targetAlpha, float duration, float delay)
    {
        if (panelCanvasGroup == null) return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, duration, delay));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        float startAlpha = panelCanvasGroup.alpha;

        if (duration <= 0f)
        {
            panelCanvasGroup.alpha = targetAlpha;
            ApplyInteractionState(isVisible: targetAlpha > 0.0001f);
            fadeRoutine = null;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            float eased = (fadeCurve != null) ? fadeCurve.Evaluate(u) : u;
            panelCanvasGroup.alpha = Mathf.LerpUnclamped(startAlpha, targetAlpha, eased);
            yield return null;
        }

        panelCanvasGroup.alpha = targetAlpha;
        ApplyInteractionState(isVisible: targetAlpha > 0.0001f);
        fadeRoutine = null;
    }

    private void ApplyInteractionState(bool isVisible)
    {
        if (blockRaycastsWhenVisible)
            panelCanvasGroup.blocksRaycasts = isVisible;
        else
            panelCanvasGroup.blocksRaycasts = false;

        if (interactableWhenVisible)
            panelCanvasGroup.interactable = isVisible;
        else
            panelCanvasGroup.interactable = false;
    }
}
