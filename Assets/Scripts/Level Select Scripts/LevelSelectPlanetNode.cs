using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class LevelSelectPlanetNode : MonoBehaviour, IInteractable, IHoverInfoUI
{
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform worldAnchor;

    [Header("Hover Panel")]
    [SerializeField] private GameObject screenSpacePanelPrefab;
    [SerializeField] private string worldUIRootTag = "WorldUIRoot";

    [Header("Tween")]
    [SerializeField] private float showDuration = 0.18f;
    [SerializeField] private float hideDuration = 0.12f;
    [SerializeField] private float hiddenScale = 0.88f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InCubic;

    private LevelSelectPlanetDefinition definition;
    private LevelSelectController controller;
    private GameObject panelInstance;
    private RectTransform panelTransform;
    private CanvasGroup canvasGroup;
    private TMP_Text titleText;
    private TMP_Text descriptionText;
    private Tween activeTween;
    private bool isVisible;

    public LevelSelectPlanetDefinition Definition => definition;
    public Transform WorldAnchor => worldAnchor != null ? worldAnchor : transform;

    private void Reset()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        worldAnchor = transform;
    }

    private void Awake()
    {
        if (worldAnchor == null)
            worldAnchor = transform;

        SpawnPanel();
        HideImmediate();
    }

    private void OnDestroy()
    {
        activeTween?.Kill();

        if (panelInstance != null)
            Destroy(panelInstance);
    }

    public void Bind(LevelSelectPlanetDefinition planetDefinition, LevelSelectController owner)
    {
        definition = planetDefinition;
        controller = owner;

        if (spriteRenderer != null && definition != null && definition.Sprite != null)
            spriteRenderer.sprite = definition.Sprite;

        ApplyHoverText();
    }

    public void SetHoverPanelPrefab(GameObject prefab)
    {
        screenSpacePanelPrefab = prefab;

        if (panelInstance == null)
        {
            SpawnPanel();
            ApplyHoverText();
            HideImmediate();
        }
    }

    public void Interact(GameObject interactor)
    {
        if (definition == null)
            return;

        controller?.SelectPlanet(this);
    }

    public void SetHoverState(bool isHovered, bool inRange)
    {
        if (!enabled)
            return;

        if (isHovered)
            Show();
        else
            Hide();
    }

    private void SpawnPanel()
    {
        if (screenSpacePanelPrefab == null)
            return;

        GameObject rootGO = GameObject.FindGameObjectWithTag(worldUIRootTag);
        if (rootGO == null)
        {
            Debug.LogError($"{nameof(LevelSelectPlanetNode)}: Could not find WorldUIRoot with tag '{worldUIRootTag}'.", this);
            return;
        }

        Canvas canvas = rootGO.GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            Debug.LogError($"{nameof(LevelSelectPlanetNode)}: WorldUIRoot has no Canvas.", this);
            return;
        }

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        panelInstance = Instantiate(screenSpacePanelPrefab, canvasRect);

        HoverPanelView view = panelInstance.GetComponent<HoverPanelView>();
        if (view == null || view.panelTransform == null || view.canvasGroup == null || view.titleText == null)
        {
            Debug.LogError($"{nameof(LevelSelectPlanetNode)}: Prefab missing HoverPanelView refs.", this);
            return;
        }

        panelTransform = view.panelTransform;
        canvasGroup = view.canvasGroup;
        titleText = view.titleText;
        descriptionText = view.descText;

        if (descriptionText != null)
            descriptionText.gameObject.SetActive(false);

        if (view.symbolImage != null)
            view.symbolImage.gameObject.SetActive(false);

        ScreenSpaceFollowWorld follow = panelInstance.GetComponent<ScreenSpaceFollowWorld>();
        if (follow == null)
            follow = panelInstance.AddComponent<ScreenSpaceFollowWorld>();

        follow.Init(Camera.main, canvasRect, WorldAnchor);
    }

    private void ApplyHoverText()
    {
        if (titleText != null)
            titleText.text = definition != null ? definition.DisplayName : string.Empty;
    }

    private void Show()
    {
        if (canvasGroup == null || panelTransform == null)
            return;

        if (!isVisible)
        {
            isVisible = true;
            canvasGroup.alpha = 0f;
            panelTransform.localScale = Vector3.one * hiddenScale;
        }

        activeTween?.Kill();
        activeTween = DOTween.Sequence()
            .Join(canvasGroup.DOFade(0.95f, 0.12f))
            .Join(panelTransform.DOScale(1f, showDuration).SetEase(showEase))
            .SetUpdate(true);
    }

    private void Hide()
    {
        if (!isVisible || canvasGroup == null || panelTransform == null)
            return;

        isVisible = false;
        activeTween?.Kill();
        activeTween = DOTween.Sequence()
            .Join(canvasGroup.DOFade(0f, hideDuration))
            .Join(panelTransform.DOScale(hiddenScale, hideDuration).SetEase(hideEase))
            .SetUpdate(true);
    }

    private void HideImmediate()
    {
        if (canvasGroup == null || panelTransform == null)
            return;

        panelInstance.SetActive(true);
        canvasGroup.alpha = 0f;
        panelTransform.localScale = Vector3.one * hiddenScale;
        isVisible = false;
    }
}
