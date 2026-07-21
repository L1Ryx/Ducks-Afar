using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class JumanisGroundAutotiler
{
    private const string JumanisScenePath = "Assets/Scenes/Live/Jumanis-1.unity";
    private const string SettingsAssetPath = "Assets/Editor/JumanisStageAutotilerSettings.asset";
    private const string GroundTilemapName = "Ground (NC)";
    private const string DirtTilemapName = "Dirt (NC)";
    private const string OceanTilemapName = "Ocean (NC)";
    private const string ShorelineTilemapName = "Shoreline (C)";
    private const string TileFolder = "Assets/Art/Tilemaps/Palettes/Jumanis Tiles";
    private const string TilePrefix = "Jumanis-Tileset";
    private const string DecorationsParentName = "Decorations";
    private const string GeneratedFlowersParentName = "Generated Jumanis Flowers";
    private const string GeneratedGrassSpotsParentName = "Generated Jumanis Grass Spots";
    private const string GeneratedSandPatchesParentName = "Generated Jumanis Sand Patches";
    private const int CenterTileIndex = 7;
    private const int SandCenterTileIndex = 10;
    private const int ShorelineFillTileIndex = SandCenterTileIndex;

    private static readonly string[] DefaultJumanisFlowerPrefabPaths =
    {
        "Assets/Prefabs/Environment/Jumanis-Flower-1.prefab",
        "Assets/Prefabs/Environment/Jumanis-Flower-2.prefab",
        "Assets/Prefabs/Environment/Jumanis-Flower-3.prefab",
    };

    private static readonly string[] DefaultJumanisGrassSpotPrefabPaths =
    {
        "Assets/Prefabs/Environment/Jumanis-Grass-1.prefab",
        "Assets/Prefabs/Environment/Jumanis-Grass-2.prefab",
        "Assets/Prefabs/Environment/Jumanis-Grass-3.prefab",
    };

    private static readonly string[] DefaultJumanisSandPatchPrefabPaths =
    {
        "Assets/Prefabs/Environment/Jumanis-Sand-1.prefab",
        "Assets/Prefabs/Environment/Jumanis-Sand-2.prefab",
        "Assets/Prefabs/Environment/Jumanis-Sand-3.prefab",
    };

    private static readonly int[] GrassTileIndices =
    {
        0, 1, 2,
        6, 7, 8,
        12, 13, 14,
        22, 23, 24, 25,
        26, 27, 28, 29,
        30, 31, 32, 33,
        34, 35, 36, 37,
    };

    private static readonly int[] SandTileIndices =
    {
        3, 4, 5,
        9, 10, 11,
        15, 16, 17,
        18, 19,
        20, 21,
    };

    private static readonly int[] RequiredTileIndices =
    {
        0, 1, 2,
        3, 4, 5,
        6, 7, 8,
        9, 10, 11,
        12, 13, 14,
        15, 16, 17,
        18, 19, 20, 21,
        22, 23, 24, 25,
        26, 27, 28, 29,
        30, 31, 32, 33,
        34, 35, 36, 37,
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

    [MenuItem("Ducks Afar/Tilemaps/Autotile Jumanis #p")]
    public static void AutotileJumanis()
    {
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Autotile Jumanis");
        int undoGroup = Undo.GetCurrentGroup();

        try
        {
            AutotileJumanisInternal();
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    [MenuItem("Ducks Afar/Tilemaps/Select Jumanis Autotiler Settings")]
    public static void SelectJumanisAutotilerSettings()
    {
        Selection.activeObject = LoadOrCreateSettings();
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    [MenuItem("Ducks Afar/Tilemaps/Clear Generated Jumanis Decorations")]
    public static void ClearGeneratedJumanisDecorations()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != JumanisScenePath)
        {
            Debug.LogWarning($"JumanisGroundAutotiler: open {JumanisScenePath} before clearing generated decorations.");
            return;
        }

        Transform decorationsParent = FindSceneTransform(activeScene, DecorationsParentName);
        if (decorationsParent == null)
        {
            Debug.Log("JumanisGroundAutotiler: no generated Jumanis decorations parent found.");
            return;
        }

        int removed = 0;
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Clear Generated Jumanis Decorations");
        int undoGroup = Undo.GetCurrentGroup();

        Transform flowersParent = FindChildByName(decorationsParent, GeneratedFlowersParentName);
        Transform grassParent = FindChildByName(decorationsParent, GeneratedGrassSpotsParentName);
        Transform sandParent = FindChildByName(decorationsParent, GeneratedSandPatchesParentName);
        if (flowersParent != null)
            removed += ClearChildren(flowersParent);
        if (grassParent != null)
            removed += ClearChildren(grassParent);
        if (sandParent != null)
            removed += ClearChildren(sandParent);

        EditorSceneManager.MarkSceneDirty(activeScene);
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log($"JumanisGroundAutotiler: removed {removed} generated decoration object(s).");
    }

    [MenuItem("Ducks Afar/Tilemaps/Autotile Jumanis #p", true)]
    private static bool CanAutotileJumanis()
    {
        return SceneManager.GetActiveScene().path == JumanisScenePath;
    }

    private static void AutotileJumanisInternal()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != JumanisScenePath)
        {
            Debug.LogWarning($"JumanisGroundAutotiler: open {JumanisScenePath} before autotiling. No tiles changed.");
            return;
        }

        JumanisStageAutotilerSettings settings = LoadOrCreateSettings();
        Tilemap ground = FindTilemap(activeScene, GroundTilemapName);
        if (ground == null)
        {
            Debug.LogError($"JumanisGroundAutotiler: could not find tilemap '{GroundTilemapName}' in {JumanisScenePath}.");
            return;
        }

        Tilemap dirt = FindTilemap(activeScene, DirtTilemapName);
        if (dirt == null)
        {
            Debug.LogError($"JumanisGroundAutotiler: could not find tilemap '{DirtTilemapName}' in {JumanisScenePath}.");
            return;
        }

        Tilemap ocean = FindTilemap(activeScene, OceanTilemapName);
        if (ocean == null)
        {
            Debug.LogError($"JumanisGroundAutotiler: could not find tilemap '{OceanTilemapName}' in {JumanisScenePath}.");
            return;
        }

        Tilemap shoreline = FindTilemap(activeScene, ShorelineTilemapName);
        if (shoreline == null)
        {
            Debug.LogError($"JumanisGroundAutotiler: could not find tilemap '{ShorelineTilemapName}' in {JumanisScenePath}.");
            return;
        }

        Dictionary<int, TileBase> tiles = LoadRequiredTiles();
        if (tiles == null)
            return;

        HashSet<TileBase> grassTiles = BuildTileSet(tiles, GrassTileIndices);
        HashSet<TileBase> sandTiles = BuildTileSet(tiles, SandTileIndices);

        int migratedSandTiles = MigrateSandTilesToDirt(ground, dirt, tiles, sandTiles);
        int grassReplacements = AutotileGrass(ground, tiles, grassTiles, out int grassCenterTilesKept);
        int sandReplacements = AutotileSand(dirt, tiles, sandTiles, out int sandCenterTilesKept);
        ShorelineSyncResult shorelineResult = SyncShoreline(ground, ocean, shoreline, tiles[ShorelineFillTileIndex]);
        DecorationPlacementResult flowerResult = RegenerateDecorations(
            activeScene,
            ground,
            dirt,
            sandTiles,
            settings.Flowers,
            LoadPrefabs(settings.Flowers, DefaultJumanisFlowerPrefabPaths),
            GeneratedFlowersParentName,
            "Jumanis-Flower",
            "Create Generated Jumanis Flower",
            new List<Vector3>(),
            grassTiles);

        List<Vector3> flowerPositions = CollectDecorationPositions(activeScene, "Jumanis-Flower", null);
        DecorationPlacementResult grassResult = RegenerateDecorations(
            activeScene,
            ground,
            dirt,
            sandTiles,
            settings.GrassSpots,
            LoadPrefabs(settings.GrassSpots, DefaultJumanisGrassSpotPrefabPaths),
            GeneratedGrassSpotsParentName,
            "Jumanis-Grass",
            "Create Generated Jumanis Grass Spot",
            flowerPositions,
            grassTiles);

        DecorationPlacementResult sandResult = RegenerateDecorations(
            activeScene,
            dirt,
            null,
            null,
            settings.SandPatches,
            LoadPrefabs(settings.SandPatches, DefaultJumanisSandPatchPrefabPaths),
            GeneratedSandPatchesParentName,
            "Jumanis-Sand",
            "Create Generated Jumanis Sand Patch",
            new List<Vector3>(),
            sandTiles);

        if (migratedSandTiles == 0 && grassReplacements == 0 && sandReplacements == 0 && shorelineResult.Added == 0 && shorelineResult.Removed == 0 && flowerResult.Created == 0 && flowerResult.Removed == 0 && grassResult.Created == 0 && grassResult.Removed == 0 && sandResult.Created == 0 && sandResult.Removed == 0)
        {
            Debug.Log($"JumanisGroundAutotiler: no Jumanis tile changes needed. Kept {grassCenterTilesKept} grass center tile(s), {sandCenterTilesKept} sand center tile(s), and {shorelineResult.Kept} shoreline tile(s).");
            return;
        }

        EditorSceneManager.MarkSceneDirty(activeScene);

        Debug.Log(
            $"JumanisGroundAutotiler: replaced {grassReplacements} grass center tile(s), kept {grassCenterTilesKept} grass center tile(s), " +
            $"migrated {migratedSandTiles} sand tile(s) to Dirt (NC), replaced {sandReplacements} sand center tile(s), kept {sandCenterTilesKept} sand center tile(s), " +
            $"added {shorelineResult.Added} shoreline tile(s), removed {shorelineResult.Removed} stale shoreline tile(s), kept {shorelineResult.Kept} shoreline tile(s), " +
            $"generated {flowerResult.Created} Jumanis Flower object(s), removed {flowerResult.Removed} previous generated flower(s), " +
            $"generated {grassResult.Created} Jumanis Grass Spot object(s), removed {grassResult.Removed} previous generated grass spot(s), " +
            $"generated {sandResult.Created} Jumanis Sand Patch object(s), removed {sandResult.Removed} previous generated sand patch(es).");
    }

    private static int AutotileGrass(Tilemap ground, Dictionary<int, TileBase> tiles, HashSet<TileBase> grassTiles, out int centerTilesKept)
    {
        TileBase centerTile = tiles[CenterTileIndex];
        var replacements = new List<TileReplacement>();
        centerTilesKept = 0;

        foreach (Vector3Int position in ground.cellBounds.allPositionsWithin)
        {
            TileBase currentTile = ground.GetTile(position);
            if (currentTile != centerTile)
                continue;

            int targetIndex = PickGrassTargetTileIndex(ground, position, grassTiles);
            if (tiles[targetIndex] == currentTile)
            {
                centerTilesKept++;
                continue;
            }

            replacements.Add(new TileReplacement(position, tiles[targetIndex]));
        }

        if (replacements.Count == 0)
            return 0;

        Undo.RegisterCompleteObjectUndo(ground, "Autotile Jumanis Ground");

        for (int i = 0; i < replacements.Count; i++)
            ground.SetTile(replacements[i].Position, replacements[i].Tile);

        EditorUtility.SetDirty(ground);
        return replacements.Count;
    }

    private static int MigrateSandTilesToDirt(Tilemap ground, Tilemap dirt, Dictionary<int, TileBase> tiles, HashSet<TileBase> sandTiles)
    {
        var migrations = new List<TileReplacement>();
        foreach (Vector3Int position in ground.cellBounds.allPositionsWithin)
        {
            TileBase currentTile = ground.GetTile(position);
            if (currentTile == null || !sandTiles.Contains(currentTile))
                continue;

            migrations.Add(new TileReplacement(position, currentTile));
        }

        if (migrations.Count == 0)
            return 0;

        Undo.RegisterCompleteObjectUndo(ground, "Move Jumanis Sand Off Ground");
        Undo.RegisterCompleteObjectUndo(dirt, "Move Jumanis Sand To Dirt");

        TileBase grassCenterTile = tiles[CenterTileIndex];
        for (int i = 0; i < migrations.Count; i++)
        {
            Vector3Int position = migrations[i].Position;
            if (!dirt.HasTile(position))
                dirt.SetTile(position, migrations[i].Tile);

            ground.SetTile(position, grassCenterTile);
        }

        EditorUtility.SetDirty(ground);
        EditorUtility.SetDirty(dirt);
        return migrations.Count;
    }

    private static int AutotileSand(Tilemap dirt, Dictionary<int, TileBase> tiles, HashSet<TileBase> sandTiles, out int centerTilesKept)
    {
        TileBase sandCenterTile = tiles[SandCenterTileIndex];
        var replacements = new List<TileReplacement>();
        centerTilesKept = 0;

        foreach (Vector3Int position in dirt.cellBounds.allPositionsWithin)
        {
            TileBase currentTile = dirt.GetTile(position);
            if (currentTile != sandCenterTile)
                continue;

            int targetIndex = PickSandTargetTileIndex(dirt, position, sandTiles);
            if (tiles[targetIndex] == currentTile)
            {
                centerTilesKept++;
                continue;
            }

            replacements.Add(new TileReplacement(position, tiles[targetIndex]));
        }

        if (replacements.Count == 0)
            return 0;

        Undo.RegisterCompleteObjectUndo(dirt, "Autotile Jumanis Sand");

        for (int i = 0; i < replacements.Count; i++)
            dirt.SetTile(replacements[i].Position, replacements[i].Tile);

        EditorUtility.SetDirty(dirt);
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

        Undo.RegisterCompleteObjectUndo(shoreline, "Sync Jumanis Shoreline");

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

    private static DecorationPlacementResult RegenerateDecorations(
        Scene scene,
        Tilemap tilemap,
        Tilemap blockedTilemap,
        HashSet<TileBase> blockedTiles,
        JumanisStageAutotilerSettings.DecorationSettings settings,
        GameObject[] prefabs,
        string generatedParentName,
        string decorationNamePrefix,
        string createUndoName,
        List<Vector3> occupiedPositions,
        HashSet<TileBase> allowedTiles)
    {
        if (prefabs.Length == 0)
        {
            Debug.LogError($"JumanisGroundAutotiler: no prefab is available for '{generatedParentName}'.");
            return new DecorationPlacementResult(0, 0);
        }

        Transform decorationsParent = FindOrCreateDecorationsParent(scene);
        Transform generatedParent = FindOrCreateGeneratedParent(scene, decorationsParent, generatedParentName);
        List<Vector3> occupied = new(occupiedPositions);
        occupied.AddRange(CollectDecorationPositions(scene, decorationNamePrefix, generatedParent));

        List<Bounds> avoidBounds = settings.AvoidSceneSpriteBounds
            ? CollectSceneSpriteAvoidanceBounds(scene, generatedParent, settings.ObjectAvoidancePadding)
            : new List<Bounds>();

        List<DecorationCandidate> candidates = BuildDecorationCandidates(tilemap, blockedTilemap, blockedTiles, settings, prefabs.Length, occupied, avoidBounds, allowedTiles);
        int removed = ClearChildren(generatedParent);
        int created = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            if (settings.MaxGenerated > 0 && created >= settings.MaxGenerated)
                break;

            DecorationCandidate candidate = candidates[i];
            GameObject prefab = prefabs[candidate.PrefabIndex];
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            if (instance == null)
                continue;

            Undo.RegisterCreatedObjectUndo(instance, createUndoName);
            instance.name = $"{prefab.name} - Generated {created + 1:000}";
            Undo.SetTransformParent(instance.transform, generatedParent, $"Parent {generatedParentName}");
            instance.transform.position = candidate.Position;
            created++;
        }

        EditorUtility.SetDirty(generatedParent);
        return new DecorationPlacementResult(created, removed);
    }

    private static List<DecorationCandidate> BuildDecorationCandidates(
        Tilemap tilemap,
        Tilemap blockedTilemap,
        HashSet<TileBase> blockedTiles,
        JumanisStageAutotilerSettings.DecorationSettings settings,
        int prefabCount,
        List<Vector3> occupiedPositions,
        List<Bounds> avoidBounds,
        HashSet<TileBase> allowedTiles)
    {
        var candidates = new List<DecorationCandidate>();
        var acceptedPositions = new List<Vector3>(occupiedPositions);
        float minimumDistanceSqr = settings.MinimumDistance * settings.MinimumDistance;
        float placementChance = Mathf.Clamp01(settings.Density / (settings.Spread * settings.Spread));

        foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
        {
            if (!IsAllowedTile(tilemap.GetTile(cell), allowedTiles))
                continue;

            if (IsBlockedTile(blockedTilemap, blockedTiles, cell))
                continue;

            float randomGate = Hash01(cell.x, cell.y, settings.Seed, 11);
            float noiseGate = Mathf.PerlinNoise(
                (cell.x + settings.Seed * 0.37f) * settings.NoiseScale,
                (cell.y - settings.Seed * 0.19f) * settings.NoiseScale);
            float gate = Mathf.Lerp(randomGate, noiseGate, settings.NoiseInfluence);
            if (gate > placementChance)
                continue;

            Vector3 center = tilemap.GetCellCenterWorld(cell);
            float jitterX = (Hash01(cell.x, cell.y, settings.Seed, 23) - 0.5f) * settings.CellJitter * 2f;
            float jitterY = (Hash01(cell.x, cell.y, settings.Seed, 37) - 0.5f) * settings.CellJitter * 2f;
            var position = new Vector3(center.x + jitterX, center.y + jitterY, center.z);

            if (!HasGroundPadding(tilemap, position, settings.EdgePadding, allowedTiles))
                continue;

            if (HasBlockedPadding(blockedTilemap, blockedTiles, position, 0f))
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
            candidates.Add(new DecorationCandidate(position, prefabIndex, order));
        }

        candidates.Sort((a, b) => a.Order.CompareTo(b.Order));
        return candidates;
    }

    private static bool HasGroundPadding(Tilemap ground, Vector3 position, float padding, HashSet<TileBase> allowedTiles)
    {
        if (padding <= 0f)
            return IsAllowedTile(ground.GetTile(ground.WorldToCell(position)), allowedTiles);

        if (!IsAllowedTile(ground.GetTile(ground.WorldToCell(position)), allowedTiles))
            return false;

        return IsAllowedTile(ground.GetTile(ground.WorldToCell(position + new Vector3(padding, 0f, 0f))), allowedTiles)
            && IsAllowedTile(ground.GetTile(ground.WorldToCell(position + new Vector3(-padding, 0f, 0f))), allowedTiles)
            && IsAllowedTile(ground.GetTile(ground.WorldToCell(position + new Vector3(0f, padding, 0f))), allowedTiles)
            && IsAllowedTile(ground.GetTile(ground.WorldToCell(position + new Vector3(0f, -padding, 0f))), allowedTiles)
            && IsAllowedTile(ground.GetTile(ground.WorldToCell(position + new Vector3(padding, padding, 0f))), allowedTiles)
            && IsAllowedTile(ground.GetTile(ground.WorldToCell(position + new Vector3(padding, -padding, 0f))), allowedTiles)
            && IsAllowedTile(ground.GetTile(ground.WorldToCell(position + new Vector3(-padding, padding, 0f))), allowedTiles)
            && IsAllowedTile(ground.GetTile(ground.WorldToCell(position + new Vector3(-padding, -padding, 0f))), allowedTiles);
    }

    private static bool IsAllowedTile(TileBase tile, HashSet<TileBase> allowedTiles)
    {
        return tile != null && (allowedTiles == null || allowedTiles.Contains(tile));
    }

    private static bool IsBlockedTile(Tilemap tilemap, HashSet<TileBase> blockedTiles, Vector3Int cell)
    {
        return tilemap != null
            && blockedTiles != null
            && blockedTiles.Contains(tilemap.GetTile(cell));
    }

    private static bool HasBlockedPadding(Tilemap tilemap, HashSet<TileBase> blockedTiles, Vector3 position, float padding)
    {
        if (tilemap == null || blockedTiles == null)
            return false;

        if (padding <= 0f)
            return IsBlockedTile(tilemap, blockedTiles, tilemap.WorldToCell(position));

        return IsBlockedTile(tilemap, blockedTiles, tilemap.WorldToCell(position))
            || IsBlockedTile(tilemap, blockedTiles, tilemap.WorldToCell(position + new Vector3(padding, 0f, 0f)))
            || IsBlockedTile(tilemap, blockedTiles, tilemap.WorldToCell(position + new Vector3(-padding, 0f, 0f)))
            || IsBlockedTile(tilemap, blockedTiles, tilemap.WorldToCell(position + new Vector3(0f, padding, 0f)))
            || IsBlockedTile(tilemap, blockedTiles, tilemap.WorldToCell(position + new Vector3(0f, -padding, 0f)))
            || IsBlockedTile(tilemap, blockedTiles, tilemap.WorldToCell(position + new Vector3(padding, padding, 0f)))
            || IsBlockedTile(tilemap, blockedTiles, tilemap.WorldToCell(position + new Vector3(padding, -padding, 0f)))
            || IsBlockedTile(tilemap, blockedTiles, tilemap.WorldToCell(position + new Vector3(-padding, padding, 0f)))
            || IsBlockedTile(tilemap, blockedTiles, tilemap.WorldToCell(position + new Vector3(-padding, -padding, 0f)));
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

    private static Transform FindOrCreateDecorationsParent(Scene scene)
    {
        Transform existing = FindSceneTransform(scene, DecorationsParentName);
        if (existing != null)
            return existing;

        var parent = new GameObject(DecorationsParentName);
        SceneManager.MoveGameObjectToScene(parent, scene);
        Undo.RegisterCreatedObjectUndo(parent, "Create Jumanis Decorations Parent");
        return parent.transform;
    }

    private static Transform FindOrCreateGeneratedParent(Scene scene, Transform decorationsParent, string generatedParentName)
    {
        Transform existing = FindChildByName(decorationsParent, generatedParentName);
        if (existing != null)
            return existing;

        var parent = new GameObject(generatedParentName);
        SceneManager.MoveGameObjectToScene(parent, scene);
        Undo.RegisterCreatedObjectUndo(parent, $"Create {generatedParentName}");
        Undo.SetTransformParent(parent.transform, decorationsParent, $"Parent {generatedParentName}");
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

    private static List<Vector3> CollectDecorationPositions(Scene scene, string decorationNamePrefix, Transform generatedParent)
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

            if (!transform.name.StartsWith(decorationNamePrefix))
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

            if (renderer.transform.name.StartsWith("Jumanis-Flower") || renderer.transform.name.StartsWith("Jumanis-Grass") || renderer.transform.name.StartsWith("Jumanis-Sand"))
                continue;

            Bounds rendererBounds = renderer.bounds;
            rendererBounds.Expand(padding * 2f);
            bounds.Add(rendererBounds);
        }

        return bounds;
    }

    private static GameObject[] LoadPrefabs(JumanisStageAutotilerSettings.DecorationSettings settings, string[] defaultPrefabPaths)
    {
        GameObject[] configuredPrefabs = settings.Prefabs;
        var prefabs = new List<GameObject>();
        for (int i = 0; i < configuredPrefabs.Length; i++)
        {
            if (configuredPrefabs[i] != null)
                prefabs.Add(configuredPrefabs[i]);
        }

        if (prefabs.Count > 0)
            return prefabs.ToArray();

        for (int i = 0; i < defaultPrefabPaths.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(defaultPrefabPaths[i]);
            if (prefab != null)
                prefabs.Add(prefab);
        }

        return prefabs.ToArray();
    }

    private static JumanisStageAutotilerSettings LoadOrCreateSettings()
    {
        JumanisStageAutotilerSettings settings = AssetDatabase.LoadAssetAtPath<JumanisStageAutotilerSettings>(SettingsAssetPath);
        if (settings != null)
        {
            settings.EnsureDefaults(DefaultJumanisFlowerPrefabPaths, DefaultJumanisGrassSpotPrefabPaths, DefaultJumanisSandPatchPrefabPaths);
            return settings;
        }

        settings = ScriptableObject.CreateInstance<JumanisStageAutotilerSettings>();
        settings.ApplyInitialDefaults();
        settings.EnsureDefaults(DefaultJumanisFlowerPrefabPaths, DefaultJumanisGrassSpotPrefabPaths, DefaultJumanisSandPatchPrefabPaths);
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
            string path = $"{TileFolder}/{TilePrefix}_{tileIndex}.asset";
            TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            if (tile == null)
            {
                Debug.LogError($"JumanisGroundAutotiler: missing Jumanis tile at {path}.");
                return null;
            }

            tiles.Add(tileIndex, tile);
        }

        return tiles;
    }

    private static HashSet<TileBase> BuildTileSet(Dictionary<int, TileBase> tiles, int[] tileIndices)
    {
        var tileSet = new HashSet<TileBase>();
        for (int i = 0; i < tileIndices.Length; i++)
        {
            if (tiles.TryGetValue(tileIndices[i], out TileBase tile) && tile != null)
                tileSet.Add(tile);
        }

        return tileSet;
    }

    private static int PickGrassTargetTileIndex(Tilemap tilemap, Vector3Int position, HashSet<TileBase> grassTiles)
    {
        bool north = HasTerrainTile(tilemap, position, 0, 1, grassTiles);
        bool east = HasTerrainTile(tilemap, position, 1, 0, grassTiles);
        bool south = HasTerrainTile(tilemap, position, 0, -1, grassTiles);
        bool west = HasTerrainTile(tilemap, position, -1, 0, grassTiles);
        bool northEast = HasTerrainTile(tilemap, position, 1, 1, grassTiles);
        bool southEast = HasTerrainTile(tilemap, position, 1, -1, grassTiles);
        bool southWest = HasTerrainTile(tilemap, position, -1, -1, grassTiles);
        bool northWest = HasTerrainTile(tilemap, position, -1, 1, grassTiles);

        int missingCardinals = CountMissing(north, east, south, west);

        if (missingCardinals == 0)
        {
            return PickSingleMissingCorner(
                !southEast, 27,
                !southWest, 28,
                !northEast, 31,
                !northWest, 32,
                CenterTileIndex);
        }

        if (missingCardinals == 1)
        {
            if (!north)
                return PickSingleMissingCorner(!southEast, 23, !southWest, 24, 1);

            if (!east)
                return PickSingleMissingCorner(!southWest, 29, !northWest, 33, 8);

            if (!south)
                return PickSingleMissingCorner(!northEast, 35, !northWest, 36, 13);

            return PickSingleMissingCorner(!southEast, 26, !northEast, 30, 6);
        }

        if (missingCardinals == 2)
        {
            if (!north && !west && east && south)
                return southEast ? 0 : 22;

            if (!north && !east && west && south)
                return southWest ? 2 : 25;

            if (!south && !west && east && north)
                return northEast ? 12 : 34;

            if (!south && !east && west && north)
                return northWest ? 14 : 37;
        }

        return CenterTileIndex;
    }

    private static int PickSandTargetTileIndex(Tilemap tilemap, Vector3Int position, HashSet<TileBase> sandTiles)
    {
        bool north = HasTerrainTile(tilemap, position, 0, 1, sandTiles);
        bool east = HasTerrainTile(tilemap, position, 1, 0, sandTiles);
        bool south = HasTerrainTile(tilemap, position, 0, -1, sandTiles);
        bool west = HasTerrainTile(tilemap, position, -1, 0, sandTiles);
        bool northEast = HasTerrainTile(tilemap, position, 1, 1, sandTiles);
        bool southEast = HasTerrainTile(tilemap, position, 1, -1, sandTiles);
        bool southWest = HasTerrainTile(tilemap, position, -1, -1, sandTiles);
        bool northWest = HasTerrainTile(tilemap, position, -1, 1, sandTiles);

        int missingCardinals = CountMissing(north, east, south, west);

        if (missingCardinals == 0)
        {
            return PickSingleMissingCorner(
                !southEast, 18,
                !southWest, 19,
                !northEast, 20,
                !northWest, 21,
                SandCenterTileIndex);
        }

        if (missingCardinals == 1)
        {
            if (!north)
                return 4;

            if (!east)
                return 11;

            if (!south)
                return 16;

            return 9;
        }

        if (missingCardinals == 2)
        {
            if (!north && !west && east && south)
                return 3;

            if (!north && !east && west && south)
                return 5;

            if (!south && !west && east && north)
                return 15;

            if (!south && !east && west && north)
                return 17;
        }

        return SandCenterTileIndex;
    }

    private static bool HasTerrainTile(Tilemap tilemap, Vector3Int position, int xOffset, int yOffset, HashSet<TileBase> terrainTiles)
    {
        TileBase tile = tilemap.GetTile(new Vector3Int(position.x + xOffset, position.y + yOffset, position.z));
        return tile != null && terrainTiles.Contains(tile);
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

    private readonly struct DecorationPlacementResult
    {
        public readonly int Created;
        public readonly int Removed;

        public DecorationPlacementResult(int created, int removed)
        {
            Created = created;
            Removed = removed;
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

    private readonly struct DecorationCandidate
    {
        public readonly Vector3 Position;
        public readonly int PrefabIndex;
        public readonly float Order;

        public DecorationCandidate(Vector3 position, int prefabIndex, float order)
        {
            Position = position;
            PrefabIndex = prefabIndex;
            Order = order;
        }
    }
}
