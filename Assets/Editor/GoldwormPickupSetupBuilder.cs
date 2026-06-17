using System.IO;
using System.Linq;
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GoldwormPickupSetupBuilder
{
    private const string PickupFolder = "Assets/Prefabs/Pickups";
    private const string GoldwormBasePrefabPath = PickupFolder + "/GoldwormPickup_Base.prefab";
    private const string CurrencyPanelPrefabPath = "Assets/Prefabs/UI/Currency Panel.prefab";
    private const string BootstrapScenePath = "Assets/Scenes/Live/Bootstrap.unity";
    private const string GoldwormPickedUpEventPath = "Assets/SOs/Events/OnPickedUp/OnPickedUpGoldworm.asset";
    private const string GoldwormShowEventPath = "Assets/SOs/Events/OnGoldwormsShowIndefinitely.asset";
    private const string GoldwormHideEventPath = "Assets/SOs/Events/OnGoldwormsHideIndefinitely.asset";
    private const string FadeOutOfLevelStartEventPath = "Assets/SOs/Events/OnFadeOutOfLevelStart.asset";
    private const string GoldwormHoverPanelPrefabPath = PickupFolder + "/GoldwormHoverPanelUI.prefab";
    private const string HoverSettingsSourcePrefabPath = "Assets/Prefabs/Pickups/HardwormPickup_Base.prefab";

    [MenuItem("Ducks Afar/Build Goldworm Pickups")]
    public static void Build()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        EnsureFolder(PickupFolder);
        EnsureFolder("Assets/SOs/Events/OnPickedUp");

        GameEvent pickedUpEvent = EnsureGameEvent(GoldwormPickedUpEventPath);
        GameEvent showEvent = EnsureGameEvent(GoldwormShowEventPath);
        GameEvent hideEvent = EnsureGameEvent(GoldwormHideEventPath);
        GameEvent fadeOutOfLevelStartEvent = EnsureGameEvent(FadeOutOfLevelStartEventPath);

        HoverPanelSettings hoverSettings = LoadHoverPanelSettings();

        GameObject basePrefab = CreateBasePickupPrefab(pickedUpEvent, hoverSettings);
        CreatePickupVariant(basePrefab, "GoldwormPickup_A", "Assets/Art/Goldworm-A.aseprite", "Goldworm-A");
        CreatePickupVariant(basePrefab, "GoldwormPickup_B", "Assets/Art/Goldworm-B.aseprite", "Goldworm-B");
        CreatePickupVariant(basePrefab, "GoldwormPickup_C", "Assets/Art/Goldworm-C.aseprite", "Goldworm-C");

        HookCurrencyPanelPrefab(showEvent, hideEvent, fadeOutOfLevelStartEvent);
        HookBootstrapCurrencyPanel(showEvent, hideEvent, fadeOutOfLevelStartEvent);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Goldworm pickup setup complete.");
    }

    private static GameObject CreateBasePickupPrefab(
        GameEvent pickedUpEvent,
        HoverPanelSettings hoverSettings)
    {
        GameObject root = new GameObject("GoldwormPickup_Base");
        root.layer = LayerMask.NameToLayer("Interactable");

        GameObject anchor = new GameObject("Anchor");
        anchor.layer = root.layer;
        anchor.transform.SetParent(root.transform, worldPositionStays: false);
        anchor.transform.localPosition = Vector3.up;

        SpriteRenderer spriteRenderer = root.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingLayerName = "Items";

        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = Vector2.one;

        root.AddComponent<YSort>();

        GoldwormPickup pickup = root.AddComponent<GoldwormPickup>();
        SerializedObject pickupObject = new SerializedObject(pickup);
        pickupObject.FindProperty("goldwormsGranted").intValue = 1;
        pickupObject.FindProperty("saveImmediately").boolValue = true;
        pickupObject.FindProperty("onPickedUp").objectReferenceValue = pickedUpEvent;
        pickupObject.ApplyModifiedPropertiesWithoutUndo();

        Type hoverPanelType = Type.GetType("GoldwormHoverPanel, Assembly-CSharp");
        if (hoverPanelType == null)
            throw new InvalidOperationException("Could not resolve GoldwormHoverPanel from Assembly-CSharp.");

        MonoBehaviour hoverPanel = root.AddComponent(hoverPanelType) as MonoBehaviour;
        SerializedObject hoverObject = new SerializedObject(hoverPanel);
        hoverObject.FindProperty("screenSpacePanelPrefab").objectReferenceValue = hoverSettings.ScreenSpacePanelPrefab;
        hoverObject.FindProperty("worldAnchor").objectReferenceValue = anchor.transform;
        hoverObject.FindProperty("worldUIRootTag").stringValue = hoverSettings.WorldUIRootTag;
        hoverObject.FindProperty("title").stringValue = "Goldworm";
        hoverObject.FindProperty("showDuration").floatValue = hoverSettings.ShowDuration;
        hoverObject.FindProperty("hideDuration").floatValue = hoverSettings.HideDuration;
        hoverObject.FindProperty("hiddenScale").floatValue = hoverSettings.HiddenScale;
        hoverObject.FindProperty("showEase").enumValueIndex = hoverSettings.ShowEaseIndex;
        hoverObject.FindProperty("hideEase").enumValueIndex = hoverSettings.HideEaseIndex;
        hoverObject.FindProperty("inRangeAlpha").floatValue = hoverSettings.InRangeAlpha;
        hoverObject.FindProperty("outOfRangeAlpha").floatValue = hoverSettings.OutOfRangeAlpha;
        hoverObject.FindProperty("symbolInRange").objectReferenceValue = hoverSettings.SymbolInRange;
        hoverObject.FindProperty("symbolOutOfRange").objectReferenceValue = hoverSettings.SymbolOutOfRange;
        hoverObject.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, GoldwormBasePrefabPath);
        UnityEngine.Object.DestroyImmediate(root);

        GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GoldwormBasePrefabPath);
        if (basePrefab == null)
            throw new FileNotFoundException($"Could not create or load base prefab at '{GoldwormBasePrefabPath}'.");

        return basePrefab;
    }

    private static void CreatePickupVariant(
        GameObject basePrefab,
        string prefabName,
        string spriteAssetPath,
        string spriteName)
    {
        Sprite sprite = LoadSprite(spriteAssetPath, spriteName);
        if (sprite == null)
            throw new FileNotFoundException($"Could not find sprite '{spriteName}' in '{spriteAssetPath}'.");

        GameObject root = PrefabUtility.InstantiatePrefab(basePrefab) as GameObject;
        if (root == null)
            throw new InvalidOperationException($"Could not instantiate base prefab '{GoldwormBasePrefabPath}'.");

        root.name = prefabName;

        SpriteRenderer spriteRenderer = root.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            throw new MissingComponentException($"{GoldwormBasePrefabPath} is missing a SpriteRenderer.");

        spriteRenderer.sprite = sprite;
        PrefabUtility.RecordPrefabInstancePropertyModifications(spriteRenderer);

        string prefabPath = $"{PickupFolder}/{prefabName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void HookCurrencyPanelPrefab(
        GameEvent showEvent,
        GameEvent hideEvent,
        GameEvent fadeOutOfLevelStartEvent)
    {
        if (!File.Exists(CurrencyPanelPrefabPath))
            return;

        GameObject root = PrefabUtility.LoadPrefabContents(CurrencyPanelPrefabPath);
        try
        {
            GoldwormCurrencyView view = ConfigureGoldwormSection(
                root.transform,
                showEvent,
                hideEvent,
                fadeOutOfLevelStartEvent);
            if (view != null)
                PrefabUtility.SaveAsPrefabAsset(root, CurrencyPanelPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void HookBootstrapCurrencyPanel(
        GameEvent showEvent,
        GameEvent hideEvent,
        GameEvent fadeOutOfLevelStartEvent)
    {
        if (!File.Exists(BootstrapScenePath))
            return;

        var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
        bool changed = false;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            GoldwormCurrencyView view = ConfigureGoldwormSection(
                root.transform,
                showEvent,
                hideEvent,
                fadeOutOfLevelStartEvent);
            changed |= view != null;
        }

        if (changed)
            EditorSceneManager.SaveScene(scene);
    }

    private static GoldwormCurrencyView ConfigureGoldwormSection(
        Transform root,
        GameEvent showEvent,
        GameEvent hideEvent,
        GameEvent fadeOutOfLevelStartEvent)
    {
        Transform section = FindDeepChild(root, "Goldworms Section");
        if (section == null)
            return null;

        CanvasGroup group = section.GetComponent<CanvasGroup>();
        if (group == null)
            group = section.gameObject.AddComponent<CanvasGroup>();

        TMP_Text amountText = section.GetComponentInChildren<TMP_Text>(true);
        GoldwormCurrencyView view = section.GetComponent<GoldwormCurrencyView>();
        if (view == null)
            view = section.gameObject.AddComponent<GoldwormCurrencyView>();

        SerializedObject viewObject = new SerializedObject(view);
        viewObject.FindProperty("sectionGroup").objectReferenceValue = group;
        viewObject.FindProperty("amountText").objectReferenceValue = amountText;
        viewObject.FindProperty("fadeInSeconds").floatValue = 0.18f;
        viewObject.FindProperty("fadeOutSeconds").floatValue = 0.35f;
        viewObject.FindProperty("visibleAlpha").floatValue = 1f;
        viewObject.FindProperty("useUnscaledTime").boolValue = true;
        viewObject.FindProperty("showIndefinitelyEvent").objectReferenceValue = showEvent;
        viewObject.FindProperty("hideIndefinitelyEvent").objectReferenceValue = hideEvent;
        viewObject.FindProperty("fadeOutForLevelTransitionEvent").objectReferenceValue = fadeOutOfLevelStartEvent;
        viewObject.ApplyModifiedPropertiesWithoutUndo();

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        EditorUtility.SetDirty(section.gameObject);

        return view;
    }

    private static Sprite LoadSprite(string assetPath, string spriteName)
    {
        return AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<Sprite>()
            .FirstOrDefault(sprite => sprite.name == spriteName)
            ?? AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static HoverPanelSettings LoadHoverPanelSettings()
    {
        HoverPanelSettings settings = new HoverPanelSettings
        {
            ScreenSpacePanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GoldwormHoverPanelPrefabPath),
            WorldUIRootTag = "WorldUIRoot",
            ShowDuration = 0.18f,
            HideDuration = 0.12f,
            HiddenScale = 0.88f,
            ShowEaseIndex = 27,
            HideEaseIndex = 8,
            InRangeAlpha = 0.95f,
            OutOfRangeAlpha = 0.75f
        };

        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(HoverSettingsSourcePrefabPath);
        if (source == null)
            return settings;

        HardwormHoverPanel sourcePanel = source.GetComponent<HardwormHoverPanel>();
        if (sourcePanel == null)
            return settings;

        SerializedObject sourceObject = new SerializedObject(sourcePanel);
        settings.WorldUIRootTag = sourceObject.FindProperty("worldUIRootTag").stringValue;
        settings.ShowDuration = sourceObject.FindProperty("showDuration").floatValue;
        settings.HideDuration = sourceObject.FindProperty("hideDuration").floatValue;
        settings.HiddenScale = sourceObject.FindProperty("hiddenScale").floatValue;
        settings.ShowEaseIndex = sourceObject.FindProperty("showEase").enumValueIndex;
        settings.HideEaseIndex = sourceObject.FindProperty("hideEase").enumValueIndex;
        settings.InRangeAlpha = sourceObject.FindProperty("inRangeAlpha").floatValue;
        settings.OutOfRangeAlpha = sourceObject.FindProperty("outOfRangeAlpha").floatValue;
        settings.SymbolInRange = sourceObject.FindProperty("symbolInRange").objectReferenceValue as Sprite;
        settings.SymbolOutOfRange = sourceObject.FindProperty("symbolOutOfRange").objectReferenceValue as Sprite;
        return settings;
    }

    private static GameEvent EnsureGameEvent(string path)
    {
        GameEvent gameEvent = AssetDatabase.LoadAssetAtPath<GameEvent>(path);
        if (gameEvent != null)
            return gameEvent;

        EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
        gameEvent = ScriptableObject.CreateInstance<GameEvent>();
        AssetDatabase.CreateAsset(gameEvent, path);
        return gameEvent;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            return;

        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string folderName = Path.GetFileName(folderPath);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root.name == childName)
            return root;

        foreach (Transform child in root)
        {
            Transform found = FindDeepChild(child, childName);
            if (found != null)
                return found;
        }

        return null;
    }

    private sealed class HoverPanelSettings
    {
        public GameObject ScreenSpacePanelPrefab;
        public string WorldUIRootTag;
        public float ShowDuration;
        public float HideDuration;
        public float HiddenScale;
        public int ShowEaseIndex;
        public int HideEaseIndex;
        public float InRangeAlpha;
        public float OutOfRangeAlpha;
        public Sprite SymbolInRange;
        public Sprite SymbolOutOfRange;
    }
}
