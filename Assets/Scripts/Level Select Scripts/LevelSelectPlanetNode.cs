using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class LevelSelectPlanetNode : MonoBehaviour, IInteractable, IHoverInfoUI
{
    [Header("Definition")]
    [SerializeField] private LevelSelectPlanetDefinition definition;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform worldAnchor;

    [Header("Hover Panel")]
    [SerializeField] private GameObject screenSpacePanelPrefab;

    [Header("Tween")]
    [SerializeField] private float showDuration = 0.18f;
    [SerializeField] private float hideDuration = 0.12f;
    [SerializeField] private float hiddenScale = 0.88f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InCubic;

    private LevelSelectController controller;
    private GameObject panelInstance;
    private RectTransform panelTransform;
    private CanvasGroup canvasGroup;
    private TMP_Text titleText;
    private TMP_Text descriptionText;
    private RectTransform hoverCanvasRect;
    private Camera hoverWorldCamera;
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
    }

    private void OnDestroy()
    {
        activeTween?.Kill();

        if (panelInstance != null)
            Destroy(panelInstance);
    }

    public void Initialize(
        LevelSelectController owner,
        GameObject hoverPanelPrefab,
        RectTransform canvasRect,
        Camera worldCamera)
    {
        controller = owner;

        if (hoverPanelPrefab != null)
            screenSpacePanelPrefab = hoverPanelPrefab;

        hoverCanvasRect = canvasRect;
        hoverWorldCamera = worldCamera != null ? worldCamera : Camera.main;

        if (spriteRenderer != null && definition != null && definition.Sprite != null)
            spriteRenderer.sprite = definition.Sprite;

        SpawnPanel();
        ApplyHoverText();
        HideImmediate();
    }

    public void Interact(GameObject interactor)
    {
        if (LevelSelectController.IsAnyCardDeckOpen())
            return;

        if (definition == null)
            return;

        controller?.SelectPlanet(this);
    }

    public void SetHoverState(bool isHovered, bool inRange)
    {
        if (!enabled)
            return;

        if (LevelSelectController.IsAnyCardDeckOpen())
        {
            Hide();
            return;
        }

        if (isHovered)
            Show();
        else
            Hide();
    }

    private void SpawnPanel()
    {
        if (panelInstance != null || screenSpacePanelPrefab == null)
            return;

        if (hoverCanvasRect == null)
        {
            Debug.LogError($"{nameof(LevelSelectPlanetNode)}: Hover canvas rect not assigned by controller.", this);
            return;
        }

        panelInstance = Instantiate(screenSpacePanelPrefab, hoverCanvasRect);

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

        follow.Init(hoverWorldCamera, hoverCanvasRect, WorldAnchor);
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
