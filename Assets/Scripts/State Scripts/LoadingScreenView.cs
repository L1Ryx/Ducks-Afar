using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LoadingScreenView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup root;
    [SerializeField] private TMP_Text messageText;

    [Header("Text")]
    [SerializeField] private string defaultMessage = "Loading";

    [Header("Timing")]
    [SerializeField, Min(0f)] private float fadeInDuration = 0.2f;
    [SerializeField, Min(0f)] private float textFadeInDuration = 0.15f;
    [SerializeField, Min(0f)] private float minimumVisibleDuration = 0.6f;
    [SerializeField, Min(0f)] private float postLoadHoldDuration = 0.1f;
    [SerializeField, Min(0f)] private float textFadeOutDuration = 0.15f;
    [SerializeField, Min(0f)] private float overlayFadeOutDelay = 0.1f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.25f;

    [SerializeField] Transform TofuAnim;
    [SerializeField] Transform BubblesAnim;
    [SerializeField] Transform AsterAnim;
    [SerializeField] Transform CodaAnim;

    [Header("Default View")]
    [SerializeField] private int sortingOrder = 5000;

    public float MinimumVisibleDuration => minimumVisibleDuration;
    public float PostLoadHoldDuration => postLoadHoldDuration;

    private bool visible;
    private Tween duckLoadingTween;
    private CanvasGroup tofuDuckGroup;
    private CanvasGroup bubblesDuckGroup;
    private CanvasGroup asterDuckGroup;
    private CanvasGroup codaDuckGroup;

    private void Awake()
    {
        EnsureInitialized();
        HideImmediate();
    }

    public void EnsureInitialized()
    {
        if (root == null)
            root = GetComponent<CanvasGroup>();

        if (root == null)
            BuildDefaultView();

        if (messageText == null)
            messageText = root.GetComponentInChildren<TMP_Text>(true);

        ResolveDuckCanvasGroups();
    }

    public IEnumerator Show(string message = null)
    {
        EnsureInitialized();
        SetMessage(message);
        SetDucksVisible(true);
        
        root.gameObject.SetActive(true);
        root.transform.SetAsLastSibling();
        SetBlocking(true);
        SetMessageAlpha(0f);
        SetDucksAlpha(0f);

        if (!visible)
            root.alpha = 0f;

        visible = true;
        Canvas.ForceUpdateCanvases();

        StartDuckLoadingAnimation();

        yield return FadeTo(1f, fadeInDuration);
        yield return FadeLoadingContentTo(1f, textFadeInDuration);
    }

    public IEnumerator ShowBlack()
    {
        EnsureInitialized();
        StopDuckLoadingAnimation();
        SetDucksVisible(false);

        root.gameObject.SetActive(true);
        root.transform.SetAsLastSibling();
        SetBlocking(true);
        SetMessageAlpha(0f);
        SetDucksAlpha(0f);

        if (!visible)
            root.alpha = 0f;

        visible = true;
        Canvas.ForceUpdateCanvases();

        yield return FadeTo(1f, fadeInDuration);
    }

    public IEnumerator Hide()
    {
        EnsureInitialized();

        yield return FadeLoadingContentTo(0f, textFadeOutDuration);

        if (overlayFadeOutDelay > 0f)
            yield return WaitForUnscaledSeconds(overlayFadeOutDelay);

        yield return FadeTo(0f, fadeOutDuration);
        StopDuckLoadingAnimation();
        SetDucksVisible(false);
        SetBlocking(false);
        visible = false;
    }

    public IEnumerator HideBlack()
    {
        EnsureInitialized();
        StopDuckLoadingAnimation();
        SetDucksVisible(false);
        SetMessageAlpha(0f);
        SetDucksAlpha(0f);

        yield return FadeTo(0f, fadeOutDuration);
        SetBlocking(false);
        visible = false;
    }

    public void HideImmediate()
    {
        EnsureInitialized();
        StopDuckLoadingAnimation();
        SetDucksVisible(false);
        root.alpha = 0f;
        SetMessageAlpha(0f);
        SetDucksAlpha(0f);
        SetBlocking(false);
        visible = false;
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = string.IsNullOrWhiteSpace(message) ? defaultMessage : message;
    }

    private IEnumerator FadeLoadingContentTo(float targetAlpha, float duration)
    {
        if (messageText == null && !HasAnyDuckGroup())
            yield break;

        if (duration <= 0f)
        {
            SetMessageAlpha(targetAlpha);
            SetDucksAlpha(targetAlpha);
            yield break;
        }

        float startMessageAlpha = messageText != null ? messageText.color.a : targetAlpha;
        float startDuckAlpha = GetDuckAlpha();
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = elapsed / duration;
            SetMessageAlpha(Mathf.Lerp(startMessageAlpha, targetAlpha, normalized));
            SetDucksAlpha(Mathf.Lerp(startDuckAlpha, targetAlpha, normalized));
            yield return null;
        }

        SetMessageAlpha(targetAlpha);
        SetDucksAlpha(targetAlpha);
    }

    private void SetMessageAlpha(float alpha)
    {
        if (messageText == null)
            return;

        Color color = messageText.color;
        color.a = alpha;
        messageText.color = color;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (root == null)
            yield break;

        if (duration <= 0f)
        {
            root.alpha = targetAlpha;
            yield break;
        }

        float startAlpha = root.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            root.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        root.alpha = targetAlpha;
    }

    private void SetBlocking(bool blocking)
    {
        if (root == null)
            return;

        root.interactable = blocking;
        root.blocksRaycasts = blocking;
    }

    private static IEnumerator WaitForUnscaledSeconds(float seconds)
    {
        float endTime = Time.unscaledTime + seconds;

        while (Time.unscaledTime < endTime)
            yield return null;
    }

    private void StartDuckLoadingAnimation()
    {
        StopDuckLoadingAnimation();

        if (TofuAnim == null || BubblesAnim == null || AsterAnim == null || CodaAnim == null)
            return;

        Sequence ducksLoading = DOTween.Sequence();

        // TofuAnim.DOMoveY(106, 0.1f);
        // BubblesAnim.DOMoveY(106, 0.1f);
        // AsterAnim.DOMoveY(106, 0.1f);
        // CodaAnim.DOMoveY(106, 0.1f);

        ducksLoading.Append(TofuAnim.DOLocalMoveY(TofuAnim.localPosition.y - 8, 0.5f));
        ducksLoading.Join(BubblesAnim.DOLocalMoveY(BubblesAnim.localPosition.y + 8, 0.5f));
        ducksLoading.Append(BubblesAnim.DOLocalMoveY(BubblesAnim.localPosition.y - 8, 0.5f));
        ducksLoading.Join(AsterAnim.DOLocalMoveY(AsterAnim.localPosition.y + 8, 0.5f));
        ducksLoading.Append(AsterAnim.DOLocalMoveY(AsterAnim.localPosition.y - 8, 0.5f));
        ducksLoading.Join(CodaAnim.DOLocalMoveY(CodaAnim.localPosition.y + 8, 0.5f));
        ducksLoading.Append(CodaAnim.DOLocalMoveY(CodaAnim.localPosition.y - 8, 0.5f));
        ducksLoading.Join(TofuAnim.DOLocalMoveY(TofuAnim.localPosition.y + 8, 0.5f));
        ducksLoading.SetLoops(-1, LoopType.Restart);

        duckLoadingTween = ducksLoading;
    }

    private void StopDuckLoadingAnimation()
    {
        TofuAnim.DOMoveY(35, 0.1f, true);
        BubblesAnim.DOMoveY(35, 0.1f, true);
        AsterAnim.DOMoveY(35, 0.1f, true);
        CodaAnim.DOMoveY(35, 0.1f, true);
        duckLoadingTween?.Kill();
        duckLoadingTween = null;
    }

    private void SetDucksVisible(bool visibleDucks)
    {
        SetTransformVisible(TofuAnim, visibleDucks);
        SetTransformVisible(BubblesAnim, visibleDucks);
        SetTransformVisible(AsterAnim, visibleDucks);
        SetTransformVisible(CodaAnim, visibleDucks);
    }

    private void SetDucksAlpha(float alpha)
    {
        SetCanvasGroupAlpha(tofuDuckGroup, alpha);
        SetCanvasGroupAlpha(bubblesDuckGroup, alpha);
        SetCanvasGroupAlpha(asterDuckGroup, alpha);
        SetCanvasGroupAlpha(codaDuckGroup, alpha);
    }

    private float GetDuckAlpha()
    {
        if (tofuDuckGroup != null)
            return tofuDuckGroup.alpha;
        if (bubblesDuckGroup != null)
            return bubblesDuckGroup.alpha;
        if (asterDuckGroup != null)
            return asterDuckGroup.alpha;
        if (codaDuckGroup != null)
            return codaDuckGroup.alpha;

        return 0f;
    }

    private bool HasAnyDuckGroup()
    {
        return tofuDuckGroup != null ||
               bubblesDuckGroup != null ||
               asterDuckGroup != null ||
               codaDuckGroup != null;
    }

    private void ResolveDuckCanvasGroups()
    {
        tofuDuckGroup = GetOrAddCanvasGroup(TofuAnim);
        bubblesDuckGroup = GetOrAddCanvasGroup(BubblesAnim);
        asterDuckGroup = GetOrAddCanvasGroup(AsterAnim);
        codaDuckGroup = GetOrAddCanvasGroup(CodaAnim);
    }

    private static CanvasGroup GetOrAddCanvasGroup(Transform target)
    {
        if (target == null)
            return null;

        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null)
            group = target.gameObject.AddComponent<CanvasGroup>();

        group.interactable = false;
        group.blocksRaycasts = false;
        return group;
    }

    private static void SetTransformVisible(Transform target, bool visibleTransform)
    {
        if (target != null)
            target.gameObject.SetActive(visibleTransform);
    }

    private static void SetCanvasGroupAlpha(CanvasGroup group, float alpha)
    {
        if (group != null)
            group.alpha = alpha;
    }

    private void BuildDefaultView()
    {
        GameObject canvasObject = new GameObject("Loading Screen Canvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        root = canvasObject.AddComponent<CanvasGroup>();

        GameObject backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(canvasObject.transform, false);

        RectTransform backgroundRect = backgroundObject.AddComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        Image background = backgroundObject.AddComponent<Image>();
        background.color = Color.black;
        background.raycastTarget = true;

        GameObject textObject = new GameObject("Loading Text");
        textObject.transform.SetParent(canvasObject.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        messageText = textObject.AddComponent<TextMeshProUGUI>();
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.color = Color.white;
        messageText.fontSize = 48f;
        messageText.raycastTarget = false;
        SetMessage(defaultMessage);
    }
}
