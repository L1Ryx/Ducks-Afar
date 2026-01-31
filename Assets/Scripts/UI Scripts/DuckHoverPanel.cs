using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DuckHoverPanel : MonoBehaviour, IHoverInfoUI
{
    [Header("Spawn")]
    [Tooltip("UI prefab (RectTransform root) to spawn under the WorldUIRoot screen-space canvas.")]
    [SerializeField] private GameObject screenSpacePanelPrefab;

    [Tooltip("World transform the UI should hover above (usually an empty child above the sprite).")]
    [SerializeField] private Transform worldAnchor;

    [Tooltip("Tag on the screen-space Canvas root used for world-attached UI.")]
    [SerializeField] private string worldUIRootTag = "WorldUIRoot";

    [Header("Content")]
    [SerializeField] private string title = "Duck";
    [TextArea] [SerializeField] private string description = "What is going on?";

    [Header("Tween")]
    [SerializeField] private float showDuration = 0.18f;
    [SerializeField] private float hideDuration = 0.12f;
    [SerializeField] private float hiddenScale = 0.88f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InCubic;

    [Header("Range Visuals")]
    [SerializeField] private float inRangeAlpha = .95f;
    [SerializeField] private float outOfRangeAlpha = .75f;
    
    [Header("Symbol")]
    [SerializeField] private Sprite symbolInRange;
    [SerializeField] private Sprite symbolOutOfRange;
    private Image symbolImage;


    // Spawned UI refs
    private GameObject panelInstance;
    private RectTransform panelTransform;
    private CanvasGroup canvasGroup;
    private TMP_Text titleText;
    private TMP_Text descText;

    private Tween activeTween;
    private bool isVisible;
    private DuckPickup pickup;

    private void Awake()
    {
        if (worldAnchor == null)
            worldAnchor = transform;

        pickup = GetComponent<DuckPickup>();

        SpawnPanel();
        if (panelInstance == null) return; // safety

        ApplyContent();

        // Start hidden
        panelInstance.SetActive(true);
        canvasGroup.alpha = 0f;
        panelTransform.localScale = Vector3.one * hiddenScale;
        isVisible = false;
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
        Debug.LogError($"{nameof(DuckHoverPanel)}: screenSpacePanelPrefab not assigned.", this);
        enabled = false;
        return;
    }

    GameObject rootGO = GameObject.FindGameObjectWithTag(worldUIRootTag);
    if (rootGO == null)
    {
        Debug.LogError($"{nameof(DuckHoverPanel)}: Could not find WorldUIRoot with tag '{worldUIRootTag}'.", this);
        enabled = false;
        return;
    }

    var canvas = rootGO.GetComponentInChildren<Canvas>();
    if (canvas == null)
    {
        Debug.LogError($"{nameof(DuckHoverPanel)}: WorldUIRoot has no Canvas.", this);
        enabled = false;
        return;
    }

    RectTransform canvasRect = canvas.GetComponent<RectTransform>();

    // 1) Instantiate FIRST
    panelInstance = Instantiate(screenSpacePanelPrefab, canvasRect);

    // 2) Cache core UI refs
    panelTransform = panelInstance.GetComponent<RectTransform>();
    canvasGroup = panelInstance.GetComponentInChildren<CanvasGroup>(true);
    var texts = panelInstance.GetComponentsInChildren<TMP_Text>(true);

    var view = panelInstance.GetComponent<HoverPanelView>();
    if (view == null)
    {
        Debug.LogError($"{nameof(DuckHoverPanel)}: HoverPanelView missing on panel prefab.", this);
        enabled = false;
        return;
    }

    panelTransform = view.panelTransform;
    canvasGroup = view.canvasGroup;
    titleText = view.titleText;
    descText = view.descText;
    symbolImage = view.symbolImage;


    // 3) NOW find the Symbol image on the spawned instance
    var symbolTf = panelInstance.transform.Find("Float Root/Symbol");
    if (symbolTf != null)
    {
        symbolImage = symbolTf.GetComponent<Image>();
        if (symbolImage != null)
            symbolImage.sprite = symbolOutOfRange;
    }
    else
    {
        // Not fatal; you can still use the panel without the symbol
        Debug.LogWarning($"{nameof(DuckHoverPanel)}: Could not find Symbol at path Float Root/Symbol on spawned panel.", this);
    }

    // 4) Init follower
    var follow = panelInstance.GetComponent<ScreenSpaceFollowWorld>();
    if (follow == null)
        follow = panelInstance.AddComponent<ScreenSpaceFollowWorld>();

    follow.Init(Camera.main, canvasRect, worldAnchor);
}


    private void ApplyContent()
    {
        if (pickup == null || pickup.ItemDef == null)
        {
            titleText.text = "Unknown Item";
            descText.text = "";
            return;
        }

        var def = pickup.ItemDef;

        titleText.text = def.displayName;
        
        descText.text = def.description ?? "";
    }


    public void SetHoverState(bool isHovered, bool inRange)
    {
        if (!enabled) return;

        if (isHovered) Show(inRange);
        else Hide();
    }

    private void Show(bool inRange)
    {
        if (symbolImage != null)
            symbolImage.sprite = inRange ? symbolInRange : symbolOutOfRange;

        float targetAlpha = inRange ? inRangeAlpha : outOfRangeAlpha;

        if (!isVisible)
        {
            isVisible = true;
            // reset baseline on first show
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
