using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[DisallowMultipleComponent]
public class NewItemPopupUI : MonoBehaviour
{
    [Header("Refs (auto-filled by Reset if names match)")]
    [SerializeField] private TMP_Text itemDescText;
    [SerializeField] private Image itemIconImage;
    [SerializeField] private CanvasGroup canvasGroup;
    [Header("Pop Animation")]
    [SerializeField] private float popScale = 1.08f;
    [SerializeField] private float popUpDuration = 0.06f;
    [SerializeField] private float popDownDuration = 0.08f;
    [Header("Floaty")] [SerializeField] private RectTransform floatyTarget;

    private Vector3 baseScale;
    private Sequence lifetimeSequence;
    private Tween popTween;


    public RectTransform RectTransform { get; private set; }

    private Coroutine lifetimeRoutine;

    private void Awake()
    {
        RectTransform = transform as RectTransform;
        baseScale = transform.localScale;

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
    }

    private void Reset()
    {
        RectTransform = transform as RectTransform;

        var desc = transform.Find("Item Description Frame/Item Desc");
        if (desc != null) itemDescText = desc.GetComponent<TMP_Text>();

        var icon = transform.Find("Item Icon Frame/Item Icon");
        if (icon != null) itemIconImage = icon.GetComponent<Image>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        var floaty = transform.Find("Floaty Content");
        if (floaty != null) floatyTarget = floaty as RectTransform;

    }

    public void Bind(ItemDefinition def, string overrideTitle = null)
    {
        if (itemDescText != null)
            itemDescText.text = !string.IsNullOrWhiteSpace(overrideTitle) ? overrideTitle : def.displayName;

        if (itemIconImage != null)
            itemIconImage.sprite = def.icon;
    }
    
    public void RebaseFloatyIfPresent()
    {
        if (floatyTarget == null) return;

        var floaty = floatyTarget.GetComponent<UIFloatyJuice>();
        if (floaty != null)
            floaty.Rebase();
    }

    public void Play(
        float fadeInSeconds,
        float holdSeconds,
        float fadeOutSeconds,
        Action onFinished = null
    )
    {
        Play(fadeInSeconds, holdSeconds, fadeOutSeconds, Vector2.zero, 0f, onFinished);
    }

    public void Play(
        float fadeInSeconds,
        float holdSeconds,
        float fadeOutSeconds,
        Vector2 exitDirection,
        float exitDistance,
        Action onFinished = null
    )
    {
        if (canvasGroup == null)
            return;

        // Kill any existing animation on this popup instance
        lifetimeSequence?.Kill();
        popTween?.Kill();

        // Reset base state (important if reused / instantiated fast)
        transform.localScale = baseScale;
        canvasGroup.alpha = 0f;
        Vector2 startAnchoredPosition = RectTransform != null ? RectTransform.anchoredPosition : Vector2.zero;
        Vector2 exitAnchoredPosition = startAnchoredPosition + GetExitOffset(exitDirection, exitDistance);

        // POP: quick up then back
        // Use SetUpdate(true) for unscaled time (same as your coroutine implementation)
        popTween = transform
            .DOScale(baseScale * popScale, popUpDuration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                transform
                    .DOScale(baseScale, popDownDuration)
                    .SetEase(Ease.InCubic)
                    .SetUpdate(true);
            });

        // LIFETIME: fade in -> hold -> fade out -> callback
        lifetimeSequence = DOTween.Sequence()
            .SetUpdate(true)
            .Join(transform.DOScale(baseScale * popScale, popUpDuration).SetEase(Ease.OutCubic))
            .Append(transform.DOScale(baseScale, popDownDuration).SetEase(Ease.InCubic))
            .Join(canvasGroup.DOFade(1f, fadeInSeconds).SetEase(Ease.OutQuad))
            .AppendInterval(holdSeconds)
            .Append(canvasGroup.DOFade(0f, fadeOutSeconds).SetEase(Ease.InQuad));

        if (RectTransform != null)
            lifetimeSequence.Join(RectTransform.DOAnchorPos(exitAnchoredPosition, fadeOutSeconds).SetEase(Ease.InCubic));

        lifetimeSequence.OnComplete(() =>
        {
            lifetimeSequence = null;
            onFinished?.Invoke();
        });
    }

    public void PlayExit(
        float fadeOutSeconds,
        Vector2 exitDirection,
        float exitDistance,
        Action onFinished = null
    )
    {
        if (canvasGroup == null)
        {
            onFinished?.Invoke();
            return;
        }

        lifetimeSequence?.Kill();
        popTween?.Kill();

        Vector2 startAnchoredPosition = RectTransform != null ? RectTransform.anchoredPosition : Vector2.zero;
        Vector2 exitAnchoredPosition = startAnchoredPosition + GetExitOffset(exitDirection, exitDistance);

        lifetimeSequence = DOTween.Sequence()
            .SetUpdate(true)
            .Join(canvasGroup.DOFade(0f, fadeOutSeconds).SetEase(Ease.InQuad));

        if (RectTransform != null)
            lifetimeSequence.Join(RectTransform.DOAnchorPos(exitAnchoredPosition, fadeOutSeconds).SetEase(Ease.InCubic));

        lifetimeSequence.OnComplete(() =>
        {
            lifetimeSequence = null;
            onFinished?.Invoke();
        });
    }

    private static Vector2 GetExitOffset(Vector2 direction, float distance)
    {
        if (direction.sqrMagnitude <= 0.0001f || distance <= 0f)
            return Vector2.zero;

        return direction.normalized * distance;
    }

    
    private IEnumerator PopRoutine()
    {
        // Scale up
        float t = 0f;
        Vector3 targetScale = baseScale * popScale;

        while (t < popUpDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / popUpDuration);
            transform.localScale = Vector3.Lerp(baseScale, targetScale, EaseOut(a));
            yield return null;
        }

        // Scale back down
        t = 0f;
        while (t < popDownDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / popDownDuration);
            transform.localScale = Vector3.Lerp(targetScale, baseScale, EaseIn(a));
            yield return null;
        }

        transform.localScale = baseScale;
    }
    
    private static float EaseOut(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private static float EaseIn(float t)
    {
        return t * t * t;
    }


    public void ForceHideAndStop()
    {
        lifetimeSequence?.Kill();
        lifetimeSequence = null;

        popTween?.Kill();
        popTween = null;

        if (canvasGroup != null) canvasGroup.alpha = 0f;
        transform.localScale = baseScale;
    }
    private void OnDestroy()
    {
        lifetimeSequence?.Kill();
        popTween?.Kill();
    }



    private IEnumerator LifetimeRoutine(float fadeIn, float hold, float fadeOut, Action onFinished)
    {
        // Start invisible
        canvasGroup.alpha = 0f;

        // Fade in
        if (fadeIn > 0f)
        {
            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(t / fadeIn);
                yield return null;
            }
        }
        canvasGroup.alpha = 1f;

        // Hold
        if (hold > 0f)
            yield return new WaitForSecondsRealtime(hold);

        // Fade out
        if (fadeOut > 0f)
        {
            float t = 0f;
            while (t < fadeOut)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(t / fadeOut);
                yield return null;
            }
        }
        canvasGroup.alpha = 0f;

        lifetimeRoutine = null;
        onFinished?.Invoke();
    }
}
