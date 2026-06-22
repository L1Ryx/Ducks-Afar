using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private List<ItemDefinition> items = new();
    public IReadOnlyList<ItemDefinition> Items => items;

    private Dictionary<string, ItemDefinition> byId;

    public void Init()
    {
        byId = new Dictionary<string, ItemDefinition>(items.Count);

        foreach (var def in items)
        {
            if (def == null) continue;
            if (string.IsNullOrWhiteSpace(def.itemId)) continue;

            byId[def.itemId] = def;
        }
    }

    public T Get<T>(string itemId) where T : ItemDefinition
    {
        EnsureInitialized();
        return byId.TryGetValue(itemId, out var def) ? def as T : null;
    }

    public ItemDefinition Get(string itemId)
    {
        EnsureInitialized();
        return byId.TryGetValue(itemId, out var def) ? def : null;
    }

    private void EnsureInitialized()
    {
        if (byId == null)
        {
            Init();
            return;
        }

        if (byId.Count != items.Count)
            Init();
    }
    
    public HardwormPackDefinition GetHardwormByPackSize(int packSize)
    {
        if (packSize <= 0) return null;

        // Linear scan is fine for prototype scale
        foreach (var def in items)
        {
            if (def is HardwormPackDefinition hw && hw.packSize == packSize)
                return hw;
        }

        return null;
    }

    

    public IReadOnlyList<ItemDefinition> AllItems => items;
}
