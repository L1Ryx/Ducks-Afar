using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public sealed class LevelSelectSpaceship : MonoBehaviour, IInteractable, IHoverInfoUI
{
    [Header("Hover Panel")]
    [SerializeField] private GameObject screenSpacePanelPrefab;
    [SerializeField] private Transform worldAnchor;
    [SerializeField] private RectTransform fallbackCanvasRect;

    [Header("Copy")]
    [SerializeField] private string title = "Spaceship";
    [TextArea] [SerializeField] private string description = "Open the level map.";

    [Header("Symbol")]
    [SerializeField] private Sprite symbolActive;
    [SerializeField] private Sprite symbolInactive;

    [Header("Scene Loading")]
    [SerializeField] private string levelSelectSceneName = "Level Select";
    [SerializeField] private string loadingMessage = "Opening map...";

    [Header("Interaction Gate")]
    [SerializeField] private LevelSelectSpaceshipInteractionSettings interactionSettings;
    [SerializeField] private bool interactableWhenNoSettings = true;

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
    private InventoryCostHoverPanelView view;
    private Tween activeTween;
    private bool isVisible;
    private bool isLoading;
    private bool IsInteractionEnabled =>
        interactionSettings != null
            ? interactionSettings.SpaceshipsInteractable
            : interactableWhenNoSettings;

    private void Reset()
    {
        worldAnchor = transform;
        gameObject.layer = LayerMask.NameToLayer("Interactable");
    }

    private void Awake()
    {
        if (worldAnchor == null)
            worldAnchor = transform;
    }

    private void Start()
    {
        SpawnPanel();
        InitializePanel();
    }

    private void OnDestroy()
    {
        activeTween?.Kill();

        if (panelInstance != null)
            Destroy(panelInstance);
    }

    public void Interact(GameObject interactor)
    {
        if (isLoading || !IsInteractionEnabled)
            return;

        if (string.IsNullOrWhiteSpace(levelSelectSceneName))
        {
            Debug.LogWarning($"{nameof(LevelSelectSpaceship)}: levelSelectSceneName is empty.", this);
            return;
        }

        isLoading = true;
        Hide();

        if (Game.IsReady && Game.Ctx?.SceneLoader != null)
        {
            Game.Ctx.SceneLoader.LoadScene(levelSelectSceneName, loadingMessage);
            return;
        }

        SceneManager.LoadScene(levelSelectSceneName);
    }

    public void SetHoverState(bool isHovered, bool inRange)
    {
        if (!enabled || isLoading || !IsInteractionEnabled)
        {
            Hide();
            return;
        }

        if (isHovered)
            Show(inRange);
        else
            Hide();
    }

    private void SpawnPanel()
    {
        if (panelInstance != null)
            return;

        if (screenSpacePanelPrefab == null)
        {
            Debug.LogError($"{nameof(LevelSelectSpaceship)}: screenSpacePanelPrefab is not assigned.", this);
            enabled = false;
            return;
        }

        RectTransform canvasRect = WorldUICanvasRegistry.CurrentCanvasRect != null
            ? WorldUICanvasRegistry.CurrentCanvasRect
            : fallbackCanvasRect;

        if (canvasRect == null)
        {
            Debug.LogError($"{nameof(LevelSelectSpaceship)}: No world UI canvas is registered or assigned.", this);
            enabled = false;
            return;
        }

        panelInstance = Instantiate(screenSpacePanelPrefab, canvasRect);
        view = panelInstance.GetComponent<InventoryCostHoverPanelView>();
        if (view == null || view.panelTransform == null || view.canvasGroup == null || view.titleText == null || view.descText == null)
        {
            Debug.LogError($"{nameof(LevelSelectSpaceship)}: Hover panel prefab is missing InventoryCostHoverPanelView refs.", this);
            enabled = false;
            return;
        }

        panelTransform = view.panelTransform;
        canvasGroup = view.canvasGroup;

        ScreenSpaceFollowWorld follow = panelInstance.GetComponent<ScreenSpaceFollowWorld>();
        if (follow == null)
            follow = panelInstance.AddComponent<ScreenSpaceFollowWorld>();

        follow.Init(Camera.main, canvasRect, worldAnchor);
    }

    private void InitializePanel()
    {
        if (view == null || canvasGroup == null || panelTransform == null)
            return;

        view.titleText.text = title;
        view.descText.text = description;

        if (view.requiredIcon != null)
            view.requiredIcon.gameObject.SetActive(false);

        if (view.requiredText != null)
            view.requiredText.gameObject.SetActive(false);

        Transform itemIcon = view.transform.Find("Float Root/Item Icon");
        if (itemIcon != null)
            itemIcon.gameObject.SetActive(false);

        panelInstance.SetActive(true);
        canvasGroup.alpha = 0f;
        panelTransform.localScale = Vector3.one * hiddenScale;
        isVisible = false;

        if (view.symbolImage != null && symbolInactive != null)
            view.symbolImage.sprite = symbolInactive;
    }

    private void Show(bool inRange)
    {
        if (view == null || canvasGroup == null || panelTransform == null)
            return;

        if (view.symbolImage != null)
            view.symbolImage.sprite = inRange ? symbolActive : symbolInactive;

        float targetAlpha = inRange ? activeAlpha : inactiveAlpha;

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
        if (!isVisible || canvasGroup == null || panelTransform == null)
            return;

        isVisible = false;
        activeTween?.Kill();
        activeTween = DOTween.Sequence()
            .Join(canvasGroup.DOFade(0f, hideDuration))
            .Join(panelTransform.DOScale(hiddenScale, hideDuration).SetEase(hideEase))
            .SetUpdate(true);
    }
}
