using UnityEngine;

public class GrantInventoryItemOnEvent : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private GameEvent triggerEvent;

    [Header("Grant")]
    [SerializeField] private ItemDefinition item;
    [SerializeField, Min(1)] private int amount = 1;

    private void OnEnable()
    {
        triggerEvent?.RegisterRuntimeListener(GrantItem);
    }

    private void OnDisable()
    {
        triggerEvent?.UnregisterRuntimeListener(GrantItem);
    }

    public void GrantItem()
    {
        if (!Game.IsReady || Game.Ctx?.Inventory == null)
        {
            Debug.LogWarning($"{nameof(GrantInventoryItemOnEvent)} on '{name}' could not grant an item because the inventory is not ready.", this);
            return;
        }

        if (item == null || string.IsNullOrWhiteSpace(item.itemId))
        {
            Debug.LogWarning($"{nameof(GrantInventoryItemOnEvent)} on '{name}' is missing a valid item definition.", this);
            return;
        }

        if (!Game.Ctx.Inventory.TryAdd(item.itemId, Mathf.Max(1, amount)))
            Debug.LogWarning($"{nameof(GrantInventoryItemOnEvent)} on '{name}' could not add item '{item.itemId}'.", this);
    }
}
