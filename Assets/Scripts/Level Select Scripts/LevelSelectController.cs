using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class LevelSelectController : MonoBehaviour
{
    private static readonly List<LevelSelectController> Instances = new();

    [Header("Level Filtering")]
    [SerializeField] private bool showLockedLevels = true;

    [Header("Placed Planets")]
    [SerializeField] private LevelSelectPlanetNode[] planets;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private GameObject planetHoverPanelPrefab;

    [Header("Card Deck")]
    [SerializeField] private RectTransform cardDeckRoot;
    [SerializeField] private CanvasGroup cardDeckCanvasGroup;
    [SerializeField] private Transform cardParent;
    [SerializeField] private LevelSelectLevelCardView cardPrefab;
    [SerializeField] private Button backButton;
    [SerializeField] private Vector2 cardDeckContentPadding = new(48f, 0f);
    [SerializeField, Min(0f)] private float backButtonOrbitGap = 64f;

    [Header("Camera Focus")]
    [SerializeField] private LevelSelectCameraPan cameraPan;
    [SerializeField, Min(0f)] private float cameraFocusDuration = 0.45f;
    [SerializeField] private Ease cameraFocusEase = Ease.OutCubic;

    [Header("Card Orbit")]
    [SerializeField] private bool animateOrbit = true;
    [SerializeField, Min(0f)] private float orbitRadius = 360f;
    [SerializeField] private float orbitSpeedDegreesPerSecond = -18f;
    [SerializeField] private float orbitStartAngleDegrees = 90f;
    [SerializeField] private bool pauseOrbitOnCardHover = true;
    [SerializeField, Min(0f)] private float hoverPauseLerpSpeed = 10f;
    [SerializeField, Min(0f)] private float orbitBoundsPadding = 80f;

    [Header("Scene Loading")]
    [SerializeField] private string loadingMessage = "Loading level...";
    [SerializeField, Min(0f)] private float levelSelectFadeOutDuration = 0.16f;

    [Header("Card Tween")]
    [SerializeField, Min(0f)] private float cardFadeInDuration = 0.16f;
    [SerializeField, Min(0f)] private float cardFadeOutDuration = 0.12f;
    [SerializeField, Min(0f)] private float cardFadeStagger = 0.035f;

    private readonly List<LevelSelectLevelCardView> activeCards = new();
    private LevelSelectPlanetNode activePlanet;
    private bool isLoadingLevel;
    private bool isClosingDeck;
    private bool waitForClickRelease;
    private float orbitAngleOffsetDegrees;
    private float orbitHoverSpeedMultiplier = 1f;
    private float previousCameraY;
    private bool hasPreviousCameraY;
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
        if (worldCamera == null)
            worldCamera = Camera.main;

        if (cameraPan == null && worldCamera != null)
            cameraPan = worldCamera.GetComponent<LevelSelectCameraPan>();

        DisableDeckFollowComponent();

        if (backButton != null)
            backButton.onClick.AddListener(HideCards);

        HideCardsImmediate();
    }

    private void Start()
    {
        InitializePlacedPlanets();
    }

    private void Update()
    {
        if (PauseUtility.IsPaused)
            return;

        if (waitForClickRelease)
        {
            if (!Input.GetMouseButton(0))
                waitForClickRelease = false;

            return;
        }
    }

    private void LateUpdate()
    {
        if (IsCardDeckVisible() && activePlanet != null)
        {
            PositionCardDeck(activePlanet);
            UpdateCardOrbit(Time.unscaledDeltaTime);
        }
    }

    private void OnDestroy()
    {
        cardDeckFadeTween?.Kill();

        if (backButton != null)
            backButton.onClick.RemoveListener(HideCards);
    }

    private void InitializePlacedPlanets()
    {
        RectTransform canvasRect = GetWorldUiCanvasRect();

        for (int i = 0; i < planets.Length; i++)
            planets[i]?.Initialize(this, planetHoverPanelPrefab, canvasRect, worldCamera);
    }

    private RectTransform GetWorldUiCanvasRect()
    {
        Canvas canvas = cardDeckRoot != null ? cardDeckRoot.GetComponentInParent<Canvas>() : null;
        return canvas != null ? canvas.GetComponent<RectTransform>() : null;
    }

    public void SelectPlanet(LevelSelectPlanetNode planet)
    {
        if (PauseUtility.IsPaused)
            return;

        if (IsAnyCardDeckOpen())
            return;

        if (planet == null || planet.Definition == null)
            return;

        activePlanet = planet;
        orbitAngleOffsetDegrees = 0f;
        if (cameraPan != null)
        {
            previousCameraY = cameraPan.CurrentPosition.y;
            hasPreviousCameraY = true;
            cameraPan.FocusOn(planet.WorldAnchor.position, cameraFocusDuration, cameraFocusEase);
        }

        RebuildCards(planet.Definition);
        PositionCardDeck(planet);
        PositionOrbitCards();
        PositionBackButton();
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
        PositionOrbitCards();
    }

    private void PlayCardFadeIns()
    {
        for (int i = 0; i < activeCards.Count; i++)
            activeCards[i]?.PlayFadeIn(cardFadeInDuration, i * cardFadeStagger);
    }

    private void PlayCardFadeOuts(float duration)
    {
        for (int i = 0; i < activeCards.Count; i++)
            activeCards[i]?.PlayFadeOut(duration);
    }

    private void FitCardDeckToContent()
    {
        if (cardDeckRoot == null)
            return;

        DisableCardParentLayout();

        float maxCardWidth = 0f;
        float maxCardHeight = 0f;
        for (int i = 0; i < activeCards.Count; i++)
        {
            if (activeCards[i] != null && activeCards[i].transform is RectTransform cardRect)
            {
                maxCardWidth = Mathf.Max(maxCardWidth, cardRect.rect.width);
                maxCardHeight = Mathf.Max(maxCardHeight, cardRect.rect.height);
            }
        }

        float preferredBackWidth = 0f;
        float preferredBackHeight = 0f;
        if (backButton != null && backButton.transform is RectTransform backRect)
        {
            preferredBackWidth = backRect.rect.width;
            preferredBackHeight = backRect.rect.height;
        }

        float orbitDiameter = orbitRadius * 2f;
        float desiredWidth = Mathf.Max(
            cardDeckRoot.rect.width,
            orbitDiameter + maxCardWidth + orbitBoundsPadding + cardDeckContentPadding.x,
            preferredBackWidth + cardDeckContentPadding.x);

        float desiredHeight = Mathf.Max(
            cardDeckRoot.rect.height,
            orbitDiameter + maxCardHeight + preferredBackHeight + orbitBoundsPadding + cardDeckContentPadding.y);

        cardDeckRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, desiredWidth);
        cardDeckRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, desiredHeight);

        PositionBackButton();

        for (int i = 0; i < activeCards.Count; i++)
            activeCards[i]?.RebaseFloatyJuice();
    }

    private void DisableCardParentLayout()
    {
        if (cardParent == null)
            return;

        if (cardParent is RectTransform cardParentRect)
        {
            cardParentRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardParentRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardParentRect.pivot = new Vector2(0.5f, 0.5f);
            cardParentRect.anchoredPosition = Vector2.zero;
            cardParentRect.sizeDelta = Vector2.zero;
        }

        foreach (LayoutGroup layoutGroup in cardParent.GetComponents<LayoutGroup>())
            layoutGroup.enabled = false;

        foreach (ContentSizeFitter fitter in cardParent.GetComponents<ContentSizeFitter>())
            fitter.enabled = false;
    }

    private void LoadLevel(LevelDefinition level)
    {
        if (PauseUtility.IsPaused)
            return;

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
        PlayCardFadeOuts(Mathf.Min(cardFadeOutDuration, levelSelectFadeOutDuration));

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
                IsPointerInsideRect(card.ClickRect, screenPosition))
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
        return isLoadingLevel ||
            isClosingDeck ||
            cardDeckRoot != null &&
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
        Camera camera = worldCamera != null ? worldCamera : Camera.main;

        if (canvasRect == null || camera == null)
            return;

        Vector3 screenPoint = camera.WorldToScreenPoint(planet.WorldAnchor.position);

        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera != null ? canvas.worldCamera : camera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            eventCamera,
            out Vector2 localPoint);

        cardDeckRoot.anchoredPosition = localPoint;
    }

    private void UpdateCardOrbit(float deltaTime)
    {
        if (PauseUtility.IsPaused || isLoadingLevel)
            return;

        UpdateOrbitHoverSpeedMultiplier(deltaTime);

        if (animateOrbit)
            orbitAngleOffsetDegrees += orbitSpeedDegreesPerSecond * orbitHoverSpeedMultiplier * deltaTime;

        PositionOrbitCards();
    }

    private void UpdateOrbitHoverSpeedMultiplier(float deltaTime)
    {
        bool shouldPause = pauseOrbitOnCardHover && ShouldPauseOrbitForHover();
        float targetMultiplier = shouldPause ? 0f : 1f;
        float lerp = 1f - Mathf.Exp(-hoverPauseLerpSpeed * deltaTime);
        orbitHoverSpeedMultiplier = Mathf.Lerp(orbitHoverSpeedMultiplier, targetMultiplier, lerp);
    }

    private void PositionOrbitCards()
    {
        int count = activeCards.Count;
        if (count <= 0)
            return;

        float step = 360f / count;
        for (int i = 0; i < count; i++)
        {
            LevelSelectLevelCardView card = activeCards[i];
            if (card == null || card.transform is not RectTransform cardRect)
                continue;

            float angleDegrees = orbitStartAngleDegrees + orbitAngleOffsetDegrees + step * i;
            float angleRadians = angleDegrees * Mathf.Deg2Rad;
            Vector2 orbitPosition = new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians)) * orbitRadius;

            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = orbitPosition;

            cardRect.localRotation = Quaternion.identity;
        }
    }

    private void PositionBackButton()
    {
        if (backButton == null || backButton.transform is not RectTransform backRect)
            return;

        backRect.anchorMin = new Vector2(0.5f, 0.5f);
        backRect.anchorMax = new Vector2(0.5f, 0.5f);
        backRect.pivot = new Vector2(0.5f, 0.5f);
        backRect.anchoredPosition = new Vector2(
            0f,
            -orbitRadius - backRect.rect.height * 0.5f - backButtonOrbitGap);
        backRect.localRotation = Quaternion.identity;
    }

    private bool ShouldPauseOrbitForHover()
    {
        if (!pauseOrbitOnCardHover)
            return false;

        Vector2 screenPosition = Input.mousePosition;
        for (int i = 0; i < activeCards.Count; i++)
        {
            LevelSelectLevelCardView card = activeCards[i];
            if (card == null)
                continue;

            if (card.IsPointerHovered)
                return true;
        }

        return false;
    }

    private void DisableDeckFollowComponent()
    {
        if (cardDeckRoot != null && cardDeckRoot.TryGetComponent(out ScreenSpaceFollowWorld follow))
            follow.enabled = false;
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

        PlayCardFadeIns();
        waitForClickRelease = Input.GetMouseButton(0);
    }

    public void HideCards()
    {
        cardDeckFadeTween?.Kill();

        if (!IsCardDeckVisible() || cardFadeOutDuration <= 0f || cardDeckCanvasGroup == null)
        {
            HideCardsImmediate();
            return;
        }

        isClosingDeck = true;
        activePlanet = null;
        waitForClickRelease = false;
        LevelSelectPointerTargeter2D.ClearAllHover();
        RestoreCameraY();

        cardDeckCanvasGroup.interactable = false;
        cardDeckCanvasGroup.blocksRaycasts = true;
        PlayCardFadeOuts(cardFadeOutDuration);

        cardDeckFadeTween = cardDeckCanvasGroup
            .DOFade(0f, cardFadeOutDuration)
            .SetEase(Ease.InQuad)
            .SetUpdate(true)
            .OnComplete(HideCardsImmediate);
    }

    private void HideCardsImmediate()
    {
        cardDeckFadeTween?.Kill();

        isClosingDeck = false;
        activePlanet = null;
        waitForClickRelease = false;
        RestoreCameraY();

        if (cardDeckCanvasGroup != null)
        {
            cardDeckCanvasGroup.alpha = 0f;
            cardDeckCanvasGroup.interactable = false;
            cardDeckCanvasGroup.blocksRaycasts = false;
        }

        if (cardDeckRoot != null)
            cardDeckRoot.gameObject.SetActive(false);
    }

    private void RestoreCameraY()
    {
        if (!hasPreviousCameraY || cameraPan == null || isLoadingLevel)
            return;

        hasPreviousCameraY = false;
        cameraPan.FocusOnY(previousCameraY, cameraFocusDuration, cameraFocusEase);
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
