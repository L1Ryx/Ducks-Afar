using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class InventoryCostInteractableHoverPanel : MonoBehaviour, IHoverInfoUI
{
    [Header("Spawn")]
    [SerializeField] private GameObject screenSpacePanelPrefab;
    [SerializeField] private Transform worldAnchor;
    [SerializeField] private string worldUIRootTag = "WorldUIRoot";

    [Header("Interactable Copy")]
    [SerializeField] private string title = "Pet Bowl";
    [TextArea] [SerializeField] private string description = "Summon Barry the Duck";

    [Header("Symbol Sprites")]
    [SerializeField] private Sprite symbolActive;   // filled hand
    [SerializeField] private Sprite symbolInactive; // unfilled hand

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
    private InventoryCostHoverPanelView view;
    private Tween activeTween;
    private bool isVisible;
    private bool lastHovered;
    private bool lastInRange;

    private InventoryCostInteractable costInteractable;

    private void Awake()
    {
        if (worldAnchor == null) worldAnchor = transform;

        costInteractable = GetComponent<InventoryCostInteractable>();
        if (costInteractable == null)
        {
            Debug.LogError($"{nameof(InventoryCostInteractableHoverPanel)} requires an InventoryCostInteractable on the same GameObject.", this);
            enabled = false;
            return;
        }

        SpawnPanel();
        if (panelInstance == null) return;

        // Fill static copy
        view.titleText.text = title;
        view.descText.text = description;

        // Fill requirement row from backend definition
        ApplyRequirementUI();

        // Start hidden
        panelInstance.SetActive(true);
        view.canvasGroup.alpha = 0f;
        view.panelTransform.localScale = Vector3.one * hiddenScale;
        isVisible = false;

        // Default symbol state
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
        if (screenSpacePanelPrefab == null)
        {
            Debug.LogError($"{nameof(InventoryCostInteractableHoverPanel)}: screenSpacePanelPrefab not assigned.", this);
            enabled = false;
            return;
        }

        GameObject rootGO = GameObject.FindGameObjectWithTag(worldUIRootTag);
        if (rootGO == null)
        {
            Debug.LogError($"{nameof(InventoryCostInteractableHoverPanel)}: Could not find WorldUIRoot with tag '{worldUIRootTag}'.", this);
            enabled = false;
            return;
        }

        var canvas = rootGO.GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            Debug.LogError($"{nameof(InventoryCostInteractableHoverPanel)}: WorldUIRoot has no Canvas.", this);
            enabled = false;
            return;
        }

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        panelInstance = Instantiate(screenSpacePanelPrefab, canvasRect);

        view = panelInstance.GetComponent<InventoryCostHoverPanelView>();
        if (view == null || view.panelTransform == null || view.canvasGroup == null ||
            view.titleText == null || view.descText == null || view.requiredText == null)
        {
            Debug.LogError($"{nameof(InventoryCostInteractableHoverPanel)}: Prefab missing InventoryCostHoverPanelView refs.", this);
            enabled = false;
            return;
        }

        var follow = panelInstance.GetComponent<ScreenSpaceFollowWorld>();
        if (follow == null) follow = panelInstance.AddComponent<ScreenSpaceFollowWorld>();
        follow.Init(Camera.main, canvasRect, worldAnchor);
    }

    private void ApplyRequirementUI()
    {
        var requiredItem = costInteractable.RequiredItem;
        int requiredCount = costInteractable.RequiredCount;

        if (requiredItem == null)
        {
            view.requiredText.text = "";
            if (view.requiredIcon != null)
                view.requiredIcon.enabled = false;
            return;
        }

        if (view.requiredIcon != null)
        {
            view.requiredIcon.enabled = requiredItem.icon != null;
            view.requiredIcon.sprite = requiredItem.icon;
        }

        view.requiredText.text = ItemQuantityFormatter.FormatNameWithCount(requiredItem, requiredCount);
    }


    public void SetHoverState(bool isHovered, bool inRange)
    {
        if (!enabled) return;

        lastHovered = isHovered;
        lastInRange = inRange;

        // If the object is temporarily non-interactable (e.g., repaired bridge),
        // do not show the panel at all.
        if (isHovered && costInteractable != null && !costInteractable.IsInteractableNow(null))
        {
            ForceHide();
            return;
        }

        if (isHovered) Show(inRange);
        else Hide();
    }


    private bool CanAfford()
    {
        if (!Game.IsReady || Game.Ctx.Inventory == null)
            return false;

        var requiredItem = costInteractable.RequiredItem;
        int requiredCount = costInteractable.RequiredCount;

        if (requiredItem == null || string.IsNullOrWhiteSpace(requiredItem.itemId))
            return false;

        return Game.Ctx.Inventory.GetCount(requiredItem.itemId) >= requiredCount;
    }


    private void Show(bool inRange)
    {
        bool canAfford = CanAfford();
        bool canInteractNow = costInteractable.IsInteractableNow(null);

        bool isActive = inRange && canAfford && canInteractNow;

        if (view.symbolImage != null)
            view.symbolImage.sprite = isActive ? symbolActive : symbolInactive;

        float targetAlpha = isActive ? activeAlpha : inactiveAlpha;

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
    
    public void Refresh()
    {
        if (!enabled) return;

        // NEW: panel not spawned yet (Awake order), nothing to refresh.
        if (view == null)
            return;

        ApplyRequirementUI();

        if (costInteractable != null && !costInteractable.IsInteractableNow(null))
        {
            ForceHide();
            return;
        }

        if (lastHovered)
            Show(lastInRange);
    }


    public void ForceHide()
    {
        if (!enabled) return;

        lastHovered = false;
        lastInRange = false;

        // NEW: if view not ready, just reset state—Hide() would NRE.
        if (view == null)
            return;

        Hide();
    }


}
