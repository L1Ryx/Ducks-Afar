using System;
using UnityEngine;

public class DuckPickup : MonoBehaviour, IInteractable
{
    [Header("Definition")]
    [SerializeField] private ItemDefinition itemDef;

    [SerializeField] private HardwormPackDefinition hardwormPackDefForSfx;

    [Header("Pickup Amount")]
    [Tooltip("Number of items granted (usually 1).")]
    [SerializeField] private int itemsGranted = 1;

    [Header("Events")]
    [SerializeField] private GameEvent onPickedUp; // optional: hook to your SO event system

    [SerializeField] private GameEvent onPickedUpGenericItem;

    public ItemDefinition ItemDef => itemDef;
    public int PacksGranted => itemsGranted;

    public void Interact(GameObject interactor)
    {

        if (itemsGranted <= 0) itemsGranted = 1;

        bool added = Game.Ctx.Inventory.TryAdd(itemDef.itemId, itemsGranted);
        if (!added)
        {
            Debug.Log($"DuckPickup '{name}': inventory full, cannot pick up '{itemDef.itemId}'.");
            return;
        }

        // SUCCESS
        Game.Ctx.HardwormPickupSfx?.PlayPickup(hardwormPackDefForSfx);
        if (onPickedUp != null) onPickedUp.Raise();
        if (onPickedUpGenericItem != null) onPickedUpGenericItem.Raise();

        Destroy(gameObject);
    }
}