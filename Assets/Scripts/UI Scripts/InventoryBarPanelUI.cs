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
    private readonly List<InventoryBarSlotUI> managedSlots = new();
    private readonly List<SlotSnapshot> snapshot = new();
    
    private struct SlotSnapshot
    {
        public string itemId;
        public int count;
    }

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
        RebaseSlots();

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
            if (managedSlots[i] == null)
            {
                Debug.LogError($"InventoryBarPanelUI: slots[{i}] is NULL");
                continue;
            }

            // Desired state for this UI slot
            string desiredItemId = null;
            int desiredCount = 0;

            if (entries != null && i < entries.Count && !string.IsNullOrEmpty(entries[i].itemId))
            {
                desiredItemId = entries[i].itemId;
                desiredCount = entries[i].count;
            }

            // Normalize empties
            if (string.IsNullOrEmpty(desiredItemId))
            {
                desiredItemId = null;
                desiredCount = 0;
            }

            // If unchanged, do nothing (prevents jerk)
            if (snapshot[i].itemId == desiredItemId && snapshot[i].count == desiredCount)
                continue;

            // Update snapshot first
            SlotSnapshot updatedSnapshot = snapshot[i];
            updatedSnapshot.itemId = desiredItemId;
            updatedSnapshot.count = desiredCount;
            snapshot[i] = updatedSnapshot;

            // Apply UI change only when needed
            if (desiredItemId == null)
            {
                managedSlots[i].SetEmpty();
            }
            else
            {
                var def = Game.Ctx.ItemDb.Get(desiredItemId);
                if (def == null)
                {
                    Debug.LogWarning($"InventoryBarPanelUI: ItemDatabase missing def for '{desiredItemId}'");
                    managedSlots[i].SetEmpty();
                }
                else
                {
                    managedSlots[i].BindItem(def, desiredCount);
                }
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

        EnsureLayoutGroup();
        EnsureSnapshotSize(managedSlots.Count);
        slotsInitialized = true;
    }

    private void EnsureLayoutGroup()
    {
        if (slotsRoot == null)
            return;

        if (!slotsRoot.TryGetComponent(out HorizontalLayoutGroup layout))
            layout = slotsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();

        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = slotSpacing;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
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
        if (activeSlotCount == count && managedSlots.Count >= count)
            return;

        EnsureSlotsExist(count);
        EnsureSnapshotSize(count);

        for (int i = 0; i < managedSlots.Count; i++)
        {
            bool shouldShow = i < count;
            InventoryBarSlotUI slot = managedSlots[i];
            if (slot == null)
                continue;

            if (!shouldShow)
            {
                slot.SetSelected(false);
                slot.SetEmpty();
            }

            slot.gameObject.SetActive(shouldShow);
        }

        activeSlotCount = count;
        lastSelectedIndex = -1;
        if (slotsRoot != null)
            LayoutRebuilder.MarkLayoutForRebuild(slotsRoot);
        RebaseSlots();
    }

    private void EnsureSlotsExist(int count)
    {
        if (slotTemplate == null || slotsRoot == null)
            return;

        while (managedSlots.Count < count)
        {
            InventoryBarSlotUI slot = Instantiate(slotTemplate, slotsRoot);
            slot.name = $"Item {managedSlots.Count + 1} Frame";
            managedSlots.Add(slot);
        }
    }

    private void EnsureSnapshotSize(int count)
    {
        while (snapshot.Count < count)
            snapshot.Add(default);

        for (int i = count; i < snapshot.Count; i++)
            snapshot[i] = default;
    }

    private void RebaseSlots()
    {
        for (int i = 0; i < activeSlotCount && i < managedSlots.Count; i++)
            managedSlots[i]?.Rebase();
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
