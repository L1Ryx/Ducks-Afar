public static class ItemQuantityFormatter
{
    public static string FormatNameWithCount(ItemDefinition item, int count)
    {
        if (item == null)
            return string.Empty;

        return FormatNameWithCount(item.displayName, count);
    }

    public static string FormatNameWithCount(string itemName, int count)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return string.Empty;

        return count > 1 ? $"{itemName} \u00d7{count}" : itemName;
    }
}
