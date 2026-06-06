using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class FieldTelescopeVisionSystem : MonoBehaviour
{
    private const string WorldUIRootTag = "WorldUIRoot";

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.18f;
    [SerializeField] private float fadeOutDuration = 0.22f;

    [Header("Obscure")]
    [SerializeField] private Color obscuredSpriteTint = new Color(0.28f, 0.28f, 0.3f, 0.32f);
    [SerializeField] private Color obscuredTilemapTint = new Color(0.35f, 0.35f, 0.38f, 0.45f);

    [Header("Dots")]
    [SerializeField] private bool showDots = false;
    [SerializeField] private Vector2 dotSizePx = new Vector2(28f, 28f);
    [SerializeField] private Vector2 dotOffsetPx = new Vector2(0f, 30f);

    private static FieldTelescopeVisionSystem instance;

    private readonly List<SpriteRendererState> spriteStates = new();
    private readonly List<TilemapState> tilemapStates = new();
    private readonly List<GameObject> activeDots = new();

    private Coroutine activeRoutine;
    private GameEvent activePlayerMovedEvent;
    private GameObject activeAudioEmitter;

    public static void Reveal(
        Sprite dotSprite,
        float duration,
        FieldTelescopeRevealEndMode endMode = FieldTelescopeRevealEndMode.Timed,
        GameEvent playerMovedEvent = null,
        GameObject audioEmitter = null
    )
    {
        if (instance == null)
        {
            var go = new GameObject(nameof(FieldTelescopeVisionSystem));
            DontDestroyOnLoad(go);
            instance = go.AddComponent<FieldTelescopeVisionSystem>();
        }

        instance.BeginReveal(dotSprite, duration, endMode, playerMovedEvent, audioEmitter);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;

        StopActiveEffect(completeTweens: false);
    }

    private void BeginReveal(Sprite dotSprite, float duration, FieldTelescopeRevealEndMode endMode, GameEvent playerMovedEvent, GameObject audioEmitter)
    {
        StopActiveEffect(completeTweens: true);
        activeAudioEmitter = audioEmitter;

        CaptureAndObscureNonInteractables();

        if (showDots)
            SpawnInteractableDots(dotSprite);

        if (endMode == FieldTelescopeRevealEndMode.UntilPlayerMoves)
        {
            if (playerMovedEvent != null)
            {
                activePlayerMovedEvent = playerMovedEvent;
                activePlayerMovedEvent.RegisterRuntimeListener(EndActiveReveal);
                return;
            }

            Debug.LogWarning($"{nameof(FieldTelescopeVisionSystem)}: No player moved event assigned; using timed reveal.");
        }

        activeRoutine = StartCoroutine(TimedRevealRoutine(Mathf.Max(0.1f, duration)));
    }

    private IEnumerator TimedRevealRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        yield return RestoreRoutine();
        activeRoutine = null;
    }

    private void EndActiveReveal()
    {
        UnregisterPlayerMovedListener();

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(EndActiveRevealRoutine());
    }

    private IEnumerator EndActiveRevealRoutine()
    {
        yield return RestoreRoutine();
        activeRoutine = null;
    }

    private IEnumerator RestoreRoutine()
    {
        bool shouldPlayClose = HasActiveEffect();

        if (shouldPlayClose)
            PlayTelescopeClose();

        FadeOutAndRestore();
        yield return new WaitForSeconds(fadeOutDuration);

        spriteStates.Clear();
        tilemapStates.Clear();
        activeDots.Clear();

        activeAudioEmitter = null;
    }

    private void CaptureAndObscureNonInteractables()
    {
        foreach (var spriteRenderer in FindObjectsOfType<SpriteRenderer>())
        {
            if (spriteRenderer == null || !spriteRenderer.enabled)
                continue;

            if (spriteRenderer.GetComponentInParent<IInteractable>() != null)
                continue;

            Color original = spriteRenderer.color;
            spriteStates.Add(new SpriteRendererState(spriteRenderer, original));

            Color target = new Color(
                original.r * obscuredSpriteTint.r,
                original.g * obscuredSpriteTint.g,
                original.b * obscuredSpriteTint.b,
                original.a * obscuredSpriteTint.a
            );

            spriteRenderer.DOKill();
            spriteRenderer.DOColor(target, fadeInDuration).SetUpdate(true);
        }

        foreach (var tilemap in FindObjectsOfType<Tilemap>())
        {
            if (tilemap == null || !tilemap.enabled)
                continue;

            if (tilemap.GetComponentInParent<IInteractable>() != null)
                continue;

            Color original = tilemap.color;
            tilemapStates.Add(new TilemapState(tilemap, original));

            Color target = new Color(
                original.r * obscuredTilemapTint.r,
                original.g * obscuredTilemapTint.g,
                original.b * obscuredTilemapTint.b,
                original.a * obscuredTilemapTint.a
            );

            DOTween.Kill(tilemap);
            DOTween.To(() => tilemap.color, c => tilemap.color = c, target, fadeInDuration)
                .SetTarget(tilemap)
                .SetUpdate(true);
        }
    }

    private void SpawnInteractableDots(Sprite dotSprite)
    {
        if (dotSprite == null)
        {
            Debug.LogWarning($"{nameof(FieldTelescopeVisionSystem)}: No visual dot sprite assigned.");
            return;
        }

        GameObject rootGO = GameObject.FindGameObjectWithTag(WorldUIRootTag);
        var canvas = rootGO != null ? rootGO.GetComponentInChildren<Canvas>() : null;
        var canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        if (canvasRect == null)
        {
            Debug.LogWarning($"{nameof(FieldTelescopeVisionSystem)}: Could not find a WorldUIRoot canvas for telescope dots.");
            return;
        }

        var seen = new HashSet<Transform>();
        foreach (var behaviour in FindObjectsOfType<MonoBehaviour>())
        {
            if (behaviour == null || behaviour is not IInteractable)
                continue;

            Transform target = behaviour.transform;
            if (!target.gameObject.activeInHierarchy || !seen.Add(target))
                continue;

            CreateDot(canvasRect, target, dotSprite);
        }
    }

    private void CreateDot(RectTransform canvasRect, Transform target, Sprite dotSprite)
    {
        var go = new GameObject($"Telescope Dot - {target.name}", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        go.transform.SetParent(canvasRect, false);
        activeDots.Add(go);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = dotSizePx;

        var image = go.GetComponent<Image>();
        image.sprite = dotSprite;
        image.preserveAspect = true;
        image.raycastTarget = false;

        var canvasGroup = go.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        var follow = go.AddComponent<ScreenSpaceFollowWorld>();
        follow.Init(Camera.main, canvasRect, FindAnchor(target));
        follow.SetOffset(dotOffsetPx);
        follow.SetClamp(true, new Vector2(8f, 8f));

        go.AddComponent<UIFloatyJuice>();

        canvasGroup.DOFade(1f, fadeInDuration).SetUpdate(true);
    }

    private static Transform FindAnchor(Transform target)
    {
        Transform anchor = target.Find("Anchor");
        return anchor != null ? anchor : target;
    }

    private void FadeOutAndRestore()
    {
        foreach (var state in spriteStates)
        {
            if (state.Renderer == null) continue;

            state.Renderer.DOKill();
            state.Renderer.DOColor(state.OriginalColor, fadeOutDuration).SetUpdate(true);
        }

        foreach (var state in tilemapStates)
        {
            if (state.Tilemap == null) continue;

            DOTween.Kill(state.Tilemap);
            DOTween.To(() => state.Tilemap.color, c => state.Tilemap.color = c, state.OriginalColor, fadeOutDuration)
                .SetTarget(state.Tilemap)
                .SetUpdate(true);
        }

        foreach (var dot in activeDots)
        {
            if (dot == null) continue;

            var canvasGroup = dot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                Destroy(dot);
                continue;
            }

            canvasGroup.DOFade(0f, fadeOutDuration)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (dot != null)
                        Destroy(dot);
                });
        }
    }

    private void StopActiveEffect(bool completeTweens)
    {
        bool shouldPlayClose = HasActiveEffect();

        UnregisterPlayerMovedListener();

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        foreach (var state in spriteStates)
        {
            if (state.Renderer == null) continue;

            state.Renderer.DOKill(completeTweens);
            state.Renderer.color = state.OriginalColor;
        }

        foreach (var state in tilemapStates)
        {
            if (state.Tilemap == null) continue;

            DOTween.Kill(state.Tilemap, completeTweens);
            state.Tilemap.color = state.OriginalColor;
        }

        foreach (var dot in activeDots)
        {
            if (dot != null)
                Destroy(dot);
        }

        spriteStates.Clear();
        tilemapStates.Clear();
        activeDots.Clear();

        if (shouldPlayClose)
            PlayTelescopeClose();

        activeAudioEmitter = null;
    }

    private bool HasActiveEffect()
    {
        return activeRoutine != null ||
               activePlayerMovedEvent != null ||
               spriteStates.Count > 0 ||
               tilemapStates.Count > 0 ||
               activeDots.Count > 0;
    }

    private void PlayTelescopeClose()
    {
        ProjectAudio.PlayOn(ProjectAudio.Config != null ? ProjectAudio.Config.TelescopeCloseCue : null, activeAudioEmitter);
    }

    private void UnregisterPlayerMovedListener()
    {
        if (activePlayerMovedEvent == null)
            return;

        activePlayerMovedEvent.UnregisterRuntimeListener(EndActiveReveal);
        activePlayerMovedEvent = null;
    }

    private readonly struct SpriteRendererState
    {
        public readonly SpriteRenderer Renderer;
        public readonly Color OriginalColor;

        public SpriteRendererState(SpriteRenderer renderer, Color originalColor)
        {
            Renderer = renderer;
            OriginalColor = originalColor;
        }
    }

    private readonly struct TilemapState
    {
        public readonly Tilemap Tilemap;
        public readonly Color OriginalColor;

        public TilemapState(Tilemap tilemap, Color originalColor)
        {
            Tilemap = tilemap;
            OriginalColor = originalColor;
        }
    }
}
