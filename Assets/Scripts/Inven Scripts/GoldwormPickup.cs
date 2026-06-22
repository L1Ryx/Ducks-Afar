using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public sealed class GoldwormPickup : MonoBehaviour, IInteractable
{
    [Header("Currency")]
    [SerializeField, Min(1)] private int goldwormsGranted = 1;

    [Header("Pickup Popup")]
    [SerializeField] private bool showPickupPopup = true;
    [SerializeField] private string popupTitle = "Goldworm";
    [SerializeField] private Sprite popupIcon;
    [Tooltip("One-hardworm popup icons are 16px sprites displayed in an 80px UI slot, so 5 keeps goldworms at the same pixel scale.")]
    [SerializeField, Min(0.01f)] private float popupPixelsPerSpritePixel = 5f;
    [SerializeField] private NewItemPopupManager popupManager;

    [Header("Persistence")]
    [Tooltip("Optional explicit override. Leave empty for an automatic scene/path/position-based pickup id.")]
    [SerializeField] private string flagIdOverride;
    [SerializeField] private PersistentId persistentId;
    [SerializeField] private bool saveImmediately = true;

    [Header("Events")]
    [SerializeField] private GameEvent onPickedUp;
    [SerializeField] private UnityEvent onSuccess;

    public int GoldwormsGranted => Mathf.Max(1, goldwormsGranted);
    public UnityEvent OnSuccessEvent => onSuccess;

    private void Awake()
    {
        if (persistentId == null)
            persistentId = GetComponent<PersistentId>();

        if (popupIcon == null)
            popupIcon = GetComponentInChildren<SpriteRenderer>(true)?.sprite;
    }

    private IEnumerator Start()
    {
        for (int i = 0; i < 60 && !Game.IsReady; i++)
            yield return null;

        if (IsAlreadyCollected())
            Destroy(gameObject);
    }

    public void Interact(GameObject interactor)
    {
        if (!Game.IsReady || Game.Ctx?.SaveState == null)
        {
            Debug.LogWarning($"{name}: cannot pick up goldworm because GameContext is not ready.", this);
            return;
        }

        if (IsAlreadyCollected())
        {
            Destroy(gameObject);
            return;
        }

        string flagId = ResolveFlagId();
        Game.Ctx.SaveState.MarkGoldwormsCollected();
        Game.Ctx.SaveState.AddGoldworms(GoldwormsGranted);
        Game.Ctx.SaveState.SetWorldState(flagId);

        ShowPickupPopup();

        onPickedUp?.Raise();
        onSuccess?.Invoke();

        if (saveImmediately && Game.Ctx.Saves != null)
            Game.Ctx.Saves.SaveToActiveSlot(captureSceneCheckpoint: false);

        Destroy(gameObject);
    }

    private void ShowPickupPopup()
    {
        if (!showPickupPopup)
            return;

        if (popupManager == null)
            popupManager = NewItemPopupManager.Instance;

        if (popupManager == null)
            popupManager = FindFirstObjectByType<NewItemPopupManager>();

        if (popupManager == null)
            return;

        string title = string.IsNullOrWhiteSpace(popupTitle) ? "Goldworm" : popupTitle.Trim();
        popupManager.ShowPickupPopup(title, ResolvePopupIcon(), popupPixelsPerSpritePixel);
    }

    private Sprite ResolvePopupIcon()
    {
        if (popupIcon != null)
            return popupIcon;

        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        return spriteRenderer != null ? spriteRenderer.sprite : null;
    }

    private bool IsAlreadyCollected()
    {
        return Game.IsReady
            && Game.Ctx?.SaveState != null
            && Game.Ctx.SaveState.HasWorldState(ResolveFlagId());
    }

    private string ResolveFlagId()
    {
        string explicitId = NormalizeId(flagIdOverride);
        if (explicitId.Length > 0)
            return explicitId;

        if (persistentId != null && persistentId.TryGetId(out string persistentFlagId))
            return persistentFlagId;

        Scene scene = gameObject.scene;
        string sceneKey = string.IsNullOrWhiteSpace(scene.path) ? scene.name : scene.path;
        Vector3 position = transform.position;

        return string.Format(
            CultureInfo.InvariantCulture,
            "goldworm:{0}:{1}:{2:0.###},{3:0.###},{4:0.###}",
            sceneKey,
            BuildTransformPath(transform),
            position.x,
            position.y,
            position.z);
    }

    private static string BuildTransformPath(Transform target)
    {
        Stack<string> path = new Stack<string>();
        Transform current = target;

        while (current != null)
        {
            path.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", path);
    }

    private static string NormalizeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }
}
