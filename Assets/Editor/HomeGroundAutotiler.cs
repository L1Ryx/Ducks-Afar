using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class HomeGroundAutotiler
{
    private const string StageHomeScenePath = "Assets/Scenes/Live/Stage Home.unity";
    private const string SettingsAssetPath = "Assets/Editor/HomeStageAutotilerSettings.asset";
    private const string GroundTilemapName = "Ground (NC)";
    private const string OceanTilemapName = "Ocean (NC)";
    private const string ShorelineTilemapName = "Shoreline (C)";
    private const string FlowersParentName = "Flowers";
    private const string GeneratedFlowersParentName = "Generated Beige Flowers";
    private const string HomeTileFolder = "Assets/Art/Tilemaps/Palettes/Home Tiles";
    private const int CenterTileIndex = 7;
    private const int ShorelineFillTileIndex = 10;

    private static readonly string[] DefaultBeigeFlowerPrefabPaths =
    {
        "Assets/Prefabs/Environment/Beige-Flower-1.prefab",
        "Assets/Prefabs/Environment/Beige-Flower-2.prefab",
        "Assets/Prefabs/Environment/Beige-Flower-3.prefab",
    };

    private static readonly int[] RequiredTileIndices =
    {
        0, 1, 2,
        6, 7, 8, 10,
        12, 13, 14,
        26, 27, 28, 29,
        30, 31, 32, 33,
        34, 35, 36, 37,
        38, 39, 40, 41,
    };

    private static readonly Vector3Int[] NeighborOffsets =
    {
        new Vector3Int(-1, -1, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(1, -1, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 1, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(1, 1, 0),
    };

    [MenuItem("Ducks Afar/Tilemaps/Autotile Stage Home #p")]
    public static void AutotileStageHome()
    {
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Autotile Stage Home");
        int undoGroup = Undo.GetCurrentGroup();

        try
        {
            AutotileStageHomeInternal();
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    [MenuItem("Ducks Afar/Tilemaps/Select Stage Home Autotiler Settings")]
    public static void SelectStageHomeAutotilerSettings()
    {
        Selection.activeObject = LoadOrCreateSettings();
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    [MenuItem("Ducks Afar/Tilemaps/Clear Generated Beige Flowers")]
    public static void ClearGeneratedBeigeFlowers()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != StageHomeScenePath)
        {
            Debug.LogWarning($"HomeGroundAutotiler: open {StageHomeScenePath} before clearing generated flowers.");
            return;
        }

        Transform flowersParent = FindSceneTransform(activeScene, FlowersParentName);
        Transform generatedParent = flowersParent == null ? null : FindChildByName(flowersParent, GeneratedFlowersParentName);
        if (generatedParent == null)
        {
            Debug.Log("HomeGroundAutotiler: no generated Beige Flowers parent found.");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Clear Generated Beige Flowers");
        int undoGroup = Undo.GetCurrentGroup();
        int removed = ClearChildren(generatedParent);
        EditorSceneManager.MarkSceneDirty(activeScene);
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log($"HomeGroundAutotiler: removed {removed} generated Beige Flower object(s).");
    }

    [MenuItem("Ducks Afar/Tilemaps/Autotile Stage Home #p", true)]
    private static bool CanAutotileStageHome()
    {
        return SceneManager.GetActiveScene().path == StageHomeScenePath;
    }

    private static void AutotileStageHomeInternal()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != StageHomeScenePath)
        {
            Debug.LogWarning($"HomeGroundAutotiler: open {StageHomeScenePath} before autotiling. No tiles changed.");
            return;
        }

        HomeStageAutotilerSettings settings = LoadOrCreateSettings();
        Tilemap ground = FindTilemap(activeScene, GroundTilemapName);
        if (ground == null)
        {
            Debug.LogError($"HomeGroundAutotiler: could not find tilemap '{GroundTilemapName}' in {StageHomeScenePath}.");
            return;
        }

        Tilemap ocean = FindTilemap(activeScene, OceanTilemapName);
        if (ocean == null)
        {
            Debug.LogError($"HomeGroundAutotiler: could not find tilemap '{OceanTilemapName}' in {StageHomeScenePath}.");
            return;
        }

        Tilemap shoreline = FindTilemap(activeScene, ShorelineTilemapName);
        if (shoreline == null)
        {
            Debug.LogError($"HomeGroundAutotiler: could not find tilemap '{ShorelineTilemapName}' in {StageHomeScenePath}.");
            return;
        }

        Dictionary<int, TileBase> tiles = LoadRequiredTiles();
        if (tiles == null)
            return;

        int groundReplacements = AutotileGround(ground, tiles, out int centerTilesKept);
        ShorelineSyncResult shorelineResult = SyncShoreline(ground, ocean, shoreline, tiles[ShorelineFillTileIndex]);
        FlowerPlacementResult flowerResult = RegenerateBeigeFlowers(activeScene, ground, settings);

        if (groundReplacements == 0 && shorelineResult.Added == 0 && shorelineResult.Removed == 0 && flowerResult.Created == 0 && flowerResult.Removed == 0)
        {
            Debug.Log($"HomeGroundAutotiler: no Stage Home tile changes needed. Kept {centerTilesKept} ground center tile(s); {shorelineResult.Kept} shoreline tile(s) already matched.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(activeScene);

        Debug.Log(
            $"HomeGroundAutotiler: replaced {groundReplacements} ground center tile(s), kept {centerTilesKept} ground center tile(s), " +
            $"added {shorelineResult.Added} shoreline tile(s), removed {shorelineResult.Removed} stale shoreline tile(s), kept {shorelineResult.Kept} shoreline tile(s), " +
            $"generated {flowerResult.Created} Beige Flower object(s), removed {flowerResult.Removed} previous generated flower(s).");
    }

    private static int AutotileGround(Tilemap ground, Dictionary<int, TileBase> tiles, out int centerTilesKept)
    {
        TileBase centerTile = tiles[CenterTileIndex];
        var replacements = new List<TileReplacement>();
        centerTilesKept = 0;

        foreach (Vector3Int position in ground.cellBounds.allPositionsWithin)
        {
            TileBase currentTile = ground.GetTile(position);
            if (currentTile != centerTile)
                continue;

            int targetIndex = PickTargetTileIndex(ground, position);
            if (targetIndex == CenterTileIndex)
            {
                centerTilesKept++;
                continue;
            }

            replacements.Add(new TileReplacement(position, tiles[targetIndex]));
        }

        if (replacements.Count == 0)
            return 0;

        Undo.RegisterCompleteObjectUndo(ground, "Autotile Stage Home Ground");

        for (int i = 0; i < replacements.Count; i++)
            ground.SetTile(replacements[i].Position, replacements[i].Tile);

        EditorUtility.SetDirty(ground);
        return replacements.Count;
    }

    private static ShorelineSyncResult SyncShoreline(Tilemap ground, Tilemap ocean, Tilemap shoreline, TileBase shorelineTile)
    {
        var expectedPositions = new HashSet<Vector3Int>();
        foreach (Vector3Int groundPosition in ground.cellBounds.allPositionsWithin)
        {
            if (!ground.HasTile(groundPosition))
                continue;

            for (int i = 0; i < NeighborOffsets.Length; i++)
            {
                Vector3Int shorelinePosition = groundPosition + NeighborOffsets[i];
                if (!ground.HasTile(shorelinePosition) && ocean.HasTile(shorelinePosition))
                    expectedPositions.Add(shorelinePosition);
            }
        }

        var stalePositions = new List<Vector3Int>();
        foreach (Vector3Int position in shoreline.cellBounds.allPositionsWithin)
        {
            if (shoreline.HasTile(position) && !expectedPositions.Contains(position))
                stalePositions.Add(position);
        }

        int added = 0;
        int kept = 0;
        foreach (Vector3Int position in expectedPositions)
        {
            if (shoreline.HasTile(position))
            {
                kept++;
                continue;
            }

            added++;
        }

        if (added == 0 && stalePositions.Count == 0)
            return new ShorelineSyncResult(0, 0, kept);

        Undo.RegisterCompleteObjectUndo(shoreline, "Sync Stage Home Shoreline");

        foreach (Vector3Int position in expectedPositions)
        {
            if (!shoreline.HasTile(position))
                shoreline.SetTile(position, shorelineTile);
        }

        for (int i = 0; i < stalePositions.Count; i++)
            shoreline.SetTile(stalePositions[i], null);

        EditorUtility.SetDirty(shoreline);
        return new ShorelineSyncResult(added, stalePositions.Count, kept);
    }

    private static FlowerPlacementResult RegenerateBeigeFlowers(Scene scene, Tilemap ground, HomeStageAutotilerSettings settings)
    {
        GameObject[] flowerPrefabs = LoadBeigeFlowerPrefabs(settings);
        if (flowerPrefabs.Length == 0)
        {
            Debug.LogError("HomeGroundAutotiler: no Beige Flower prefabs are available for generation.");
            return new FlowerPlacementResult(0, 0);
        }

        Transform flowersParent = FindOrCreateFlowersParent(scene);
        Transform generatedParent = FindOrCreateGeneratedFlowersParent(scene, flowersParent);

        List<Vector3> occupiedPositions = CollectExistingBeigeFlowerPositions(scene, generatedParent);
        List<Bounds> avoidBounds = settings.AvoidSceneSpriteBounds
            ? CollectSceneSpriteAvoidanceBounds(scene, generatedParent, settings.ObjectAvoidancePadding)
            : new List<Bounds>();

        List<FlowerCandidate> candidates = BuildFlowerCandidates(ground, settings, flowerPrefabs.Length, occupiedPositions, avoidBounds);
        int removed = ClearChildren(generatedParent);
        int created = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            if (settings.MaxGeneratedFlowers > 0 && created >= settings.MaxGeneratedFlowers)
                break;

            FlowerCandidate candidate = candidates[i];
            GameObject prefab = flowerPrefabs[candidate.PrefabIndex];
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            if (instance == null)
                continue;

            Undo.RegisterCreatedObjectUndo(instance, "Create Generated Beige Flower");
            instance.name = $"{prefab.name} - Generated {created + 1:000}";
            Undo.SetTransformParent(instance.transform, generatedParent, "Parent Generated Beige Flower");
            instance.transform.position = candidate.Position;
            created++;
        }

        EditorUtility.SetDirty(generatedParent);
        return new FlowerPlacementResult(created, removed);
    }

    private static List<FlowerCandidate> BuildFlowerCandidates(
        Tilemap ground,
        HomeStageAutotilerSettings settings,
        int prefabCount,
        List<Vector3> occupiedPositions,
        List<Bounds> avoidBounds)
    {
        var candidates = new List<FlowerCandidate>();
        var acceptedPositions = new List<Vector3>(occupiedPositions);
        float minimumDistanceSqr = settings.MinimumDistance * settings.MinimumDistance;
        float placementChance = Mathf.Clamp01(settings.Density / (settings.Spread * settings.Spread));

        foreach (Vector3Int cell in ground.cellBounds.allPositionsWithin)
        {
            if (!ground.HasTile(cell))
                continue;

            float randomGate = Hash01(cell.x, cell.y, settings.Seed, 11);
            float noiseGate = Mathf.PerlinNoise(
                (cell.x + settings.Seed * 0.37f) * settings.NoiseScale,
                (cell.y - settings.Seed * 0.19f) * settings.NoiseScale);
            float gate = Mathf.Lerp(randomGate, noiseGate, settings.NoiseInfluence);
            if (gate > placementChance)
                continue;

            Vector3 center = ground.GetCellCenterWorld(cell);
            float jitterX = (Hash01(cell.x, cell.y, settings.Seed, 23) - 0.5f) * settings.CellJitter * 2f;
            float jitterY = (Hash01(cell.x, cell.y, settings.Seed, 37) - 0.5f) * settings.CellJitter * 2f;
            var position = new Vector3(center.x + jitterX, center.y + jitterY, center.z);

            if (!HasGroundPadding(ground, position, settings.EdgePadding))
                continue;

            if (IsInsideAvoidanceBounds(avoidBounds, position))
                continue;

            if (IsTooClose(position, acceptedPositions, minimumDistanceSqr))
                continue;

            acceptedPositions.Add(position);
            int prefabIndex = Mathf.FloorToInt(Hash01(cell.x, cell.y, settings.Seed, 53) * prefabCount);
            if (prefabIndex >= prefabCount)
                prefabIndex = prefabCount - 1;

            float order = Hash01(cell.x, cell.y, settings.Seed, 71);
            candidates.Add(new FlowerCandidate(position, prefabIndex, order));
        }

        candidates.Sort((a, b) => a.Order.CompareTo(b.Order));
        return candidates;
    }

    private static bool HasGroundPadding(Tilemap ground, Vector3 position, float padding)
    {
        if (padding <= 0f)
            return ground.HasTile(ground.WorldToCell(position));

        if (!ground.HasTile(ground.WorldToCell(position)))
            return false;

        return ground.HasTile(ground.WorldToCell(position + new Vector3(padding, 0f, 0f)))
            && ground.HasTile(ground.WorldToCell(position + new Vector3(-padding, 0f, 0f)))
            && ground.HasTile(ground.WorldToCell(position + new Vector3(0f, padding, 0f)))
            && ground.HasTile(ground.WorldToCell(position + new Vector3(0f, -padding, 0f)))
            && ground.HasTile(ground.WorldToCell(position + new Vector3(padding, padding, 0f)))
            && ground.HasTile(ground.WorldToCell(position + new Vector3(padding, -padding, 0f)))
            && ground.HasTile(ground.WorldToCell(position + new Vector3(-padding, padding, 0f)))
            && ground.HasTile(ground.WorldToCell(position + new Vector3(-padding, -padding, 0f)));
    }

    private static bool IsTooClose(Vector3 position, List<Vector3> occupiedPositions, float minimumDistanceSqr)
    {
        if (minimumDistanceSqr <= 0f)
            return false;

        for (int i = 0; i < occupiedPositions.Count; i++)
        {
            if (((Vector2)position - (Vector2)occupiedPositions[i]).sqrMagnitude < minimumDistanceSqr)
                return true;
        }

        return false;
    }

    private static bool IsInsideAvoidanceBounds(List<Bounds> avoidBounds, Vector3 position)
    {
        for (int i = 0; i < avoidBounds.Count; i++)
        {
            Bounds bounds = avoidBounds[i];
            if (position.x >= bounds.min.x && position.x <= bounds.max.x && position.y >= bounds.min.y && position.y <= bounds.max.y)
                return true;
        }

        return false;
    }

    private static Transform FindOrCreateFlowersParent(Scene scene)
    {
        Transform existing = FindSceneTransform(scene, FlowersParentName);
        if (existing != null)
            return existing;

        var parent = new GameObject(FlowersParentName);
        SceneManager.MoveGameObjectToScene(parent, scene);
        Undo.RegisterCreatedObjectUndo(parent, "Create Flowers Parent");
        return parent.transform;
    }

    private static Transform FindOrCreateGeneratedFlowersParent(Scene scene, Transform flowersParent)
    {
        Transform existing = FindChildByName(flowersParent, GeneratedFlowersParentName);
        if (existing != null)
            return existing;

        var parent = new GameObject(GeneratedFlowersParentName);
        SceneManager.MoveGameObjectToScene(parent, scene);
        Undo.RegisterCreatedObjectUndo(parent, "Create Generated Beige Flowers Parent");
        Undo.SetTransformParent(parent.transform, flowersParent, "Parent Generated Beige Flowers");
        parent.transform.localPosition = Vector3.zero;
        return parent.transform;
    }

    private static int ClearChildren(Transform parent)
    {
        int count = parent.childCount;
        for (int i = count - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);

        return count;
    }

    private static Transform FindSceneTransform(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(includeInactive: true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == objectName)
                    return transforms[i];
            }
        }

        return null;
    }

    private static Transform FindChildByName(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;
        }

        return null;
    }

    private static List<Vector3> CollectExistingBeigeFlowerPositions(Scene scene, Transform generatedParent)
    {
        var positions = new List<Vector3>();
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform transform = transforms[i];
            if (transform.gameObject.scene != scene)
                continue;

            if (generatedParent != null && transform.IsChildOf(generatedParent))
                continue;

            if (!transform.name.StartsWith("Beige-Flower"))
                continue;

            positions.Add(transform.position);
        }

        return positions;
    }

    private static List<Bounds> CollectSceneSpriteAvoidanceBounds(Scene scene, Transform generatedParent, float padding)
    {
        var bounds = new List<Bounds>();
        SpriteRenderer[] renderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (!renderer.enabled || renderer.gameObject.scene != scene)
                continue;

            if (generatedParent != null && renderer.transform.IsChildOf(generatedParent))
                continue;

            if (renderer.transform.name.StartsWith("Beige-Flower"))
                continue;

            Bounds rendererBounds = renderer.bounds;
            rendererBounds.Expand(padding * 2f);
            bounds.Add(rendererBounds);
        }

        return bounds;
    }

    private static GameObject[] LoadBeigeFlowerPrefabs(HomeStageAutotilerSettings settings)
    {
        GameObject[] configuredPrefabs = settings.BeigeFlowerPrefabs;
        var prefabs = new List<GameObject>();
        for (int i = 0; i < configuredPrefabs.Length; i++)
        {
            if (configuredPrefabs[i] != null)
                prefabs.Add(configuredPrefabs[i]);
        }

        if (prefabs.Count > 0)
            return prefabs.ToArray();

        for (int i = 0; i < DefaultBeigeFlowerPrefabPaths.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultBeigeFlowerPrefabPaths[i]);
            if (prefab != null)
                prefabs.Add(prefab);
        }

        return prefabs.ToArray();
    }

    private static HomeStageAutotilerSettings LoadOrCreateSettings()
    {
        HomeStageAutotilerSettings settings = AssetDatabase.LoadAssetAtPath<HomeStageAutotilerSettings>(SettingsAssetPath);
        if (settings != null)
        {
            settings.EnsureDefaultPrefabs(DefaultBeigeFlowerPrefabPaths);
            return settings;
        }

        settings = ScriptableObject.CreateInstance<HomeStageAutotilerSettings>();
        settings.EnsureDefaultPrefabs(DefaultBeigeFlowerPrefabPaths);
        AssetDatabase.CreateAsset(settings, SettingsAssetPath);
        AssetDatabase.SaveAssets();
        return settings;
    }

    private static Tilemap FindTilemap(Scene scene, string tilemapName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Tilemap[] tilemaps = root.GetComponentsInChildren<Tilemap>(includeInactive: true);
            for (int i = 0; i < tilemaps.Length; i++)
            {
                if (tilemaps[i].name == tilemapName)
                    return tilemaps[i];
            }
        }

        return null;
    }

    private static Dictionary<int, TileBase> LoadRequiredTiles()
    {
        var tiles = new Dictionary<int, TileBase>();
        for (int i = 0; i < RequiredTileIndices.Length; i++)
        {
            int tileIndex = RequiredTileIndices[i];
            string path = $"{HomeTileFolder}/home_tileset_{tileIndex}.asset";
            TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            if (tile == null)
            {
                Debug.LogError($"HomeGroundAutotiler: missing Home Palette tile at {path}.");
                return null;
            }

            tiles.Add(tileIndex, tile);
        }

        return tiles;
    }

    private static int PickTargetTileIndex(Tilemap tilemap, Vector3Int position)
    {
        bool north = HasTile(tilemap, position, 0, 1);
        bool east = HasTile(tilemap, position, 1, 0);
        bool south = HasTile(tilemap, position, 0, -1);
        bool west = HasTile(tilemap, position, -1, 0);
        bool northEast = HasTile(tilemap, position, 1, 1);
        bool southEast = HasTile(tilemap, position, 1, -1);
        bool southWest = HasTile(tilemap, position, -1, -1);
        bool northWest = HasTile(tilemap, position, -1, 1);

        int missingCardinals = CountMissing(north, east, south, west);

        if (missingCardinals == 0)
        {
            return PickSingleMissingCorner(
                !southEast, 31,
                !southWest, 32,
                !northEast, 35,
                !northWest, 36,
                CenterTileIndex);
        }

        if (missingCardinals == 1)
        {
            if (!north)
                return PickSingleMissingCorner(!southEast, 27, !southWest, 28, 1);

            if (!east)
                return PickSingleMissingCorner(!southWest, 33, !northWest, 37, 8);

            if (!south)
                return PickSingleMissingCorner(!northEast, 39, !northWest, 40, 13);

            return PickSingleMissingCorner(!southEast, 30, !northEast, 34, 6);
        }

        if (missingCardinals == 2)
        {
            if (!north && !west && east && south)
                return southEast ? 0 : 26;

            if (!north && !east && west && south)
                return southWest ? 2 : 29;

            if (!south && !west && east && north)
                return northEast ? 12 : 38;

            if (!south && !east && west && north)
                return northWest ? 14 : 41;
        }

        return CenterTileIndex;
    }

    private static bool HasTile(Tilemap tilemap, Vector3Int position, int xOffset, int yOffset)
    {
        return tilemap.HasTile(new Vector3Int(position.x + xOffset, position.y + yOffset, position.z));
    }

    private static int CountMissing(bool north, bool east, bool south, bool west)
    {
        int count = 0;
        if (!north) count++;
        if (!east) count++;
        if (!south) count++;
        if (!west) count++;
        return count;
    }

    private static int PickSingleMissingCorner(
        bool firstMissing, int firstTile,
        bool secondMissing, int secondTile,
        int fallbackTile)
    {
        if (firstMissing == secondMissing)
            return fallbackTile;

        return firstMissing ? firstTile : secondTile;
    }

    private static int PickSingleMissingCorner(
        bool firstMissing, int firstTile,
        bool secondMissing, int secondTile,
        bool thirdMissing, int thirdTile,
        bool fourthMissing, int fourthTile,
        int fallbackTile)
    {
        int missingCount = 0;
        if (firstMissing) missingCount++;
        if (secondMissing) missingCount++;
        if (thirdMissing) missingCount++;
        if (fourthMissing) missingCount++;

        if (missingCount != 1)
            return fallbackTile;

        if (firstMissing) return firstTile;
        if (secondMissing) return secondTile;
        if (thirdMissing) return thirdTile;
        return fourthTile;
    }

    private static float Hash01(int x, int y, int seed, int salt)
    {
        unchecked
        {
            uint hash = (uint)seed;
            hash ^= (uint)(x * 374761393);
            hash = (hash << 13) | (hash >> 19);
            hash ^= (uint)(y * 668265263);
            hash = (hash << 11) | (hash >> 21);
            hash ^= (uint)(salt * 2246822519);
            hash *= 3266489917;
            hash ^= hash >> 16;
            return (hash & 0x00FFFFFF) / 16777216f;
        }
    }

    private readonly struct TileReplacement
    {
        public readonly Vector3Int Position;
        public readonly TileBase Tile;

        public TileReplacement(Vector3Int position, TileBase tile)
        {
            Position = position;
            Tile = tile;
        }
    }

    private readonly struct ShorelineSyncResult
    {
        public readonly int Added;
        public readonly int Removed;
        public readonly int Kept;

        public ShorelineSyncResult(int added, int removed, int kept)
        {
            Added = added;
            Removed = removed;
            Kept = kept;
        }
    }

    private readonly struct FlowerPlacementResult
    {
        public readonly int Created;
        public readonly int Removed;

        public FlowerPlacementResult(int created, int removed)
        {
            Created = created;
            Removed = removed;
        }
    }

    private readonly struct FlowerCandidate
    {
        public readonly Vector3 Position;
        public readonly int PrefabIndex;
        public readonly float Order;

        public FlowerCandidate(Vector3 position, int prefabIndex, float order)
        {
            Position = position;
            PrefabIndex = prefabIndex;
            Order = order;
        }
    }
}
