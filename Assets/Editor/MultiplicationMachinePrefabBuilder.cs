using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MultiplicationMachinePrefabBuilder
{
    private const string SourcePrefabPath = "Assets/Prefabs/World/AdditionMachine.prefab";
    private const string TargetPrefabPath = "Assets/Prefabs/World/MultiplicationMachine.prefab";

    private const string SlotSpritePath = "Assets/Art/Mult-Slot.aseprite";
    private const string OutputSpritePath = "Assets/Art/Mult-Output-Slot.aseprite";
    private const string SymbolSpritePath = "Assets/Art/Mult-Symbol.aseprite";

    [MenuItem("Ducks Afar/Prefabs/Rebuild Multiplication Machine")]
    public static void Build()
    {
        GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
        Sprite slotSprite = LoadSprite(SlotSpritePath, "Mult-Slot");
        Sprite outputSprite = LoadSprite(OutputSpritePath, "Mult-Output-Slot");
        Sprite symbolSprite = LoadSprite(SymbolSpritePath, "Mult-Symbol");

        if (sourcePrefab == null || slotSprite == null || outputSprite == null || symbolSprite == null)
        {
            Debug.LogError("MultiplicationMachinePrefabBuilder: missing source prefab or multiplication sprites.");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
        try
        {
            instance.name = "MultiplicationMachine";

            AdditionMachine machine = instance.GetComponent<AdditionMachine>();
            if (machine == null)
            {
                Debug.LogError("MultiplicationMachinePrefabBuilder: source prefab has no AdditionMachine component.");
                return;
            }

            SetMachineOperation(machine);
            SetSlot(instance.transform, "SlotA", slotSprite, "Multiply hardworm packs.");
            SetSlot(instance.transform, "SlotB", slotSprite, "Multiply hardworm packs.");
            SetOutput(instance.transform, outputSprite);
            SetOperationSymbol(instance.transform, symbolSprite);

            PrefabUtility.SaveAsPrefabAsset(instance, TargetPrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"MultiplicationMachinePrefabBuilder: rebuilt {TargetPrefabPath}.");
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static Sprite LoadSprite(string path, string spriteName)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .FirstOrDefault(sprite => sprite.name == spriteName);
    }

    private static void SetMachineOperation(AdditionMachine machine)
    {
        var serializedMachine = new SerializedObject(machine);
        serializedMachine.FindProperty("operation").enumValueIndex = (int)AdditionMachine.MathOperation.Multiplication;
        serializedMachine.FindProperty("isSubtractionMachine").boolValue = false;
        serializedMachine.FindProperty("isAdditionMachine").boolValue = false;
        serializedMachine.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetSlot(Transform root, string slotName, Sprite slotSprite, string description)
    {
        Transform slot = FindChildRecursive(root, slotName);
        if (slot == null)
            return;

        SpriteRenderer slotRenderer = slot.GetComponent<SpriteRenderer>();
        if (slotRenderer != null)
            slotRenderer.sprite = slotSprite;

        AdditionSlotHoverPanel hoverPanel = slot.GetComponent<AdditionSlotHoverPanel>();
        if (hoverPanel != null)
        {
            var serializedHover = new SerializedObject(hoverPanel);
            serializedHover.FindProperty("description").stringValue = description;
            serializedHover.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetOutput(Transform root, Sprite outputSprite)
    {
        Transform output = FindChildRecursive(root, "OutputSlot");
        if (output == null)
            return;

        SpriteRenderer outputRenderer = output.GetComponent<SpriteRenderer>();
        if (outputRenderer != null)
            outputRenderer.sprite = outputSprite;

        AdditionOutputSlotHoverPanel hoverPanel = output.GetComponent<AdditionOutputSlotHoverPanel>();
        if (hoverPanel != null)
        {
            var serializedHover = new SerializedObject(hoverPanel);
            serializedHover.FindProperty("description").stringValue = "Claim multiplied hardworms.";
            serializedHover.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetOperationSymbol(Transform root, Sprite symbolSprite)
    {
        Transform symbol = FindChildRecursive(root, "Plus-Symbol") ?? FindChildRecursive(root, "Minus-Symbol");
        if (symbol == null)
            return;

        symbol.name = "Mult-Symbol";

        SpriteRenderer symbolRenderer = symbol.GetComponent<SpriteRenderer>();
        if (symbolRenderer != null)
            symbolRenderer.sprite = symbolSprite;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), childName);
            if (found != null)
                return found;
        }

        return null;
    }
}
