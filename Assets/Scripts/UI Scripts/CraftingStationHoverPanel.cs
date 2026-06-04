using UnityEngine;
using DG.Tweening;

public class CraftingStationHoverPanel : MonoBehaviour, IHoverInfoUI
{
    [Header("Spawn")]
    [SerializeField] private GameObject screenSpacePanelPrefab;
    [SerializeField] private Transform worldAnchor;
    [SerializeField] private string worldUIRootTag = "WorldUIRoot";

    [Header("Copy")]
    [SerializeField] private string title = "Crafting Station";
    [TextArea] [SerializeField] private string description = "Combine items into something new.";

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
    [SerializeField] private float activeAlpha = 1f;
    [SerializeField] private float inactiveAlpha = 0.45f;

    private GameObject panelInstance;
    private CraftingHoverPanelView view;
    private Tween activeTween;
    private bool isVisible;
    private bool lastHovered;
    private bool lastInRange;

    private CraftingStation station;

    private void Awake()
    {
        if (worldAnchor == null) worldAnchor = transform;

        station = GetComponent<CraftingStation>();
        if (station == null)
        {
            Debug.LogError($"{nameof(CraftingStationHoverPanel)} requires CraftingStation on the same GameObject.", this);
            enabled = false;
            return;
        }

        SpawnPanel();
        if (panelInstance == null) return;

        view.titleText.text = title;
        view.descText.text = description;

        ApplyTradeUI();

        panelInstance.SetActive(true);
        view.canvasGroup.alpha = 0f;
        view.panelTransform.localScale = Vector3.one * hiddenScale;
        isVisible = false;

        if (view.symbolImage != null && symbolInactive != null)
            view.symbolImage.sprite = symbolInactive;
    }

    private void OnDestroy()
    {
        if (panelInstance != null)
            Destroy(panelInstance);
    }

    private void SpawnPanel()
    {
        GameObject rootGO = GameObject.FindGameObjectWithTag(worldUIRootTag);
        var canvas = rootGO != null ? rootGO.GetComponentInChildren<Canvas>() : null;
        if (canvas == null)
        {
            Debug.LogError($"{nameof(CraftingStationHoverPanel)}: WorldUIRoot missing Canvas.", this);
            enabled = false;
            return;
        }

        var canvasRect = canvas.GetComponent<RectTransform>();
        panelInstance = Instantiate(screenSpacePanelPrefab, canvasRect);

        view = panelInstance.GetComponent<CraftingHoverPanelView>();
        if (view == null || view.panelTransform == null || view.canvasGroup == null ||
            view.titleText == null || view.descText == null ||
            view.inputText == null || view.outputText == null)
        {
            Debug.LogError($"{nameof(CraftingStationHoverPanel)}: Prefab missing CraftingHoverPanelView refs.", this);
            enabled = false;
            return;
        }

        var follow = panelInstance.GetComponent<ScreenSpaceFollowWorld>();
        if (follow == null) follow = panelInstance.AddComponent<ScreenSpaceFollowWorld>();
        follow.Init(Camera.main, canvasRect, worldAnchor);
    }

    private void ApplyTradeUI()
    {
        // Input (from base class getters)
        var inputItem = station.RequiredItem;
        int inputCount = station.RequiredCount;

        if (view.inputIcon != null)
        {
            view.inputIcon.enabled = inputItem != null && inputItem.icon != null;
            view.inputIcon.sprite = inputItem != null ? inputItem.icon : null;
        }

        view.inputText.text = ItemQuantityFormatter.FormatNameWithCount(inputItem, inputCount);

        // Output (from trade)
        var trade = station.Trade;
        if (trade == null || trade.outputItem == null)
        {
            if (view.outputIcon != null) view.outputIcon.enabled = false;
            view.outputText.text = "";
            return;
        }

        if (view.outputIcon != null)
        {
            view.outputIcon.enabled = trade.outputItem.icon != null;
            view.outputIcon.sprite = trade.outputItem.icon;
        }

        view.outputText.text = ItemQuantityFormatter.FormatNameWithCount(trade.outputItem, trade.outputCount);
    }

    private bool CanAfford()
    {
        if (!Game.IsReady || Game.Ctx.Inventory == null)
            return false;

        var inputItem = station.RequiredItem;
        int inputCount = station.RequiredCount;

        if (inputItem == null || string.IsNullOrWhiteSpace(inputItem.itemId))
            return false;

        return Game.Ctx.Inventory.GetCount(inputItem.itemId) >= inputCount;
    }

    public void SetHoverState(bool isHovered, bool inRange)
    {
        if (!enabled) return;

        lastHovered = isHovered;
        lastInRange = inRange;

        if (isHovered) Show(inRange);
        else Hide();
    }

    public void RefreshAccessibility()
    {
        if (!enabled) return;
        if (!isVisible) return;          // only refresh when panel is up
        if (!lastHovered) return;        // defensive

        bool active = lastInRange && CanAfford();

        if (view.symbolImage != null)
            view.symbolImage.sprite = active ? symbolActive : symbolInactive;

        float targetAlpha = active ? activeAlpha : inactiveAlpha;

        // Do NOT replay the pop-in tween; just update alpha smoothly
        view.canvasGroup.DOKill();
        view.canvasGroup.DOFade(targetAlpha, 0.08f).SetUpdate(true);
    }


    private void Show(bool inRange)
    {
        bool canAfford = CanAfford();
        bool active = inRange && canAfford;

        if (view.symbolImage != null)
            view.symbolImage.sprite = active ? symbolActive : symbolInactive;

        float targetAlpha = active ? activeAlpha : inactiveAlpha;

        if (!isVisible)
        {
            isVisible = true;
            view.canvasGroup.alpha = 0f;
            view.panelTransform.localScale = Vector3.one * hiddenScale;
        }

        activeTween?.Kill();
        activeTween = DOTween.Sequence()
            .Join(view.canvasGroup.DOFade(targetAlpha, 0.12f))
            .Join(view.panelTransform.DOScale(1f, showDuration).SetEase(showEase))
            .SetUpdate(true);
    }

    private void Hide()
    {
        if (!isVisible) return;

        isVisible = false;
        activeTween?.Kill();
        activeTween = DOTween.Sequence()
            .Join(view.canvasGroup.DOFade(0f, hideDuration))
            .Join(view.panelTransform.DOScale(hiddenScale, hideDuration).SetEase(hideEase))
            .SetUpdate(true);
    }
}
