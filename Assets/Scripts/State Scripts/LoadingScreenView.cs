using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LoadingScreenView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup root;
    [SerializeField] private TMP_Text messageText;

    [Header("Text")]
    [SerializeField] private string defaultMessage = "Loading...";

    [Header("Timing")]
    [SerializeField, Min(0f)] private float fadeInDuration = 0.2f;
    [SerializeField, Min(0f)] private float textFadeInDuration = 0.15f;
    [SerializeField, Min(0f)] private float minimumVisibleDuration = 0.6f;
    [SerializeField, Min(0f)] private float postLoadHoldDuration = 0.1f;
    [SerializeField, Min(0f)] private float textFadeOutDuration = 0.15f;
    [SerializeField, Min(0f)] private float overlayFadeOutDelay = 0.1f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.25f;

    [Header("Default View")]
    [SerializeField] private int sortingOrder = 5000;

    public float MinimumVisibleDuration => minimumVisibleDuration;
    public float PostLoadHoldDuration => postLoadHoldDuration;

    private bool visible;

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
    }

    public IEnumerator Show(string message = null)
    {
        EnsureInitialized();
        SetMessage(message);

        root.gameObject.SetActive(true);
        root.transform.SetAsLastSibling();
        SetBlocking(true);
        SetMessageAlpha(0f);

        if (!visible)
            root.alpha = 0f;

        visible = true;
        Canvas.ForceUpdateCanvases();

        yield return FadeTo(1f, fadeInDuration);
        yield return FadeMessageTo(1f, textFadeInDuration);
    }

    public IEnumerator Hide()
    {
        EnsureInitialized();

        yield return FadeMessageTo(0f, textFadeOutDuration);

        if (overlayFadeOutDelay > 0f)
            yield return WaitForUnscaledSeconds(overlayFadeOutDelay);

        yield return FadeTo(0f, fadeOutDuration);
        SetBlocking(false);
        visible = false;
    }

    public void HideImmediate()
    {
        EnsureInitialized();
        root.alpha = 0f;
        SetMessageAlpha(0f);
        SetBlocking(false);
        visible = false;
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = string.IsNullOrWhiteSpace(message) ? defaultMessage : message;
    }

    private IEnumerator FadeMessageTo(float targetAlpha, float duration)
    {
        if (messageText == null)
            yield break;

        if (duration <= 0f)
        {
            SetMessageAlpha(targetAlpha);
            yield break;
        }

        float startAlpha = messageText.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetMessageAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration));
            yield return null;
        }

        SetMessageAlpha(targetAlpha);
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
