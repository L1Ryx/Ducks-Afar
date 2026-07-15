using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FieldTelescopeHoverPanel : MonoBehaviour, IHoverInfoUI
{
    [Header("Spawn")]
    [SerializeField] private GameObject screenSpacePanelPrefab;
    [SerializeField] private Transform worldAnchor;
    [SerializeField] private string worldUIRootTag = "WorldUIRoot";

    [Header("Copy")]
    [SerializeField] private string title = "Field Telescope";
    [TextArea] [SerializeField] private string description = "Reveal nearby points of interest.";

    [Header("Symbol")]
    [SerializeField] private Sprite symbolActive;
    [SerializeField] private Sprite symbolInactive;

    [Header("Tween")]
    [SerializeField] private float showDuration = 0.18f;
    [SerializeField] private float hideDuration = 0.12f;
    [SerializeField] private float hiddenScale = 0.88f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InCubic;

    [Header("Alpha")]
    [SerializeField] private float activeAlpha = 0.95f;
    [SerializeField] private float inactiveAlpha = 0.7f;

    private GameObject panelInstance;
    private RectTransform panelTransform;
    private CanvasGroup canvasGroup;
    private HoverPanelView view;
    private Tween activeTween;
    private bool isVisible;

    private void Awake()
    {
        if (worldAnchor == null)
            worldAnchor = transform;

        SpawnPanel();
        if (panelInstance == null)
            return;

        view.titleText.text = title;
        view.descText.text = description;

        panelInstance.SetActive(true);
        canvasGroup.alpha = 0f;
        panelTransform.localScale = Vector3.one * hiddenScale;
        isVisible = false;

        if (view.symbolImage != null && symbolInactive != null)
            view.symbolImage.sprite = symbolInactive;
    }

    private void OnDestroy()
    {
        activeTween?.Kill();

        if (panelInstance != null)
            Destroy(panelInstance);
    }

    private void SpawnPanel()
    {
        if (screenSpacePanelPrefab == null)
        {
            Debug.LogError($"{nameof(FieldTelescopeHoverPanel)}: screenSpacePanelPrefab not assigned.", this);
            enabled = false;
            return;
        }

        GameObject rootGO = GameObject.FindGameObjectWithTag(worldUIRootTag);
        if (rootGO == null)
        {
            Debug.LogError($"{nameof(FieldTelescopeHoverPanel)}: Could not find WorldUIRoot with tag '{worldUIRootTag}'.", this);
            enabled = false;
            return;
        }

        var canvas = rootGO.GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            Debug.LogError($"{nameof(FieldTelescopeHoverPanel)}: WorldUIRoot has no Canvas.", this);
            enabled = false;
            return;
        }

        var canvasRect = canvas.GetComponent<RectTransform>();
        panelInstance = Instantiate(screenSpacePanelPrefab, canvasRect);

        view = panelInstance.GetComponent<HoverPanelView>();
        if (view == null || view.panelTransform == null || view.canvasGroup == null ||
            view.titleText == null || view.descText == null)
        {
            Debug.LogError($"{nameof(FieldTelescopeHoverPanel)}: Prefab missing HoverPanelView refs.", this);
            enabled = false;
            return;
        }

        panelTransform = view.panelTransform;
        canvasGroup = view.canvasGroup;

        var follow = panelInstance.GetComponent<ScreenSpaceFollowWorld>();
        if (follow == null) follow = panelInstance.AddComponent<ScreenSpaceFollowWorld>();
        follow.Init(Camera.main, canvasRect, worldAnchor);
    }

    public void SetHoverState(bool isHovered, bool inRange)
    {
        if (!enabled) return;

        if (isHovered) Show(inRange);
        else Hide();
    }

    private void Show(bool inRange)
    {
        bool active = inRange;

        if (view.symbolImage != null)
            view.symbolImage.sprite = active ? symbolActive : symbolInactive;

        float targetAlpha = active ? activeAlpha : inactiveAlpha;
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
        if (!isVisible) return;

        isVisible = false;
        activeTween?.Kill();
        activeTween = DOTween.Sequence()
            .Join(canvasGroup.DOFade(0f, hideDuration))
            .Join(panelTransform.DOScale(hiddenScale, hideDuration).SetEase(hideEase))
            .SetUpdate(true);
    }
}
