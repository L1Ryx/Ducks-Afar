using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class LevelSelectLevelCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ICanvasRaycastFilter
{
    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("State")]
    [SerializeField] private LevelSelectTwoStateSymbol completedSymbol;
    [SerializeField] private Transform artifactSymbolParent;
    [SerializeField] private LevelSelectTwoStateSymbol artifactSymbolPrefab;
    [SerializeField] private CanvasGroup lockedCanvasGroup;

    [Header("Input")]
    [SerializeField] private Button button;
    [SerializeField] private Vector2 hitAreaInset = new(24f, 10f);

    [Header("Tween")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private UIFloatyJuice floatyJuice;

    [Header("Hover Scale")]
    [SerializeField] private bool scaleOnHover = true;
    [SerializeField, Min(1f)] private float hoverScaleMultiplier = 1.08f;
    [SerializeField, Min(0f)] private float hoverScaleLerpSpeed = 18f;

    private LevelDefinition level;
    private RectTransform rectTransform;
    private Vector3 baseScale = Vector3.one;
    private float visibleAlpha = 1f;
    private Tween fadeTween;
    private bool pointerHovered;

    public UnityEvent<LevelDefinition> OnLevelSelected { get; } = new();
    public LevelDefinition BoundLevel => level;
    public bool CanSelect => level != null && (button == null || button.interactable);
    public bool IsPointerHovered => pointerHovered;
    public RectTransform ClickRect
    {
        get
        {
            if (button != null && button.transform is RectTransform buttonRect)
                return buttonRect;

            return rectTransform != null ? rectTransform : transform as RectTransform;
        }
    }

    private void Reset()
    {
        button = GetComponent<Button>();
        fadeCanvasGroup = GetComponent<CanvasGroup>();
        floatyJuice = GetComponent<UIFloatyJuice>();
        rectTransform = GetComponent<RectTransform>();
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
            baseScale = rectTransform.localScale;

        if (fadeCanvasGroup == null)
            fadeCanvasGroup = GetComponent<CanvasGroup>();

        if (floatyJuice == null)
            floatyJuice = GetComponent<UIFloatyJuice>();

        if (button != null)
            button.onClick.AddListener(HandleClicked);
    }

    private void Update()
    {
        UpdateHoverScale();
    }

    private void OnDestroy()
    {
        fadeTween?.Kill();

        if (button != null)
            button.onClick.RemoveListener(HandleClicked);
    }

    private void OnDisable()
    {
        pointerHovered = false;

        if (rectTransform != null)
            rectTransform.localScale = baseScale;
    }

    public void Bind(LevelDefinition levelDefinition, SaveStateModel saveState)
    {
        level = levelDefinition;

        bool hasLevel = level != null;
        string levelId = hasLevel ? level.LevelId : string.Empty;
        bool unlocked = hasLevel && (saveState == null || saveState.IsLevelUnlocked(levelId));
        bool completed = hasLevel && saveState != null && saveState.IsLevelCompleted(levelId);

        if (titleText != null)
            titleText.text = hasLevel ? level.DisplayName : "Unknown Level";

        if (descriptionText != null)
            descriptionText.text = hasLevel ? level.Description : string.Empty;

        if (completedSymbol != null)
            completedSymbol.SetState(completed);

        if (button != null)
            button.interactable = unlocked;

        visibleAlpha = unlocked ? 1f : 0.55f;

        if (lockedCanvasGroup != null && lockedCanvasGroup != fadeCanvasGroup)
            lockedCanvasGroup.alpha = visibleAlpha;
        else if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = visibleAlpha;

        RebuildArtifactSymbols(level, saveState);
    }

    public void PlayFadeIn(float duration, float delay)
    {
        if (fadeCanvasGroup == null)
            return;

        fadeTween?.Kill();
        fadeCanvasGroup.alpha = 0f;
        fadeTween = fadeCanvasGroup
            .DOFade(visibleAlpha, duration)
            .SetDelay(delay)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    public Tween PlayFadeOut(float duration)
    {
        if (fadeCanvasGroup == null)
            return null;

        fadeTween?.Kill();
        fadeTween = fadeCanvasGroup
            .DOFade(0f, duration)
            .SetEase(Ease.InQuad)
            .SetUpdate(true);

        return fadeTween;
    }

    public void RebaseFloatyJuice()
    {
        if (rectTransform != null)
            baseScale = rectTransform.localScale;

        if (floatyJuice != null)
            floatyJuice.Rebase();
    }

    private void UpdateHoverScale()
    {
        if (!scaleOnHover || rectTransform == null)
            return;

        Vector3 targetScale = baseScale * (pointerHovered ? hoverScaleMultiplier : 1f);
        float lerp = 1f - Mathf.Exp(-hoverScaleLerpSpeed * Time.unscaledDeltaTime);
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, lerp);
    }

    private void RebuildArtifactSymbols(LevelDefinition levelDefinition, SaveStateModel saveState)
    {
        if (artifactSymbolParent == null)
            return;

        for (int i = artifactSymbolParent.childCount - 1; i >= 0; i--)
        {
            Destroy(artifactSymbolParent.GetChild(i).gameObject);
        }

        if (levelDefinition == null || artifactSymbolPrefab == null)
            return;

        foreach (LevelArtifactDefinition artifact in levelDefinition.Artifacts)
        {
            if (artifact == null)
                continue;

            LevelSelectTwoStateSymbol symbol = Instantiate(artifactSymbolPrefab, artifactSymbolParent);
            symbol.transform.SetAsFirstSibling();

            bool discovered = saveState != null && saveState.IsArtifactDiscovered(artifact.ArtifactId);
            symbol.SetState(discovered, artifact.Icon);
        }
    }

    private void HandleClicked()
    {
        if (level == null)
            return;

        OnLevelSelected.Invoke(level);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerHovered = false;
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        RectTransform hitRect = ClickRect;
        if (hitRect == null)
            return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(hitRect, screenPoint, eventCamera, out Vector2 localPoint))
            return false;

        Rect rect = hitRect.rect;
        Vector2 safeInset = new Vector2(
            Mathf.Min(Mathf.Max(0f, hitAreaInset.x), rect.width * 0.45f),
            Mathf.Min(Mathf.Max(0f, hitAreaInset.y), rect.height * 0.45f));

        rect.xMin += safeInset.x;
        rect.xMax -= safeInset.x;
        rect.yMin += safeInset.y;
        rect.yMax -= safeInset.y;

        return rect.Contains(localPoint);
    }
}
