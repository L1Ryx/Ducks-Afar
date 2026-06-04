using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class CanvasGroupFade : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Timing")]
    [Min(0f)][SerializeField] private float fadeInDuration = 0.25f;
    [Min(0f)][SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private Ease fadeInEase = Ease.OutQuad;
    [SerializeField] private Ease fadeOutEase = Ease.InQuad;
    [SerializeField] private bool useUnscaledTime = true;

    private Tween activeTween;

    public CanvasGroup CanvasGroup
    {
        get
        {
            EnsureCanvasGroup();
            return canvasGroup;
        }
    }

    public bool IsVisible { get; private set; }
    public bool IsTransitioning => activeTween != null && activeTween.IsActive() && activeTween.IsPlaying();

    private void Reset()
    {
        EnsureCanvasGroup();
    }

    private void Awake()
    {
        EnsureCanvasGroup();
        IsVisible = canvasGroup != null && canvasGroup.alpha > 0.001f;
        ApplyInteractionState(IsVisible);
    }

    private void OnDisable()
    {
        KillActiveTween(false);
    }

    public Tween FadeIn()
    {
        return FadeTo(true, fadeInDuration, fadeInEase);
    }

    public Tween FadeIn(float duration)
    {
        return FadeTo(true, duration, fadeInEase);
    }

    public Tween FadeOut()
    {
        return FadeTo(false, fadeOutDuration, fadeOutEase);
    }

    public Tween FadeOut(float duration)
    {
        return FadeTo(false, duration, fadeOutEase);
    }

    public Tween FadeTo(bool visible)
    {
        return visible ? FadeIn() : FadeOut();
    }

    public Tween FadeTo(bool visible, float duration, Ease ease)
    {
        EnsureCanvasGroup();

        if (canvasGroup == null)
            return null;

        KillActiveTween(false);
        ApplyInteractionState(false);

        float targetAlpha = visible ? 1f : 0f;

        if (duration <= 0f || Mathf.Approximately(canvasGroup.alpha, targetAlpha))
        {
            SetVisibleInstant(visible);
            return null;
        }

        activeTween = canvasGroup
            .DOFade(targetAlpha, duration)
            .SetEase(ease)
            .SetUpdate(useUnscaledTime)
            .SetTarget(this)
            .OnComplete(() =>
            {
                ApplyVisibleState(visible);
                activeTween = null;
            });

        return activeTween;
    }

    public void ShowInstant()
    {
        SetVisibleInstant(true);
    }

    public void HideInstant()
    {
        SetVisibleInstant(false);
    }

    public void SetVisibleInstant(bool visible)
    {
        EnsureCanvasGroup();

        if (canvasGroup == null)
            return;

        KillActiveTween(false);
        ApplyVisibleState(visible);
    }

    public void KillActiveTween(bool complete)
    {
        if (activeTween == null)
            return;

        if (activeTween.IsActive())
            activeTween.Kill(complete);

        activeTween = null;
    }

    private void EnsureCanvasGroup()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    private void ApplyInteractionState(bool interactive)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.interactable = interactive;
        canvasGroup.blocksRaycasts = interactive;
    }

    private void ApplyVisibleState(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
        IsVisible = visible;
        ApplyInteractionState(visible);
    }
}
