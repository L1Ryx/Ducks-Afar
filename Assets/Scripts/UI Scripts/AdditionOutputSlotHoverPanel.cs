using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

public class AdditionOutputSlotHoverPanel : MonoBehaviour, IHoverInfoUI
{
    [Header("Spawn")]
    [SerializeField] private GameObject screenSpacePanelPrefab;
    [SerializeField] private Transform worldAnchor;
    [SerializeField] private string worldUIRootTag = "WorldUIRoot";

    [Header("Copy")]
    [SerializeField] private string title = "Ancient Output";
    [TextArea] [SerializeField] private string description = "Claim the combined hardworm pack.";

    [Header("Unavailable State")]
    [SerializeField] private Sprite unavailableIcon;
    [SerializeField] private string unavailableText = "Need both inputs";

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
    
    [Header("Unavailable Copy")]
    [SerializeField] private string missingInputsText = "INCOMPLETE";
    [SerializeField] private string invalidResultText = "NONE LEFT";

    

    private GameObject panelInstance;
    private AdditionOutputHoverPanelView view;

    private AdditionOutputSlot outputSlot;
    private AdditionMachine machine;

    private Tween activeTween;
    private bool isVisible;

    private bool lastHovered;
    private bool lastInRange;

    private void Awake()
    {
        if (worldAnchor == null) worldAnchor = transform;

        outputSlot = GetComponent<AdditionOutputSlot>();
        if (outputSlot == null)
        {
            Debug.LogError($"{nameof(AdditionOutputSlotHoverPanel)} requires AdditionOutputSlot on the same GameObject.", this);
            enabled = false;
            return;
        }
        
        machine = outputSlot.Machine;
        if (machine == null)
        {
            Debug.LogError($"{nameof(AdditionOutputSlotHoverPanel)}: Missing AdditionMachine reference.", this);
            enabled = false;
            return;
        }

        SpawnPanel();
        if (panelInstance == null) return;

        view.titleText.text = title;
        view.descText.text = description;

        ApplyResultUI();

        panelInstance.SetActive(true);
        view.canvasGroup.alpha = 0f;
        view.panelTransform.localScale = Vector3.one * hiddenScale;
        isVisible = false;

        ApplyAccessibilityVisuals(false);
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
            Debug.LogError($"{nameof(AdditionOutputSlotHoverPanel)}: screenSpacePanelPrefab not assigned.", this);
            enabled = false;
            return;
        }

        GameObject rootGO = GameObject.FindGameObjectWithTag(worldUIRootTag);
        if (rootGO == null)
        {
            Debug.LogError($"{nameof(AdditionOutputSlotHoverPanel)}: Could not find WorldUIRoot with tag '{worldUIRootTag}'.", this);
            enabled = false;
            return;
        }

        var canvas = rootGO.GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            Debug.LogError($"{nameof(AdditionOutputSlotHoverPanel)}: WorldUIRoot has no Canvas.", this);
            enabled = false;
            return;
        }

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        panelInstance = Instantiate(screenSpacePanelPrefab, canvasRect);

        view = panelInstance.GetComponent<AdditionOutputHoverPanelView>();
        if (view == null || view.panelTransform == null || view.canvasGroup == null ||
            view.titleText == null || view.descText == null ||
            view.resultName == null || view.resultIcon == null)
        {
            Debug.LogError($"{nameof(AdditionOutputSlotHoverPanel)}: Prefab missing AdditionOutputHoverPanelView refs.", this);
            enabled = false;
            return;
        }

        var follow = panelInstance.GetComponent<ScreenSpaceFollowWorld>();
        if (follow == null) follow = panelInstance.AddComponent<ScreenSpaceFollowWorld>();
        follow.Init(Camera.main, canvasRect, worldAnchor);
    }

    public void SetHoverState(bool isHovered, bool inRange)
    {
        if (!enabled) return;

        lastHovered = isHovered;
        lastInRange = inRange;

        if (isHovered) Show(inRange);
        else Hide();
    }

    public void Refresh()
    {
        if (!enabled) return;

        ApplyResultUI();

        if (isVisible && lastHovered)
        {
            bool active = lastInRange && CanClaimNow();
            ApplyAccessibilityVisuals(active);
        }
    }

    private void Show(bool inRange)
    {
        ApplyResultUI();

        bool active = inRange && CanClaimNow();
        ApplyAccessibilityVisuals(active);

        if (!isVisible)
        {
            isVisible = true;
            view.canvasGroup.alpha = 0f;
            view.panelTransform.localScale = Vector3.one * hiddenScale;
        }

        activeTween?.Kill();
        activeTween = DOTween.Sequence()
            .Join(view.canvasGroup.DOFade(active ? activeAlpha : inactiveAlpha, 0.12f))
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

    private void ApplyResultUI()
    {
        if (machine == null || !Game.IsReady || Game.Ctx.ItemDb == null)
        {
            view.resultName.text = missingInputsText;
            view.resultIcon.sprite = unavailableIcon;
            view.resultIcon.enabled = unavailableIcon != null;
            return;
        }

        // Case 1: Missing inputs
        if (!machine.HasBothInputs)
        {
            view.resultName.text = missingInputsText;
            view.resultIcon.sprite = unavailableIcon;
            view.resultIcon.enabled = unavailableIcon != null;
            return;
        }

        int result = machine.Result;

        // Case 2: Subtraction result invalid (<= 0)
        if (result <= 0)
        {
            view.resultName.text = invalidResultText;
            view.resultIcon.sprite = unavailableIcon;
            view.resultIcon.enabled = unavailableIcon != null;
            return;
        }

        // Case 3: Valid result, but no matching pack definition
        var outDef = Game.Ctx.ItemDb.GetHardwormByPackSize(result);
        if (outDef == null)
        {
            view.resultName.text = $"No pack for {result}";
            view.resultIcon.sprite = unavailableIcon;
            view.resultIcon.enabled = unavailableIcon != null;
            return;
        }

        // Case 4: Valid output
        view.resultName.text = ItemQuantityFormatter.FormatNameWithCount(outDef, 1);
        view.resultIcon.sprite = outDef.icon;
        view.resultIcon.enabled = outDef.icon != null;
    }
    
    private bool CanClaimNow()
    {
        if (!Game.IsReady || Game.Ctx.Inventory == null || Game.Ctx.ItemDb == null)
            return false;

        if (machine == null || !machine.HasBothInputs)
            return false;

        int result = machine.Result;
        if (result <= 0)
            return false;

        var outDef = Game.Ctx.ItemDb.GetHardwormByPackSize(result);
        if (outDef == null)
            return false;

        return Game.Ctx.Inventory.CanAdd(outDef.itemId);
    }


    private void ApplyAccessibilityVisuals(bool isActive)
    {
        if (view.symbolImage != null)
            view.symbolImage.sprite = isActive ? symbolActive : symbolInactive;

        if (isVisible)
        {
            float targetAlpha = isActive ? activeAlpha : inactiveAlpha;
            view.canvasGroup.DOKill();
            view.canvasGroup.DOFade(targetAlpha, 0.08f).SetUpdate(true);
        }
    }
}
