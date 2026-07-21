using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    private const string DynamicHardwormIdPrefix = "hardworm_pack_";
    private const int LargeHardwormIconPackSize = 7;

    [SerializeField] private List<ItemDefinition> items = new();
    public IReadOnlyList<ItemDefinition> Items => items;

    private Dictionary<string, ItemDefinition> byId;
    private readonly Dictionary<int, HardwormPackDefinition> dynamicHardwormsBySize = new();
    private readonly Dictionary<string, HardwormPackDefinition> dynamicHardwormsById = new();

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
        return Get(itemId) as T;
    }

    public ItemDefinition Get(string itemId)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        if (byId.TryGetValue(itemId, out var def))
            return def;

        return GetDynamicHardwormById(itemId);
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

        // Linear scan is fine for prototype scale.
        foreach (var def in items)
        {
            if (def is HardwormPackDefinition hw && hw.packSize == packSize)
                return hw;
        }

        return GetOrCreateDynamicHardworm(packSize);
    }

    public static string GetDynamicHardwormItemId(int packSize)
    {
        return $"{DynamicHardwormIdPrefix}{packSize}";
    }

    public static bool TryGetDynamicHardwormPackSize(string itemId, out int packSize)
    {
        packSize = 0;
        if (string.IsNullOrWhiteSpace(itemId) || !itemId.StartsWith(DynamicHardwormIdPrefix))
            return false;

        string value = itemId.Substring(DynamicHardwormIdPrefix.Length);
        return int.TryParse(value, out packSize) && packSize > 0;
    }

    private HardwormPackDefinition GetDynamicHardwormById(string itemId)
    {
        if (dynamicHardwormsById.TryGetValue(itemId, out HardwormPackDefinition cached))
            return cached;

        return TryGetDynamicHardwormPackSize(itemId, out int packSize)
            ? GetOrCreateDynamicHardworm(packSize)
            : null;
    }

    private HardwormPackDefinition GetOrCreateDynamicHardworm(int packSize)
    {
        if (dynamicHardwormsBySize.TryGetValue(packSize, out HardwormPackDefinition cached))
            return cached;

        HardwormPackDefinition template = FindHardwormTemplate(packSize);
        HardwormPackDefinition pack = CreateInstance<HardwormPackDefinition>();
        pack.hideFlags = HideFlags.HideAndDontSave;
        pack.itemId = GetDynamicHardwormItemId(packSize);
        pack.displayName = packSize == 1 ? "Single Hardworm" : $"Pack of {packSize} Hardworms";
        pack.description = template != null ? template.description : "Used for crafting.";
        pack.icon = template != null ? template.icon : null;
        pack.packSize = packSize;

        dynamicHardwormsBySize[packSize] = pack;
        dynamicHardwormsById[pack.itemId] = pack;
        return pack;
    }

    private HardwormPackDefinition FindHardwormTemplate(int packSize)
    {
        HardwormPackDefinition exact = null;
        HardwormPackDefinition large = null;
        HardwormPackDefinition highestBelow = null;

        foreach (var item in items)
        {
            if (item is not HardwormPackDefinition hardworm)
                continue;

            if (hardworm.packSize == packSize)
                exact = hardworm;

            if (hardworm.packSize == LargeHardwormIconPackSize)
                large = hardworm;

            if (hardworm.packSize < packSize && (highestBelow == null || hardworm.packSize > highestBelow.packSize))
                highestBelow = hardworm;
        }

        if (packSize >= LargeHardwormIconPackSize && large != null)
            return large;

        return exact != null ? exact : highestBelow;
    }

    public IReadOnlyList<ItemDefinition> AllItems => items;
}
