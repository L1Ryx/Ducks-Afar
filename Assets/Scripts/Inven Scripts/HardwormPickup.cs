using System;
using UnityEngine;
using UnityEngine.Events;

public class HardwormPickup : MonoBehaviour, IInteractable
{
    [Header("Definition")]
    [SerializeField] private HardwormPackDefinition packDef;

    [Header("Pickup Amount")]
    [Tooltip("Number of packs granted (usually 1).")]
    [SerializeField] private int packsGranted = 1;

    [Header("Events")]
    [SerializeField] private GameEvent onPickedUp; // optional: hook to your SO event system

    [SerializeField] private GameEvent onPickedUpGenericItem;
    [SerializeField] private UnityEvent onSuccess;
    
    public HardwormPackDefinition PackDef => packDef;
    public int PacksGranted => packsGranted;
    public UnityEvent OnSuccessEvent => onSuccess;

    public void Interact(GameObject interactor)
    {
        if (!Game.IsReady || Game.Ctx.Inventory == null)
        {
            Debug.LogWarning("Cannot pick up hardworm: GameContext/Inventory not ready. Start from Bootstrap scene.");
            return;
        }

        if (packDef == null || string.IsNullOrWhiteSpace(packDef.itemId))
        {
            Debug.LogError($"HardwormPickup on '{name}' is missing a valid HardwormPackDefinition (itemId).");
            return;
        }

        if (packsGranted <= 0) packsGranted = 1;

        bool added = Game.Ctx.Inventory.TryAdd(packDef.itemId, packsGranted);
        if (!added)
        {
            Debug.Log($"HardwormPickup '{name}': inventory full, cannot pick up '{packDef.itemId}'.");
            return;
        }

        // SUCCESS
        Game.Ctx.HardwormPickupSfx?.PlayPickup(packDef);
        if (onPickedUp != null) onPickedUp.Raise();
        if (onPickedUpGenericItem != null) onPickedUpGenericItem.Raise();
        onSuccess?.Invoke();

        Destroy(gameObject);
    }
}
