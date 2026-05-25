using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class FieldTelescopePrefabBuilder
{
    private const string TelescopeSpritePath = "Assets/Art/Field-Telescope.aseprite";
    private const string DotSpritePath = "Assets/Art/Visual-Dot.aseprite";
    private const string HoverPanelPath = "Assets/Prefabs/Pickups/DuckHoverPanelUI.prefab";
    private const string PlayerMovedEventPath = "Assets/SOs/Events/OnPlayerMoved.asset";
    private const string PrefabPath = "Assets/Prefabs/World/Field Telescope.prefab";
    private const string ActiveHandGuid = "6a3be6617649240208b4a2e0453b7141";
    private const string InactiveHandGuid = "ba4d2723f32904fb093c35822a56cde2";

    [MenuItem("Ducks Afar/Build Field Telescope Prefab")]
    public static void Build()
    {
        AssetDatabase.Refresh();

        Sprite telescopeSprite = LoadSprite(TelescopeSpritePath, "Field-Telescope");
        Sprite dotSprite = LoadSprite(DotSpritePath, "Visual-Dot");
        GameObject hoverPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HoverPanelPath);

        if (telescopeSprite == null)
        {
            Debug.LogError($"Could not load telescope sprite at {TelescopeSpritePath}.");
            return;
        }

        if (dotSprite == null)
            Debug.LogWarning($"Could not load visual dot sprite at {DotSpritePath}.");

        if (hoverPanelPrefab == null)
            Debug.LogWarning($"Could not load hover panel prefab at {HoverPanelPath}.");

        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

        int interactableLayer = LayerMask.NameToLayer("Interactable");
        if (interactableLayer < 0) interactableLayer = 0;

        GameObject root = new GameObject("Field Telescope");
        root.layer = interactableLayer;

        var renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = telescopeSprite;
        renderer.sortingLayerName = "Stations";
        renderer.sortingOrder = 0;

        var bodyCollider = root.AddComponent<BoxCollider2D>();
        bodyCollider.isTrigger = false;
        bodyCollider.size = telescopeSprite.bounds.size;

        var telescope = root.AddComponent<FieldTelescope>();
        SetSerialized(telescope, "visualDotSprite", dotSprite);
        SetSerialized(telescope, "revealEndMode", FieldTelescopeRevealEndMode.UntilPlayerMoves);
        SetSerialized(telescope, "revealDuration", 2.5f);
        SetSerialized(telescope, "playerMovedEvent", AssetDatabase.LoadAssetAtPath<GameEvent>(PlayerMovedEventPath));

        GameObject trigger = new GameObject("Trigger Collider");
        trigger.layer = interactableLayer;
        trigger.transform.SetParent(root.transform, false);
        var triggerCollider = trigger.AddComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = new Vector2(1.5f, 1.5f);

        GameObject anchor = new GameObject("Anchor");
        anchor.layer = interactableLayer;
        anchor.transform.SetParent(root.transform, false);
        anchor.transform.localPosition = new Vector3(0f, 3f, 0f);

        var hover = root.AddComponent<FieldTelescopeHoverPanel>();
        SetSerialized(hover, "screenSpacePanelPrefab", hoverPanelPrefab);
        SetSerialized(hover, "worldAnchor", anchor.transform);
        SetSerialized(hover, "worldUIRootTag", "WorldUIRoot");
        SetSerialized(hover, "title", "Field Telescope");
        SetSerialized(hover, "description", "Reveal nearby points of interest.");
        SetSerialized(hover, "symbolActive", LoadFirstSpriteByGuid(ActiveHandGuid));
        SetSerialized(hover, "symbolInactive", LoadFirstSpriteByGuid(InactiveHandGuid));
        SetSerialized(hover, "showDuration", 0.18f);
        SetSerialized(hover, "hideDuration", 0.12f);
        SetSerialized(hover, "hiddenScale", 0.88f);
        SetSerialized(hover, "showEase", DG.Tweening.Ease.OutBack);
        SetSerialized(hover, "hideEase", DG.Tweening.Ease.InCubic);
        SetSerialized(hover, "activeAlpha", 0.95f);
        SetSerialized(hover, "inactiveAlpha", 0.7f);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        if (prefab == null)
        {
            Debug.LogError($"Failed to save {PrefabPath}.");
            return;
        }

        EditorUtility.SetDirty(prefab);
        AssetDatabase.SaveAssets();
        Debug.Log($"Built {PrefabPath}.");
    }

    private static Sprite LoadSprite(string path, string preferredName)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .FirstOrDefault(s => s.name == preferredName)
            ?? AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    }

    private static Sprite LoadFirstSpriteByGuid(string guid)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        return string.IsNullOrEmpty(path) ? null : LoadSprite(path, null);
    }

    private static void SetSerialized(Object target, string propertyName, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetSerialized(Object target, string propertyName, string value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetSerialized(Object target, string propertyName, float value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetSerialized(Object target, string propertyName, DG.Tweening.Ease value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.enumValueIndex = (int)value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetSerialized(Object target, string propertyName, FieldTelescopeRevealEndMode value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.enumValueIndex = (int)value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
