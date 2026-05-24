using System.Collections.Generic;
using UnityEngine;

public class NewItemPopupManager : MonoBehaviour
{
    [Header("Prefab & Parent")]
    [SerializeField] private NewItemPopupUI popupPrefab;
    [SerializeField] private RectTransform popupParent;

    [Header("Layout")]
    [SerializeField] private RectTransform anchor;

    [Header("Timing")]
    [SerializeField] private float fadeInSeconds = 0.12f;
    [SerializeField] private float holdSeconds = 1.25f;
    [SerializeField] private float fadeOutSeconds = 0.18f;

    [Header("Exit Animation")]
    [SerializeField] private Vector2 exitDirection = Vector2.right;
    [SerializeField] private float exitDistance = 640f;

    [Header("Optional")]
    [SerializeField] private bool showQuantitySuffixIfMultiple = false;

    private NewItemPopupUI activePopup;

    // Snapshot of last-known counts
    private readonly Dictionary<string, int> prevCounts = new();

    private void Awake()
    {
        if (popupParent == null)
            popupParent = transform as RectTransform;
        
        if (anchor == null)
            Debug.LogWarning("NewItemPopupManager: Anchor is not assigned.");
    }

    private void OnEnable()
    {
        // Prime snapshot if game is ready; otherwise the first event will populate.
        TryRefreshSnapshotFromGame();
    }

    /// <summary>
    /// Hook this to your GameEventListener UnityEvent Response
    /// for the InventoryChangedEventBridge's onInventoryChanged GameEvent.
    /// </summary>
    public void HandleInventoryChangedEvent()
    {
        if (!Game.IsReady || Game.Ctx?.Inventory == null || Game.Ctx.ItemDb == null)
            return;

        var currentCounts = BuildCounts(Game.Ctx.Inventory);

        foreach (var kvp in currentCounts)
        {
            prevCounts.TryGetValue(kvp.Key, out var previous);
            int delta = kvp.Value - previous;

            if (delta > 0)
                SpawnPopup(kvp.Key, delta);
        }

        prevCounts.Clear();
        foreach (var kvp in currentCounts)
            prevCounts[kvp.Key] = kvp.Value;
    }

    private void SpawnPopup(string itemId, int deltaAdded)
    {
        if (popupPrefab == null)
        {
            Debug.LogWarning("NewItemPopupManager: popupPrefab is not assigned.");
            return;
        }

        var def = Game.Ctx.ItemDb.Get(itemId);
        if (def == null)
        {
            Debug.LogWarning($"NewItemPopupManager: ItemDatabase has no definition for itemId '{itemId}'.");
            return;
        }

        ExitActivePopup();

        var popup = Instantiate(popupPrefab, popupParent, false);
        popup.Bind(def);
        popup.RebaseFloatyIfPresent();
        PositionAtAnchor(popup);

        activePopup = popup;

        popup.Play(fadeInSeconds, holdSeconds, fadeOutSeconds, exitDirection, exitDistance, () =>
        {
            if (activePopup == popup)
                activePopup = null;

            Destroy(popup.gameObject);
        });
    }

    private void PositionAtAnchor(NewItemPopupUI popup)
    {
        if (anchor == null || popup == null || popup.RectTransform == null)
            return;

        popup.RectTransform.anchoredPosition = anchor.anchoredPosition;
    }

    private void ExitActivePopup()
    {
        if (activePopup == null)
            return;

        var exitingPopup = activePopup;
        activePopup = null;

        exitingPopup.PlayExit(fadeOutSeconds, exitDirection, exitDistance, () =>
        {
            Destroy(exitingPopup.gameObject);
        });
    }

    private Dictionary<string, int> BuildCounts(InventoryModel inventory)
    {
        var dict = new Dictionary<string, int>();
        var entries = inventory.Data.entries;

        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (!string.IsNullOrEmpty(e.itemId))
                dict[e.itemId] = e.count;
        }

        return dict;
    }

    private void TryRefreshSnapshotFromGame()
    {
        prevCounts.Clear();

        if (!Game.IsReady || Game.Ctx == null || Game.Ctx.Inventory == null)
            return;

        var entries = Game.Ctx.Inventory.Data.entries;
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (string.IsNullOrWhiteSpace(e.itemId)) continue;
            prevCounts[e.itemId] = e.count;
        }
    }
}
