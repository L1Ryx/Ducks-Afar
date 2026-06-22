using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(AlternateWorldGridAutotileUtility))]
public sealed class AlternateWorldGridAutotileUtilityEditor : Editor
{
    private const string AuralisTileFolder = "Assets/Art/Tilemaps/Palettes/Home Tiles";
    private const string SolarisTileFolder = "Assets/Art/Tilemaps/Palettes/Duck Temp Tiles";
    private const string AuralisTilePrefix = "home_tileset";
    private const string SolarisTilePrefix = "TEMP_DUCK_TILESET";
    private const string SourceOceanTilePath = "Assets/Art/Animated Tiles/Water-Home-Tile-Animated.asset";
    private const string TargetOceanTilePath = "Assets/Art/Animated Tiles/Water-Tile-Animated.asset";

    private readonly struct GroundTileMappingIndex
    {
        public readonly int Source;
        public readonly int Target;

        public GroundTileMappingIndex(int source, int target)
        {
            Source = source;
            Target = target;
        }
    }

    private static readonly GroundTileMappingIndex[] DefaultGroundTileMappings =
    {
        new(0, 63),
        new(1, 64),
        new(2, 65),
        new(6, 71),
        new(7, 72),
        new(8, 73),
        new(12, 122),
        new(13, 123),
        new(14, 125),
        new(26, 55),
        new(27, 56),
        new(28, 57),
        new(29, 58),
        new(30, 59),
        new(31, 60),
        new(32, 61),
        new(33, 62),
        new(34, 66),
        new(35, 67),
        new(36, 68),
        new(37, 69),
        new(38, 74),
        new(39, 75),
        new(40, 76),
        new(41, 77),
    };

    private static readonly string[] DefaultBeigeFlowerPrefabPaths =
    {
        "Assets/Prefabs/Environment/Beige-Flower-1.prefab",
        "Assets/Prefabs/Environment/Beige-Flower-2.prefab",
        "Assets/Prefabs/Environment/Beige-Flower-3.prefab",
    };

    private static readonly string[] DefaultYellowFlowerPrefabPaths =
    {
        "Assets/Prefabs/Environment/Yellow-Flower-1.prefab",
        "Assets/Prefabs/Environment/Yellow-Flower-2.prefab",
        "Assets/Prefabs/Environment/Yellow-Flower-3.prefab",
    };

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var utility = (AlternateWorldGridAutotileUtility)target;

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("Editor Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Resolve Scene References"))
            ResolveReferences(utility, markDirty: true);

        if (GUILayout.Button("Load Default Solaris Mappings"))
            LoadDefaultMappings(utility);

        EditorGUILayout.Space(4f);

        if (GUILayout.Button("Build Ground Layer"))
            RunWithUndo("Build Alternate Ground Layer", () => BuildGround(utility));

        if (GUILayout.Button("Build Ocean Layer"))
            RunWithUndo("Build Alternate Ocean Layer", () => BuildOcean(utility));

        if (GUILayout.Button("Build Shoreline Layer"))
            RunWithUndo("Build Alternate Shoreline Layer", () => BuildShoreline(utility));

        if (GUILayout.Button("Copy Beige Flowers As Yellow"))
            RunWithUndo("Copy Alternate Yellow Flowers", () => CopyFlowers(utility));

        EditorGUILayout.Space(4f);

        if (GUILayout.Button("Build All Alternate World Layers"))
            BuildAll(utility);

        EditorGUILayout.HelpBox(
            "Build actions modify only the target world grid/layers and the target flower parent. Source Auralis layers are read-only.",
            MessageType.Info);
    }

    private static void RunWithUndo(string undoName, System.Action action)
    {
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(undoName);
        int undoGroup = Undo.GetCurrentGroup();

        try
        {
            action?.Invoke();
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    private static void ResolveReferences(AlternateWorldGridAutotileUtility utility, bool markDirty)
    {
        if (utility == null)
            return;

        Undo.RecordObject(utility, "Resolve Alternate World Grid Autotiler References");
        utility.ResolveSceneReferences();

        if (markDirty)
        {
            EditorUtility.SetDirty(utility);
            MarkActiveSceneDirty();
        }
    }

    public static void LoadDefaultMappings(AlternateWorldGridAutotileUtility utility)
    {
        if (utility == null)
            return;

        Undo.RecordObject(utility, "Load Alternate World Grid Autotiler Defaults");

        utility.groundTileMappings.Clear();
        for (int i = 0; i < DefaultGroundTileMappings.Length; i++)
        {
            GroundTileMappingIndex mapping = DefaultGroundTileMappings[i];
            TileBase source = LoadTile(AuralisTileFolder, AuralisTilePrefix, mapping.Source);
            TileBase target = LoadTile(SolarisTileFolder, SolarisTilePrefix, mapping.Target);

            if (source == null || target == null)
            {
                Debug.LogWarning($"AlternateWorldGridAutotileUtility: missing ground mapping tile index {mapping.Source} -> {mapping.Target}. Source={source}, Target={target}");
                continue;
            }

            utility.groundTileMappings.Add(new AlternateWorldGridAutotileUtility.TileMapping
            {
                source = source,
                target = target,
            });
        }

        utility.sourceOceanTile = AssetDatabase.LoadAssetAtPath<TileBase>(SourceOceanTilePath);
        utility.targetOceanTile = AssetDatabase.LoadAssetAtPath<TileBase>(TargetOceanTilePath);

        utility.flowerPrefabMappings.Clear();
        for (int i = 0; i < DefaultBeigeFlowerPrefabPaths.Length; i++)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultBeigeFlowerPrefabPaths[i]);
            GameObject target = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultYellowFlowerPrefabPaths[i]);

            if (source == null || target == null)
            {
                Debug.LogWarning($"AlternateWorldGridAutotileUtility: missing flower mapping {DefaultBeigeFlowerPrefabPaths[i]} -> {DefaultYellowFlowerPrefabPaths[i]}.");
                continue;
            }

            utility.flowerPrefabMappings.Add(new AlternateWorldGridAutotileUtility.PrefabMapping
            {
                source = source,
                target = target,
            });
        }

        utility.targetFlowersParentName = "Yellow Flowers";
        utility.ResolveSceneReferences();
        EditorUtility.SetDirty(utility);
        MarkActiveSceneDirty();

        Debug.Log(
            $"AlternateWorldGridAutotileUtility: loaded {utility.groundTileMappings.Count} ground mapping(s), " +
            $"{utility.flowerPrefabMappings.Count} flower mapping(s), ocean target '{utility.targetOceanTile}'.");
    }

    public static void BuildAll(AlternateWorldGridAutotileUtility utility)
    {
        RunWithUndo("Build Alternate World Grid", () =>
        {
            BuildGround(utility);
            BuildOcean(utility);
            BuildShoreline(utility);
            CopyFlowers(utility);
        });
    }

    private static TileBase LoadTile(string folder, string prefix, int tileIndex)
    {
        return AssetDatabase.LoadAssetAtPath<TileBase>($"{folder}/{prefix}_{tileIndex}.asset");
    }

    private static void BuildGround(AlternateWorldGridAutotileUtility utility)
    {
        ResolveReferences(utility, markDirty: false);

        if (!EnsureSourceTilemap(utility.sourceGround, utility.groundLayerName))
            return;

        Tilemap targetGround = EnsureTargetLayer(utility, utility.sourceGround, utility.groundLayerName);
        if (targetGround == null)
            return;

        Dictionary<TileBase, TileBase> mappings = BuildTileMap(utility.groundTileMappings);
        if (mappings.Count == 0)
        {
            Debug.LogError("AlternateWorldGridAutotileUtility: ground tile mappings are empty. Use 'Load Default Solaris Mappings' first.");
            return;
        }

        if (utility.clearTargetLayersBeforeBuild)
            ClearTilemap(targetGround);

        int written = 0;
        int unmapped = 0;
        foreach (Vector3Int position in utility.sourceGround.cellBounds.allPositionsWithin)
        {
            TileBase sourceTile = utility.sourceGround.GetTile(position);
            if (sourceTile == null)
                continue;

            if (!mappings.TryGetValue(sourceTile, out TileBase targetTile) || targetTile == null)
            {
                unmapped++;
                continue;
            }

            CopyCell(utility.sourceGround, targetGround, position, targetTile);
            written++;
        }

        FinishTilemap(targetGround);
        MarkActiveSceneDirty();

        Debug.Log($"AlternateWorldGridAutotileUtility: wrote {written} Solaris ground tile(s). Unmapped source tile cell(s): {unmapped}.");
    }

    private static void BuildOcean(AlternateWorldGridAutotileUtility utility)
    {
        ResolveReferences(utility, markDirty: false);

        if (!EnsureSourceTilemap(utility.sourceOcean, utility.oceanLayerName))
            return;

        if (utility.targetOceanTile == null)
        {
            Debug.LogError("AlternateWorldGridAutotileUtility: Target Ocean Tile is missing. Use 'Load Default Solaris Mappings' first or assign one.");
            return;
        }

        Tilemap targetOcean = EnsureTargetLayer(utility, utility.sourceOcean, utility.oceanLayerName);
        if (targetOcean == null)
            return;

        if (utility.clearTargetLayersBeforeBuild)
            ClearTilemap(targetOcean);

        int written = 0;
        int skipped = 0;
        foreach (Vector3Int position in utility.sourceOcean.cellBounds.allPositionsWithin)
        {
            TileBase sourceTile = utility.sourceOcean.GetTile(position);
            if (sourceTile == null)
                continue;

            if (utility.sourceOceanTile != null && sourceTile != utility.sourceOceanTile)
            {
                skipped++;
                continue;
            }

            CopyCell(utility.sourceOcean, targetOcean, position, utility.targetOceanTile);
            written++;
        }

        FinishTilemap(targetOcean);
        MarkActiveSceneDirty();

        Debug.Log($"AlternateWorldGridAutotileUtility: wrote {written} Solaris ocean tile(s). Skipped {skipped} non-matching source ocean tile(s).");
    }

    private static void BuildShoreline(AlternateWorldGridAutotileUtility utility)
    {
        ResolveReferences(utility, markDirty: false);

        if (!EnsureSourceTilemap(utility.sourceShoreline, utility.shorelineLayerName))
            return;

        Tilemap targetShoreline = EnsureTargetLayer(utility, utility.sourceShoreline, utility.shorelineLayerName);
        if (targetShoreline == null)
            return;

        if (utility.clearTargetLayersBeforeBuild)
            ClearTilemap(targetShoreline);

        int written = CopyTilemapOneToOne(utility.sourceShoreline, targetShoreline);
        FinishTilemap(targetShoreline);
        MarkActiveSceneDirty();

        Debug.Log($"AlternateWorldGridAutotileUtility: copied {written} shoreline tile(s) 1:1.");
    }

    private static void CopyFlowers(AlternateWorldGridAutotileUtility utility)
    {
        ResolveReferences(utility, markDirty: false);

        if (utility.sourceFlowersParent == null)
        {
            Debug.LogError($"AlternateWorldGridAutotileUtility: source flowers parent '{utility.sourceFlowersParentName}' was not found.");
            return;
        }

        Transform targetRoot = EnsureTargetWorldGrid(utility);
        if (targetRoot == null)
            return;

        Transform targetParent = EnsureTargetFlowersParent(utility, targetRoot);
        if (targetParent == null)
            return;

        if (utility.clearTargetFlowersBeforeCopy)
            ClearChildren(targetParent);

        int copied = 0;
        int unmapped = 0;
        Transform[] sourceTransforms = utility.sourceFlowersParent.GetComponentsInChildren<Transform>(utility.includeInactiveFlowers);
        for (int i = 0; i < sourceTransforms.Length; i++)
        {
            Transform source = sourceTransforms[i];
            if (source == utility.sourceFlowersParent)
                continue;

            if (!TryGetTargetFlowerPrefab(utility, source.gameObject, out GameObject targetPrefab))
            {
                if (source.name.StartsWith("Beige-Flower"))
                    unmapped++;
                continue;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(targetPrefab, source.gameObject.scene);
            if (instance == null)
                continue;

            Undo.RegisterCreatedObjectUndo(instance, "Create Alternate Yellow Flower");
            Undo.SetTransformParent(instance.transform, targetParent, "Parent Alternate Yellow Flower");

            instance.name = ReplaceFlowerName(source.name, targetPrefab.name);
            instance.SetActive(source.gameObject.activeSelf);
            instance.transform.SetPositionAndRotation(source.position, source.rotation);
            instance.transform.localScale = source.localScale;
            copied++;
        }

        EditorUtility.SetDirty(targetParent);
        MarkActiveSceneDirty();

        Debug.Log($"AlternateWorldGridAutotileUtility: copied {copied} beige flower(s) as yellow. Unmapped beige flower object(s): {unmapped}.");
    }

    private static bool EnsureSourceTilemap(Tilemap tilemap, string layerName)
    {
        if (tilemap != null)
            return true;

        Debug.LogError($"AlternateWorldGridAutotileUtility: source tilemap '{layerName}' was not found.");
        return false;
    }

    private static Tilemap EnsureTargetLayer(AlternateWorldGridAutotileUtility utility, Tilemap source, string layerName)
    {
        Transform targetRoot = EnsureTargetWorldGrid(utility);
        if (targetRoot == null)
            return null;

        Tilemap existing = AlternateWorldGridAutotileUtility.FindTilemapUnder(targetRoot, layerName);
        if (existing != null)
        {
            AssignTargetLayerReference(utility, layerName, existing);
            return existing;
        }

        GameObject layerObject = Object.Instantiate(source.gameObject, targetRoot);
        Undo.RegisterCreatedObjectUndo(layerObject, $"Create Alternate {layerName}");
        layerObject.name = layerName;
        layerObject.transform.localPosition = source.transform.localPosition;
        layerObject.transform.localRotation = source.transform.localRotation;
        layerObject.transform.localScale = source.transform.localScale;

        Tilemap target = layerObject.GetComponent<Tilemap>();
        if (target == null)
            target = layerObject.AddComponent<Tilemap>();

        AssignTargetLayerReference(utility, layerName, target);
        return target;
    }

    private static Transform EnsureTargetWorldGrid(AlternateWorldGridAutotileUtility utility)
    {
        if (utility.targetWorldGrid != null)
            return utility.targetWorldGrid;

        Transform existing = AlternateWorldGridAutotileUtility.FindSceneTransform(utility.targetWorldGridName);
        if (existing != null)
        {
            Undo.RecordObject(utility, "Assign Alternate World Grid");
            utility.targetWorldGrid = existing;
            EditorUtility.SetDirty(utility);
            return existing;
        }

        var targetObject = new GameObject(utility.targetWorldGridName);
        Undo.RegisterCreatedObjectUndo(targetObject, "Create Alternate World Grid");

        if (utility.sourceWorldGrid != null)
        {
            targetObject.transform.SetPositionAndRotation(utility.sourceWorldGrid.position, utility.sourceWorldGrid.rotation);
            targetObject.transform.localScale = utility.sourceWorldGrid.localScale;

            Grid sourceGrid = utility.sourceWorldGrid.GetComponent<Grid>();
            if (sourceGrid != null)
            {
                Grid targetGrid = targetObject.AddComponent<Grid>();
                EditorUtility.CopySerialized(sourceGrid, targetGrid);
            }
        }
        else
        {
            targetObject.AddComponent<Grid>();
        }

        targetObject.SetActive(utility.targetWorldGridActiveAfterBuild);
        utility.targetWorldGrid = targetObject.transform;
        EditorUtility.SetDirty(utility);
        return utility.targetWorldGrid;
    }

    private static Transform EnsureTargetFlowersParent(AlternateWorldGridAutotileUtility utility, Transform targetRoot)
    {
        if (utility.targetFlowersParent != null)
            return utility.targetFlowersParent;

        Transform existing = AlternateWorldGridAutotileUtility.FindChildByName(targetRoot, utility.targetFlowersParentName);
        if (existing != null)
        {
            utility.targetFlowersParent = existing;
            EditorUtility.SetDirty(utility);
            return existing;
        }

        var parent = new GameObject(utility.targetFlowersParentName);
        Undo.RegisterCreatedObjectUndo(parent, "Create Alternate Flower Parent");
        Undo.SetTransformParent(parent.transform, targetRoot, "Parent Alternate Flower Parent");
        parent.transform.localPosition = Vector3.zero;
        parent.transform.localRotation = Quaternion.identity;
        parent.transform.localScale = Vector3.one;
        utility.targetFlowersParent = parent.transform;
        EditorUtility.SetDirty(utility);
        return utility.targetFlowersParent;
    }

    private static void AssignTargetLayerReference(AlternateWorldGridAutotileUtility utility, string layerName, Tilemap tilemap)
    {
        Undo.RecordObject(utility, "Assign Alternate Tilemap Reference");

        if (layerName == utility.groundLayerName)
            utility.targetGround = tilemap;
        else if (layerName == utility.oceanLayerName)
            utility.targetOcean = tilemap;
        else if (layerName == utility.shorelineLayerName)
            utility.targetShoreline = tilemap;

        EditorUtility.SetDirty(utility);
    }

    private static Dictionary<TileBase, TileBase> BuildTileMap(List<AlternateWorldGridAutotileUtility.TileMapping> mappings)
    {
        var map = new Dictionary<TileBase, TileBase>();
        if (mappings == null)
            return map;

        for (int i = 0; i < mappings.Count; i++)
        {
            var mapping = mappings[i];
            if (mapping?.source == null || mapping.target == null)
                continue;

            map[mapping.source] = mapping.target;
        }

        return map;
    }

    private static int CopyTilemapOneToOne(Tilemap source, Tilemap target)
    {
        int written = 0;
        foreach (Vector3Int position in source.cellBounds.allPositionsWithin)
        {
            TileBase sourceTile = source.GetTile(position);
            if (sourceTile == null)
                continue;

            CopyCell(source, target, position, sourceTile);
            written++;
        }

        return written;
    }

    private static void CopyCell(Tilemap source, Tilemap target, Vector3Int position, TileBase targetTile)
    {
        target.SetTile(position, targetTile);
        target.SetTileFlags(position, TileFlags.None);
        target.SetTransformMatrix(position, source.GetTransformMatrix(position));
        target.SetColor(position, source.GetColor(position));
        target.SetTileFlags(position, source.GetTileFlags(position));
    }

    private static void ClearTilemap(Tilemap tilemap)
    {
        Undo.RegisterCompleteObjectUndo(tilemap, $"Clear {tilemap.name}");
        tilemap.ClearAllTiles();
        EditorUtility.SetDirty(tilemap);
    }

    private static void FinishTilemap(Tilemap tilemap)
    {
        tilemap.CompressBounds();
        EditorUtility.SetDirty(tilemap);
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
    }

    private static bool TryGetTargetFlowerPrefab(AlternateWorldGridAutotileUtility utility, GameObject sourceObject, out GameObject targetPrefab)
    {
        targetPrefab = null;

        if (utility.flowerPrefabMappings == null)
            return false;

        GameObject sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(sourceObject);
        for (int i = 0; i < utility.flowerPrefabMappings.Count; i++)
        {
            var mapping = utility.flowerPrefabMappings[i];
            if (mapping?.source == null || mapping.target == null)
                continue;

            if (sourcePrefab == mapping.source || sourceObject.name.StartsWith(mapping.source.name))
            {
                targetPrefab = mapping.target;
                return true;
            }
        }

        return false;
    }

    private static string ReplaceFlowerName(string sourceName, string targetPrefabName)
    {
        if (sourceName.StartsWith("Beige-Flower-1"))
            return sourceName.Replace("Beige-Flower-1", targetPrefabName);

        if (sourceName.StartsWith("Beige-Flower-2"))
            return sourceName.Replace("Beige-Flower-2", targetPrefabName);

        if (sourceName.StartsWith("Beige-Flower-3"))
            return sourceName.Replace("Beige-Flower-3", targetPrefabName);

        return targetPrefabName;
    }

    private static void MarkActiveSceneDirty()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
            EditorSceneManager.MarkSceneDirty(scene);
    }
}
