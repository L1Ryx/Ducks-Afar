using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class DevDiagnosticsOverlay : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup root;
    [SerializeField] private TMP_Text text;
    [SerializeField] private int sortingOrder = 4800;

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F2;
    [SerializeField] private bool visibleOnStart = false;

    [Header("Refresh")]
    [SerializeField, Min(1f)] private float refreshRateHz = 8f;

    private readonly StringBuilder sb = new StringBuilder(512);
    private float timer;
    private bool visible;

    private void Awake()
    {
#if UNITY_EDITOR
        EnsureView();
        SetVisible(visibleOnStart);
#else
        gameObject.SetActive(false);
#endif
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(toggleKey))
            SetVisible(!visible);

        if (!visible || text == null)
            return;

        timer += Time.unscaledDeltaTime;
        float interval = 1f / Mathf.Max(1f, refreshRateHz);
        if (timer < interval)
            return;

        timer = 0f;
        text.text = BuildText();
#endif
    }

    public void SetVisible(bool shouldShow)
    {
        EnsureView();

        visible = shouldShow;
        if (root == null)
            return;

        root.alpha = visible ? 1f : 0f;
        root.interactable = false;
        root.blocksRaycasts = false;

        if (visible && text != null)
            text.text = BuildText();
    }

    private void EnsureView()
    {
        if (root == null)
            root = GetComponent<CanvasGroup>();

        if (text == null)
            text = GetComponentInChildren<TMP_Text>(true);

        if (root == null || text == null)
            BuildDefaultView();
    }

    private string BuildText()
    {
        sb.Clear();

        if (!Game.IsReady || Game.Ctx == null)
        {
            sb.AppendLine("DEV DIAGNOSTICS");
            sb.AppendLine("GameContext: not ready");
            return sb.ToString();
        }

        GameContext ctx = Game.Ctx;
        SaveStateModel save = ctx.SaveState;

        sb.AppendLine("DEV DIAGNOSTICS");
        sb.Append("Scene: ").AppendLine(SceneManager.GetActiveScene().name);
        sb.Append("Loading: ").AppendLine(ctx.SceneLoader != null && ctx.SceneLoader.IsLoading ? "yes" : "no");
        sb.Append("Level: ").AppendLine(ctx.LevelState != null ? ctx.LevelState.CurrentState.ToString() : "(null)");
        sb.Append("Pause: ");
        sb.Append(ctx.Pause != null && ctx.Pause.IsPaused ? "paused" : "running");
        sb.Append(" / pausable=");
        sb.AppendLine(ctx.Pause != null && ctx.Pause.IsScenePausable ? "yes" : "no");
        sb.Append("Dialogue: ").AppendLine(ctx.Dialogue != null && ctx.Dialogue.IsRunning ? "running" : "idle");
        sb.Append("Interaction Lock: ").AppendLine(ctx.InteractionLock != null && ctx.InteractionLock.IsLocked ? "locked" : "clear");
        sb.Append("Inventory Types: ").AppendLine(ctx.Inventory?.Data?.entries != null ? ctx.Inventory.Data.entries.Count.ToString() : "(null)");

        if (save != null)
        {
            sb.Append("Active Slot: ").AppendLine(save.HasActiveSlot ? save.ActiveSlotIndex.ToString() : "none");
            sb.Append("Save Scene: ").AppendLine(string.IsNullOrWhiteSpace(save.CurrentSceneName) ? "(none)" : save.CurrentSceneName);
            sb.Append("Location: ").AppendLine(string.IsNullOrWhiteSpace(save.CurrentLocation) ? "(none)" : save.CurrentLocation);
            sb.Append("Companion: ").AppendLine(string.IsNullOrWhiteSpace(save.CurrentCompanionId) ? "(none)" : save.CurrentCompanionId);
            sb.Append("Playtime: ").Append(save.TimePlayedSeconds.ToString("F1")).AppendLine("s");
        }

        return sb.ToString();
    }

    private void BuildDefaultView()
    {
        GameObject canvasObject = new GameObject("Dev Diagnostics Canvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root = canvasObject.AddComponent<CanvasGroup>();

        GameObject panelObject = new GameObject("Panel");
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(16f, -16f);
        panelRect.sizeDelta = new Vector2(460f, 330f);

        Image panel = panelObject.AddComponent<Image>();
        panel.color = new Color(0f, 0f, 0f, 0.7f);
        panel.raycastTarget = false;

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(panelObject.transform, false);
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14f, 12f);
        textRect.offsetMax = new Vector2(-14f, -12f);

        text = textObject.AddComponent<TextMeshProUGUI>();
        text.color = Color.white;
        text.fontSize = 22f;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;
    }
}
