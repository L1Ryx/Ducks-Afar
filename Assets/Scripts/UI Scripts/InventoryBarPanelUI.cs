using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InventoryBarPanelUI : MonoBehaviour
{
    [Header("Panel Fade")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeSeconds = 0.20f;
    [SerializeField] private Ease fadeEase = Ease.OutQuad;

    [Header("Slots")]
    [SerializeField] private RectTransform slotsRoot;
    [SerializeField] private InventoryBarSlotUI slotTemplate;
    [SerializeField] private InventoryBarSlotUI[] slots = Array.Empty<InventoryBarSlotUI>();
    [SerializeField, Min(0f)] private float slotSpacing = 60f;

    [Header("Audio")] [SerializeField] private AudioCue ac;

    private int lastSelectedIndex = -1;
    private bool subscribed;
    private bool slotsInitialized;
    private int activeSlotCount = -1;
    private Vector2 slotSize = new(100f, 100f);
    private readonly List<InventoryBarSlotUI> managedSlots = new();

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Start hidden unless you want it visible in non-level scenes
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        EnsureSlotSetup();
        SetVisibleSlotCount(0);
    }

    private void OnEnable()
    {
        StartCoroutine(SubscribeWhenReady());
    }

    private void OnDisable()
    {
        Unsubscribe();
        DOTween.Kill(canvasGroup);
    }

    public void TestPrint()
    {
        Debug.Log("TestPrint");
    }

    private IEnumerator SubscribeWhenReady()
    {
        while (!Game.IsReady || Game.Ctx == null || Game.Ctx.Inventory == null || Game.Ctx.ItemDb == null || Game.Ctx.InventorySelection == null)
            yield return null;

        if (subscribed) yield break;

        Game.Ctx.Inventory.OnChanged += HandleInventoryChanged;
        subscribed = true;

        yield return null; // one frame
        SyncSlotCountFromInventory();

        RefreshSlots();
        ApplySelection(force: true);

    }

    private void Unsubscribe()
    {
        if (!subscribed) return;

        if (Game.IsReady && Game.Ctx != null && Game.Ctx.Inventory != null)
        {
            Game.Ctx.Inventory.OnChanged -= HandleInventoryChanged;
        }

        subscribed = false;
    }

    private void Update()
    {
        if (!Game.IsReady || Game.Ctx?.Inventory == null || Game.Ctx.InventorySelection == null) return;
        ApplySelection(force: false);
    }

    private void HandleInventoryChanged()
    {
        RefreshSlots();
        ApplySelection(force: true);
    }

    private void RefreshSlots()
    {
        if (!Game.IsReady || Game.Ctx?.Inventory == null || Game.Ctx.ItemDb == null)
            return;

        SyncSlotCountFromInventory();

        var entries = Game.Ctx.Inventory.Data.entries;

        for (int i = 0; i < activeSlotCount; i++)
        {
            InventoryBarSlotUI slot = i < managedSlots.Count ? managedSlots[i] : null;
            if (slot == null)
            {
                Debug.LogError($"InventoryBarPanelUI: slots[{i}] is NULL");
                continue;
            }

            if (entries == null || i >= entries.Count || string.IsNullOrEmpty(entries[i].itemId))
            {
                slot.SetEmpty();
                continue;
            }

            InventoryEntry entry = entries[i];
            var def = Game.Ctx.ItemDb.Get(entry.itemId);
            if (def == null)
            {
                Debug.LogWarning($"InventoryBarPanelUI: ItemDatabase missing def for '{entry.itemId}'");
                slot.SetEmpty();
            }
            else
            {
                slot.BindItem(def, entry.count);
            }
        }
    }


    private void ApplySelection(bool force)
    {
        var entries = Game.Ctx.Inventory.Data.entries;
        SyncSlotCountFromInventory();

        int entryCount = entries?.Count ?? 0;

        int selected = (entryCount > 0) ? Game.Ctx.InventorySelection.SelectedIndex : -1;

        if (!force && selected == lastSelectedIndex)
            return;

        // 🔊 PLAY SOUND WHEN SELECTION ACTUALLY CHANGES
        if (!force && lastSelectedIndex != -1 && selected != -1)
        {
            if (ac != null && Game.IsReady)
                Game.Ctx.Audio.PlayCueGlobal(ac);
        }

        // Only update what changed
        if (lastSelectedIndex >= 0 && lastSelectedIndex < managedSlots.Count)
            managedSlots[lastSelectedIndex]?.SetSelected(false);

        if (selected >= 0 && selected < activeSlotCount && selected < entryCount)
            managedSlots[selected]?.SetSelected(true);

        lastSelectedIndex = selected;
    }

    private void EnsureSlotSetup()
    {
        if (slotsInitialized)
        {
            EnsureLayoutGroup();
            return;
        }

        if (slotsRoot == null)
            slotsRoot = transform as RectTransform;

        managedSlots.Clear();

        if (slots != null)
        {
            foreach (InventoryBarSlotUI slot in slots)
            {
                if (slot != null && !managedSlots.Contains(slot))
                    managedSlots.Add(slot);
            }
        }

        if (managedSlots.Count == 0 && slotsRoot != null)
            managedSlots.AddRange(slotsRoot.GetComponentsInChildren<InventoryBarSlotUI>(true));

        if (slotTemplate == null && managedSlots.Count > 0)
            slotTemplate = managedSlots[0];

        managedSlots.Sort(CompareSlotSiblingOrder);
        CaptureSlotSize();
        ConfigureSlotLayout();
        EnsureLayoutGroup();
        slotsInitialized = true;
    }

    private int CompareSlotSiblingOrder(InventoryBarSlotUI a, InventoryBarSlotUI b)
    {
        if (a == b) return 0;
        if (a == null) return 1;
        if (b == null) return -1;

        return a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
    }

    private void CaptureSlotSize()
    {
        RectTransform templateRect = slotTemplate != null ? slotTemplate.transform as RectTransform : null;
        if (templateRect == null)
            return;

        Vector2 size = templateRect.sizeDelta;
        if (size.x > 0f && size.y > 0f)
            slotSize = size;
    }

    private void ConfigureSlotLayout()
    {
        for (int i = 0; i < managedSlots.Count; i++)
        {
            InventoryBarSlotUI slot = managedSlots[i];
            if (slot == null)
                continue;

            slot.transform.SetSiblingIndex(i);

            if (slot.transform is RectTransform rect)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, slotSize.x);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, slotSize.y);
            }

            if (!slot.TryGetComponent(out LayoutElement layoutElement))
                layoutElement = slot.gameObject.AddComponent<LayoutElement>();

            layoutElement.ignoreLayout = false;
            layoutElement.minWidth = slotSize.x;
            layoutElement.minHeight = slotSize.y;
            layoutElement.preferredWidth = slotSize.x;
            layoutElement.preferredHeight = slotSize.y;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;
        }
    }

    private void EnsureLayoutGroup()
    {
        if (slotsRoot == null)
            return;

        if (!slotsRoot.TryGetComponent(out HorizontalLayoutGroup layout))
            layout = slotsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();

        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = slotSpacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childScaleWidth = false;
        layout.childScaleHeight = false;
    }

    private void SyncSlotCountFromInventory()
    {
        int count = 0;
        if (Game.IsReady && Game.Ctx?.Inventory != null)
            count = Game.Ctx.Inventory.Data?.entries?.Count ?? 0;

        SetVisibleSlotCount(count);
    }

    private void SetVisibleSlotCount(int count)
    {
        EnsureSlotSetup();

        count = Mathf.Max(0, count);
        if (activeSlotCount == count && HasExpectedVisibleSlots(count))
            return;

        EnsureSlotsExist(count);
        ConfigureSlotLayout();
        ResizeSlotsRoot(count);

        for (int i = 0; i < managedSlots.Count; i++)
        {
            bool shouldShow = i < count;
            InventoryBarSlotUI slot = managedSlots[i];
            if (slot == null)
                continue;

            bool wasShowing = slot.gameObject.activeSelf;
            if (!shouldShow)
            {
                slot.SetSelected(false);
                slot.SetEmpty();
                slot.gameObject.SetActive(false);
                continue;
            }

            if (!wasShowing)
            {
                slot.ResetForReuse();
            }

            slot.gameObject.SetActive(shouldShow);
        }

        activeSlotCount = count;
        lastSelectedIndex = -1;
        RebuildLayoutNow();
    }

    private bool HasExpectedVisibleSlots(int count)
    {
        if (managedSlots.Count < count)
            return false;

        for (int i = 0; i < managedSlots.Count; i++)
        {
            InventoryBarSlotUI slot = managedSlots[i];
            if (slot == null)
                continue;

            bool shouldShow = i < count;
            if (slot.gameObject.activeSelf != shouldShow)
                return false;
        }

        return true;
    }

    private void EnsureSlotsExist(int count)
    {
        if (slotTemplate == null || slotsRoot == null)
            return;

        while (managedSlots.Count < count)
        {
            InventoryBarSlotUI slot = Instantiate(slotTemplate, slotsRoot);
            slot.name = $"Item {managedSlots.Count + 1} Frame";
            slot.CopyBaseStateFrom(slotTemplate);
            slot.ResetForReuse();
            managedSlots.Add(slot);
        }
    }

    private void ResizeSlotsRoot(int count)
    {
        if (slotsRoot == null)
            return;

        float width = count <= 0 ? slotSize.x : count * slotSize.x + Mathf.Max(0, count - 1) * slotSpacing;
        slotsRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }

    private void RebuildLayoutNow()
    {
        if (slotsRoot == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(slotsRoot);
        Canvas.ForceUpdateCanvases();
    }

    // Hook these from GameEventListener responses
    public void FadeInOnLevelStarted()
    {
        FadeTo(1f, true);
    }

    public void FadeOutOnLevelCompleted()
    {
        FadeTo(0f, false);
    }

    private void FadeTo(float alpha, bool interactive)
    {
        DOTween.Kill(canvasGroup);

        canvasGroup.DOFade(alpha, fadeSeconds)
            .SetEase(fadeEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                canvasGroup.interactable = interactive;
                canvasGroup.blocksRaycasts = interactive;
            });
    }
}
