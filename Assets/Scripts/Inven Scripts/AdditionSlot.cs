using UnityEngine;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine.Events;

public class AdditionSlot : MonoBehaviour, IInteractable
{
    [Header("Visual (prototype for later)")]
    [SerializeField] private SpriteRenderer filledSpriteRenderer;
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private Sprite filledSprite;
    [SerializeField] private SpriteRenderer itemIconRenderer;
    
    [Header("Icon Visual")]
    [SerializeField] private Transform itemIconRoot;

    [Header("Icon Tween")]
    [SerializeField] private float iconShowDuration = 0.16f;
    [SerializeField] private float iconHideDuration = 0.10f;
    [SerializeField] private float iconHiddenScale = 0.75f;
    [SerializeField] private Ease iconShowEase = Ease.OutBack;
    [SerializeField] private Ease iconHideEase = Ease.InCubic;
    [SerializeField] private float iconFadeDuration = 0.10f;
    [Header("Events")] [SerializeField] private UnityEvent onSlotSlateChanged;
    
    [SerializeField] private AdditionMachine machine;
    private AdditionMachineCadenceSfx cadenceSfx;
    private SubtractionMachineCadenceSfx cadenceSfxSub;


    private Tween iconTween;

    public bool IsEmpty => !HasValue;


    public bool HasValue { get; private set; }
    public int Value { get; private set; }
    public string StoredItemId { get; private set; } // for refund
    private void Awake()
    {
        if (itemIconRoot == null && itemIconRenderer != null)
            itemIconRoot = itemIconRenderer.transform;

        if (machine != null)
        {
            cadenceSfx = machine.GetComponent<AdditionMachineCadenceSfx>();
            cadenceSfxSub = machine.GetComponent<SubtractionMachineCadenceSfx>();
        }
        SetIconHiddenImmediate();
    }
    
    public string StoredItemName
    {
        get
        {
            if (!HasValue || string.IsNullOrEmpty(StoredItemId)) return null;
            var def = Game.Ctx?.ItemDb?.Get(StoredItemId);
            return def != null ? def.displayName : null;
        }
    }

    public Sprite StoredItemIcon
    {
        get
        {
            if (!HasValue || string.IsNullOrEmpty(StoredItemId)) return null;
            var def = Game.Ctx?.ItemDb?.Get(StoredItemId);
            return def != null ? def.icon : null;
        }
    }

    private void SetIconHiddenImmediate()
    {
        if (itemIconRenderer == null) return;

        iconTween?.Kill();

        itemIconRenderer.sprite = null;
        itemIconRenderer.enabled = false;

        // Reset alpha to 1 so future fades behave predictably
        var c = itemIconRenderer.color;
        c.a = 1f;
        itemIconRenderer.color = c;

        if (itemIconRoot != null)
            itemIconRoot.localScale = Vector3.one * iconHiddenScale;
    }

    public void Interact(GameObject interactor)
    {
        if (!Game.IsReady || Game.Ctx.Inventory == null || Game.Ctx.ItemDb == null || Game.Ctx.InventorySelection == null)
        {
            Debug.LogWarning($"{name}: Missing GameContext dependencies.");
            return;
        }

        var inv = Game.Ctx.Inventory;

        // If filled, clicking removes it (refund) for prototype convenience
        if (HasValue)
        {
            // Refund back to inventory (may fail if inventory is full of types)
            bool refunded = inv.TryAdd(StoredItemId, 1);
            if (!refunded)
            {
                Debug.Log($"{name}: Cannot refund {StoredItemId} because backpack is full (4 types).");
                return;
            }

            // SUCCESS
            ItemDefinition itemDef = Game.Ctx.ItemDb.Get(StoredItemId);
            if (itemDef is HardwormPackDefinition hwItemDef)
            {
                Game.Ctx.HardwormPickupSfx?.PlayPickup(hwItemDef);
            }
            else
            {
                // Fallback: still play at least one tick if something unexpected is stored
                Game.Ctx.HardwormPickupSfx?.PlayPickupCount(1);
                Debug.LogWarning("hwItemDef not assigned. Playing pickup sound for 1.");
            }
            ClearNoRefund(); // state clear only; refund already done
            UpdateVisual();
            onSlotSlateChanged?.Invoke();
            return;
        }

        // Empty slot: place currently selected item (must be a hardworm pack)
        string selectedItemId = Game.Ctx.InventorySelection.GetSelectedItemId();
        if (string.IsNullOrEmpty(selectedItemId))
            return;

        var def = Game.Ctx.ItemDb.Get(selectedItemId);
        if (def is not HardwormPackDefinition hwDef)
        {
            Debug.Log($"{name}: Selected item is not a hardworm pack.");
            return;
        }

        // Must have at least 1 pack
        if (inv.GetCount(selectedItemId) < 1)
        {
            Debug.Log($"{name}: Not enough of selected item to place.");
            return;
        }

        // Consume 1 pack
        if (!inv.TryRemove(selectedItemId, 1))
        {
            Debug.LogWarning($"{name}: Failed to remove selected item unexpectedly.");
            return;
        }

        HasValue = true;
        Value = hwDef.packSize;
        StoredItemId = selectedItemId;
        ShowIcon(def.icon);
        
        cadenceSfx?.PlayPlacement(this, hwDef.packSize);
        cadenceSfxSub?.PlayPlacement(this, hwDef.packSize);


        UpdateVisual();
        onSlotSlateChanged?.Invoke();
    }
    
    private void ShowIcon(Sprite icon)
    {
        if (itemIconRenderer == null || itemIconRoot == null) return;

        iconTween?.Kill();

        itemIconRenderer.sprite = icon;
        itemIconRenderer.enabled = (icon != null);

        // Start hidden baseline each time
        itemIconRoot.localScale = Vector3.one * iconHiddenScale;

        var c = itemIconRenderer.color;
        c.a = 0f;
        itemIconRenderer.color = c;

        iconTween = DOTween.Sequence()
            .Join(itemIconRoot.DOScale(1f, iconShowDuration).SetEase(iconShowEase))
            .Join(itemIconRenderer.DOFade(1f, iconFadeDuration))
            .SetUpdate(true);
    }

    private void HideIcon()
    {
        if (itemIconRenderer == null || itemIconRoot == null) return;
        if (itemIconRenderer.sprite == null) return;

        iconTween?.Kill();

        iconTween = DOTween.Sequence()
            .Join(itemIconRoot.DOScale(iconHiddenScale, iconHideDuration).SetEase(iconHideEase))
            .Join(itemIconRenderer.DOFade(0f, iconHideDuration))
            .SetUpdate(true)
            .OnComplete(() =>
            {
                itemIconRenderer.sprite = null;
                itemIconRenderer.enabled = false;

                var c = itemIconRenderer.color;
                c.a = 1f;
                itemIconRenderer.color = c;
            });
    }


    public void ClearNoRefund()
    {
        HasValue = false;
        Value = 0;
        StoredItemId = null;

        HideIcon();
        UpdateVisual();
        onSlotSlateChanged?.Invoke();
    }


    private void Reset()
    {
        filledSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // If your child is named exactly "ItemIcon"
        var iconTf = transform.Find("ItemIcon");
        if (iconTf != null)
        {
            itemIconRoot = iconTf;
            itemIconRenderer = iconTf.GetComponent<SpriteRenderer>();
        }
    }


    private void UpdateVisual()
    {
        if (filledSpriteRenderer == null) return;

        if (!HasValue)
        {
            if (emptySprite != null) filledSpriteRenderer.sprite = emptySprite;
        }
        else
        {
            if (filledSprite != null) filledSpriteRenderer.sprite = filledSprite;
        }
    }
}
