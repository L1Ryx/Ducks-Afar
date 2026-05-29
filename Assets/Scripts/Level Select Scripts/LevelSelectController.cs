using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class LevelSelectController : MonoBehaviour
{
    private static readonly List<LevelSelectController> Instances = new();

    [Header("Catalog")]
    [SerializeField] private LevelSelectCatalog catalog;
    [SerializeField] private bool showLockedLevels = true;

    [Header("Planet Layout")]
    [SerializeField] private LevelSelectPlanetNode planetPrefab;
    [SerializeField] private Transform planetParent;
    [SerializeField] private GameObject planetHoverPanelPrefab;
    [SerializeField] private Vector2 planetStartPosition = new Vector2(-6f, 0f);
    [SerializeField] private float planetSpacing = 5f;
    [SerializeField] private bool buildPlanetsOnStart = true;

    [Header("Card Deck")]
    [SerializeField] private RectTransform cardDeckRoot;
    [SerializeField] private CanvasGroup cardDeckCanvasGroup;
    [SerializeField] private Transform cardParent;
    [SerializeField] private LevelSelectLevelCardView cardPrefab;
    [SerializeField] private Button backButton;
    [SerializeField] private Vector2 cardDeckEdgePadding = new(16f, 16f);
    [SerializeField] private Vector2 cardDeckContentPadding = new(48f, 0f);

    [Header("Scene Loading")]
    [SerializeField] private string loadingMessage = "Loading level...";
    [SerializeField, Min(0f)] private float levelSelectFadeOutDuration = 0.16f;

    private readonly List<LevelSelectLevelCardView> activeCards = new();
    private LevelSelectPlanetNode activePlanet;
    private bool isLoadingLevel;
    private bool waitForClickRelease;
    private Tween cardDeckFadeTween;

    private SaveStateModel SaveState => Game.IsReady ? Game.Ctx?.SaveState : null;

    private void OnEnable()
    {
        if (!Instances.Contains(this))
            Instances.Add(this);
    }

    private void OnDisable()
    {
        Instances.Remove(this);
    }

    private void Awake()
    {
        if (planetParent == null)
            planetParent = transform;

        DisableDeckFollowComponent();

        if (backButton != null)
            backButton.onClick.AddListener(HideCards);

        HideCards();
    }

    private void Start()
    {
        if (buildPlanetsOnStart)
        {
            BuildPlanets();
        }
        else
        {
            BindExistingPlanets();
        }
    }

    private void Update()
    {
        if (waitForClickRelease)
        {
            if (!Input.GetMouseButton(0))
                waitForClickRelease = false;

            return;
        }

        if (!Input.GetMouseButtonDown(0))
            return;

        HandleCardDeckFallbackClick(Input.mousePosition);
    }

    private void LateUpdate()
    {
        if (IsCardDeckVisible() && activePlanet != null)
            PositionCardDeck(activePlanet);
    }

    private void OnDestroy()
    {
        cardDeckFadeTween?.Kill();

        if (backButton != null)
            backButton.onClick.RemoveListener(HideCards);
    }

    public void BuildPlanets()
    {
        if (catalog == null || planetPrefab == null)
            return;

        for (int i = planetParent.childCount - 1; i >= 0; i--)
        {
            Destroy(planetParent.GetChild(i).gameObject);
        }

        for (int i = 0; i < catalog.Planets.Count; i++)
        {
            LevelSelectPlanetDefinition planet = catalog.Planets[i];
            if (planet == null)
                continue;

            LevelSelectPlanetNode node = Instantiate(planetPrefab, planetParent);
            node.transform.localPosition = new Vector3(
                planetStartPosition.x + planetSpacing * i,
                planetStartPosition.y,
                0f);

            if (planetHoverPanelPrefab != null)
                node.SetHoverPanelPrefab(planetHoverPanelPrefab);

            node.Bind(planet, this);
        }
    }

    public void BindExistingPlanets()
    {
        if (catalog == null || planetParent == null)
            return;

        LevelSelectPlanetNode[] planetNodes = planetParent.GetComponentsInChildren<LevelSelectPlanetNode>(true);
        int planetCount = Mathf.Min(planetNodes.Length, catalog.Planets.Count);

        for (int i = 0; i < planetCount; i++)
        {
            LevelSelectPlanetDefinition planet = catalog.Planets[i];
            if (planet == null || planetNodes[i] == null)
                continue;

            if (planetHoverPanelPrefab != null)
                planetNodes[i].SetHoverPanelPrefab(planetHoverPanelPrefab);

            planetNodes[i].Bind(planet, this);
        }
    }

    public void SelectPlanet(LevelSelectPlanetNode planet)
    {
        if (IsAnyCardDeckOpen())
            return;

        if (planet == null || planet.Definition == null)
            return;

        activePlanet = planet;
        RebuildCards(planet.Definition);
        PositionCardDeck(planet);
        ShowCards();
    }

    private void RebuildCards(LevelSelectPlanetDefinition planet)
    {
        if (cardParent == null || cardPrefab == null)
            return;

        for (int i = cardParent.childCount - 1; i >= 0; i--)
        {
            Destroy(cardParent.GetChild(i).gameObject);
        }

        activeCards.Clear();
        SaveStateModel saveState = SaveState;

        foreach (LevelDefinition level in planet.Levels)
        {
            if (level == null)
                continue;

            bool unlocked = saveState == null || saveState.IsLevelUnlocked(level.LevelId);
            if (!showLockedLevels && !unlocked)
                continue;

            LevelSelectLevelCardView card = Instantiate(cardPrefab, cardParent);
            card.Bind(level, saveState);
            card.OnLevelSelected.AddListener(LoadLevel);
            activeCards.Add(card);
        }

        FitCardDeckToContent();
    }

    private void FitCardDeckToContent()
    {
        if (cardDeckRoot == null || cardParent is not RectTransform cardParentRect)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(cardParentRect);

        float preferredCardsWidth = LayoutUtility.GetPreferredWidth(cardParentRect);
        float preferredCardsHeight = LayoutUtility.GetPreferredHeight(cardParentRect);
        if (preferredCardsWidth <= 0f || preferredCardsHeight <= 0f)
        {
            float fallbackWidth = 0f;
            float fallbackHeight = 0f;
            for (int i = 0; i < activeCards.Count; i++)
            {
                if (activeCards[i] != null && activeCards[i].transform is RectTransform cardRect)
                {
                    fallbackWidth = Mathf.Max(fallbackWidth, cardRect.rect.width);
                    fallbackHeight += cardRect.rect.height;
                }
            }

            if (cardParent.TryGetComponent(out HorizontalOrVerticalLayoutGroup layout) && activeCards.Count > 1)
                fallbackHeight += layout.spacing * (activeCards.Count - 1);

            if (preferredCardsWidth <= 0f)
                preferredCardsWidth = fallbackWidth;

            if (preferredCardsHeight <= 0f)
                preferredCardsHeight = fallbackHeight;
        }

        float preferredBackWidth = 0f;
        float preferredBackHeight = 0f;
        if (backButton != null && backButton.transform is RectTransform backRect)
        {
            preferredBackWidth = backRect.rect.width;
            preferredBackHeight = backRect.rect.height;
        }

        float desiredWidth = Mathf.Max(
            cardDeckRoot.rect.width,
            preferredCardsWidth + cardDeckContentPadding.x,
            preferredBackWidth + cardDeckContentPadding.x);

        float desiredHeight = Mathf.Max(
            cardDeckRoot.rect.height,
            preferredCardsHeight + preferredBackHeight + cardDeckContentPadding.y);

        cardDeckRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, desiredWidth);
        cardDeckRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, desiredHeight);
        LayoutRebuilder.ForceRebuildLayoutImmediate(cardDeckRoot);
    }

    private void LoadLevel(LevelDefinition level)
    {
        if (isLoadingLevel)
            return;

        if (level == null || string.IsNullOrWhiteSpace(level.SceneName))
            return;

        isLoadingLevel = true;
        FadeOutCardsThenLoad(level);
    }

    private void FadeOutCardsThenLoad(LevelDefinition level)
    {
        LevelSelectPointerTargeter2D.ClearAllHover();

        if (cardDeckCanvasGroup == null || levelSelectFadeOutDuration <= 0f)
        {
            StartLevelLoad(level);
            return;
        }

        cardDeckCanvasGroup.interactable = false;
        cardDeckCanvasGroup.blocksRaycasts = true;

        cardDeckFadeTween?.Kill();
        cardDeckFadeTween = cardDeckCanvasGroup
            .DOFade(0f, levelSelectFadeOutDuration)
            .SetEase(Ease.InQuad)
            .SetUpdate(true)
            .OnComplete(() => StartLevelLoad(level));
    }

    private void StartLevelLoad(LevelDefinition level)
    {
        if (cardDeckRoot != null)
            cardDeckRoot.gameObject.SetActive(false);

        if (Game.IsReady && Game.Ctx?.SceneLoader != null)
        {
            Game.Ctx.SceneLoader.LoadScene(level.SceneName, loadingMessage);
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(level.SceneName);
    }

    private void HandleCardDeckFallbackClick(Vector2 screenPosition)
    {
        if (isLoadingLevel)
            return;

        if (!IsCardDeckVisible())
            return;

        if (backButton != null &&
            backButton.interactable &&
            backButton.transform is RectTransform backRect &&
            IsPointerInsideRect(backRect, screenPosition))
        {
            HideCards();
            return;
        }

        for (int i = 0; i < activeCards.Count; i++)
        {
            LevelSelectLevelCardView card = activeCards[i];
            if (card == null || !card.CanSelect || card.transform is not RectTransform cardRect)
                continue;

            if (IsPointerInsideRect(cardRect, screenPosition))
            {
                LoadLevel(card.BoundLevel);
                return;
            }
        }
    }

    public static bool IsPointerOverOpenDeck(Vector2 screenPosition)
    {
        for (int i = 0; i < Instances.Count; i++)
        {
            LevelSelectController controller = Instances[i];
            if (controller != null && controller.IsPointerOverDeck(screenPosition))
                return true;
        }

        return false;
    }

    public static bool IsAnyCardDeckOpen()
    {
        for (int i = 0; i < Instances.Count; i++)
        {
            LevelSelectController controller = Instances[i];
            if (controller != null && controller.IsCardDeckOpen())
                return true;
        }

        return false;
    }

    private bool IsPointerOverDeck(Vector2 screenPosition)
    {
        if (!IsCardDeckVisible())
            return false;

        if (cardDeckRoot != null && IsPointerInsideRect(cardDeckRoot, screenPosition))
            return true;

        for (int i = 0; i < activeCards.Count; i++)
        {
            LevelSelectLevelCardView card = activeCards[i];
            if (card != null &&
                card.gameObject.activeInHierarchy &&
                card.transform is RectTransform cardRect &&
                IsPointerInsideRect(cardRect, screenPosition))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsCardDeckVisible()
    {
        return cardDeckRoot != null &&
            cardDeckRoot.gameObject.activeInHierarchy &&
            (cardDeckCanvasGroup == null || (cardDeckCanvasGroup.alpha > 0.01f && cardDeckCanvasGroup.blocksRaycasts));
    }

    private bool IsCardDeckOpen()
    {
        return cardDeckRoot != null &&
            cardDeckRoot.gameObject.activeInHierarchy &&
            (cardDeckCanvasGroup == null || cardDeckCanvasGroup.alpha > 0.01f);
    }

    private void PositionCardDeck(LevelSelectPlanetNode planet)
    {
        if (cardDeckRoot == null || planet == null || planet.Definition == null)
            return;

        DisableDeckFollowComponent();

        Canvas canvas = cardDeckRoot.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        Camera camera = Camera.main;

        if (canvasRect == null || camera == null)
            return;

        Vector3 screenPoint = camera.WorldToScreenPoint(planet.WorldAnchor.position);
        screenPoint.y += planet.Definition.LevelCardsScreenOffset.y;

        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera != null ? canvas.worldCamera : camera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            eventCamera,
            out Vector2 localPoint);

        cardDeckRoot.anchoredPosition = ClampDeckToCanvas(localPoint, canvasRect, cardDeckRoot, cardDeckEdgePadding);
    }

    private void DisableDeckFollowComponent()
    {
        if (cardDeckRoot != null && cardDeckRoot.TryGetComponent(out ScreenSpaceFollowWorld follow))
            follow.enabled = false;
    }

    private static Vector2 ClampDeckToCanvas(Vector2 localPoint, RectTransform canvasRect, RectTransform deckRoot, Vector2 padding)
    {
        GetDeckAllowedRanges(canvasRect, deckRoot, padding, out float minX, out float maxX, out float minY, out float maxY);

        if (minX <= maxX)
            localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);

        if (minY <= maxY)
            localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);

        return localPoint;
    }

    private static void GetDeckAllowedRanges(
        RectTransform canvasRect,
        RectTransform deckRoot,
        Vector2 padding,
        out float minX,
        out float maxX,
        out float minY,
        out float maxY)
    {
        Rect canvas = canvasRect.rect;
        Rect deck = deckRoot.rect;

        minX = canvas.xMin + padding.x + deck.width * deckRoot.pivot.x;
        maxX = canvas.xMax - padding.x - deck.width * (1f - deckRoot.pivot.x);
        minY = canvas.yMin + padding.y + deck.height * deckRoot.pivot.y;
        maxY = canvas.yMax - padding.y - deck.height * (1f - deckRoot.pivot.y);
    }

    private void ShowCards()
    {
        cardDeckFadeTween?.Kill();

        if (cardDeckRoot != null)
            cardDeckRoot.gameObject.SetActive(true);

        if (cardDeckCanvasGroup != null)
        {
            cardDeckCanvasGroup.alpha = 1f;
            cardDeckCanvasGroup.interactable = true;
            cardDeckCanvasGroup.blocksRaycasts = true;
        }

        waitForClickRelease = Input.GetMouseButton(0);
    }

    public void HideCards()
    {
        cardDeckFadeTween?.Kill();

        activePlanet = null;
        waitForClickRelease = false;

        if (cardDeckCanvasGroup != null)
        {
            cardDeckCanvasGroup.alpha = 0f;
            cardDeckCanvasGroup.interactable = false;
            cardDeckCanvasGroup.blocksRaycasts = false;
        }

        if (cardDeckRoot != null)
            cardDeckRoot.gameObject.SetActive(false);
    }

    private static bool IsPointerInsideRect(RectTransform rect, Vector2 screenPosition)
    {
        if (rect == null)
            return false;

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, eventCamera))
            return true;

        if (canvas == null)
            return false;

        if (canvas.scaleFactor > 0f)
        {
            Vector2 scaledPosition = screenPosition * canvas.scaleFactor;
            if (RectTransformUtility.RectangleContainsScreenPoint(rect, scaledPosition, eventCamera))
                return true;
        }

        if (Screen.width > 0 && Screen.height > 0)
        {
            Rect pixelRect = canvas.pixelRect;
            Vector2 pixelRectPosition = new Vector2(
                screenPosition.x * pixelRect.width / Screen.width,
                screenPosition.y * pixelRect.height / Screen.height);

            if (RectTransformUtility.RectangleContainsScreenPoint(rect, pixelRectPosition, eventCamera))
                return true;
        }

        return false;
    }
}
