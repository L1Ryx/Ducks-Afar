using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class HomeGroundAutotiler
{
    private const string StageHomeScenePath = "Assets/Scenes/Live/Stage Home.unity";
    private const string GroundTilemapName = "Ground (NC)";
    private const string OceanTilemapName = "Ocean (NC)";
    private const string ShorelineTilemapName = "Shoreline (C)";
    private const string HomeTileFolder = "Assets/Art/Tilemaps/Palettes/Home Tiles";
    private const int CenterTileIndex = 7;
    private const int ShorelineFillTileIndex = 10;

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
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != StageHomeScenePath)
        {
            Debug.LogWarning($"HomeGroundAutotiler: open {StageHomeScenePath} before autotiling. No tiles changed.");
            return;
        }

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

        if (groundReplacements == 0 && shorelineResult.Added == 0 && shorelineResult.Removed == 0)
        {
            Debug.Log($"HomeGroundAutotiler: no Stage Home tile changes needed. Kept {centerTilesKept} ground center tile(s); {shorelineResult.Kept} shoreline tile(s) already matched.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(activeScene);

        Debug.Log(
            $"HomeGroundAutotiler: replaced {groundReplacements} ground center tile(s), kept {centerTilesKept} ground center tile(s), " +
            $"added {shorelineResult.Added} shoreline tile(s), removed {shorelineResult.Removed} stale shoreline tile(s), kept {shorelineResult.Kept} shoreline tile(s).");
    }

    [MenuItem("Ducks Afar/Tilemaps/Autotile Stage Home #p", true)]
    private static bool CanAutotileStageHome()
    {
        return SceneManager.GetActiveScene().path == StageHomeScenePath;
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
}
