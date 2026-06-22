using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SimpleInteractableHoverPanel : MonoBehaviour, IHoverInfoUI
{
    [Header("Spawn")]
    [SerializeField] private GameObject screenSpacePanelPrefab;
    [SerializeField] private Transform worldAnchor;
    [SerializeField] private string worldUIRootTag = "WorldUIRoot";

    [Header("Content")]
    [SerializeField] private string title = "Sign";

    [Header("Tween")]
    [SerializeField] private float showDuration = 0.18f;
    [SerializeField] private float hideDuration = 0.12f;
    [SerializeField] private float hiddenScale = 0.88f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InCubic;

    [Header("Range Visuals")]
    [SerializeField] private float inRangeAlpha = 0.95f;
    [SerializeField] private float outOfRangeAlpha = 0.75f;

    [Header("Symbol")]
    [SerializeField] private Sprite symbolInRange;
    [SerializeField] private Sprite symbolOutOfRange;

    private GameObject panelInstance;
    private RectTransform panelTransform;
    private CanvasGroup canvasGroup;
    private Image symbolImage;
    private Tween activeTween;
    private bool isVisible;

    private void Awake()
    {
        if (worldAnchor == null)
            worldAnchor = transform;

        SpawnPanel();
        if (panelInstance == null)
            return;

        ApplyContent();
        panelInstance.SetActive(true);
        canvasGroup.alpha = 0f;
        panelTransform.localScale = Vector3.one * hiddenScale;
        isVisible = false;
    }

    private void OnDestroy()
    {
        activeTween?.Kill();

        if (panelInstance != null)
            Destroy(panelInstance);
    }

    public void SetHoverState(bool isHovered, bool inRange)
    {
        if (!enabled)
            return;

        if (isHovered)
            Show(inRange);
        else
            Hide();
    }

    private void SpawnPanel()
    {
        if (screenSpacePanelPrefab == null)
        {
            Debug.LogError($"{nameof(SimpleInteractableHoverPanel)}: screenSpacePanelPrefab not assigned.", this);
            enabled = false;
            return;
        }

        GameObject rootGO = GameObject.FindGameObjectWithTag(worldUIRootTag);
        if (rootGO == null)
        {
            Debug.LogError($"{nameof(SimpleInteractableHoverPanel)}: could not find WorldUIRoot with tag '{worldUIRootTag}'.", this);
            enabled = false;
            return;
        }

        Canvas canvas = rootGO.GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            Debug.LogError($"{nameof(SimpleInteractableHoverPanel)}: WorldUIRoot has no Canvas.", this);
            enabled = false;
            return;
        }

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        panelInstance = Instantiate(screenSpacePanelPrefab, canvasRect);

        HoverPanelView view = panelInstance.GetComponent<HoverPanelView>();
        if (view == null)
        {
            Debug.LogError($"{nameof(SimpleInteractableHoverPanel)}: HoverPanelView missing on panel prefab.", this);
            enabled = false;
            return;
        }

        panelTransform = view.panelTransform;
        canvasGroup = view.canvasGroup;
        symbolImage = view.symbolImage;

        ScreenSpaceFollowWorld follow = panelInstance.GetComponent<ScreenSpaceFollowWorld>();
        if (follow == null)
            follow = panelInstance.AddComponent<ScreenSpaceFollowWorld>();

        follow.Init(Camera.main, canvasRect, worldAnchor);
    }

    private void ApplyContent()
    {
        HoverPanelView view = panelInstance.GetComponent<HoverPanelView>();
        TMP_Text titleText = view.titleText;
        TMP_Text descText = view.descText;

        if (titleText != null)
            titleText.text = title;

        if (descText != null)
            descText.text = string.Empty;

        if (symbolImage != null)
            symbolImage.sprite = symbolOutOfRange;
    }

    private void Show(bool inRange)
    {
        if (panelTransform == null || canvasGroup == null)
            return;

        if (symbolImage != null)
            symbolImage.sprite = inRange ? symbolInRange : symbolOutOfRange;

        float targetAlpha = inRange ? inRangeAlpha : outOfRangeAlpha;

        if (!isVisible)
        {
            isVisible = true;
            canvasGroup.alpha = 0f;
            panelTransform.localScale = Vector3.one * hiddenScale;
        }

        activeTween?.Kill();
        activeTween = DOTween.Sequence()
            .Join(canvasGroup.DOFade(targetAlpha, 0.12f))
            .Join(panelTransform.DOScale(1f, showDuration).SetEase(showEase))
            .SetUpdate(true);
    }

    private void Hide()
    {
        if (!isVisible || panelTransform == null || canvasGroup == null)
            return;

        isVisible = false;
        activeTween?.Kill();
        activeTween = DOTween.Sequence()
            .Join(canvasGroup.DOFade(0f, hideDuration))
            .Join(panelTransform.DOScale(hiddenScale, hideDuration).SetEase(hideEase))
            .SetUpdate(true);
    }
}
