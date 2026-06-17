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
    private const string HomeTileFolder = "Assets/Art/Tilemaps/Palettes/Home Tiles";
    private const int CenterTileIndex = 7;

    private static readonly int[] RequiredTileIndices =
    {
        0, 1, 2,
        6, 7, 8,
        12, 13, 14,
        26, 27, 28, 29,
        30, 31, 32, 33,
        34, 35, 36, 37,
        38, 39, 40, 41,
    };

    [MenuItem("Ducks Afar/Tilemaps/Autotile Stage Home Ground #p")]
    public static void AutotileStageHomeGround()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != StageHomeScenePath)
        {
            Debug.LogWarning($"HomeGroundAutotiler: open {StageHomeScenePath} before autotiling. No tiles changed.");
            return;
        }

        Tilemap ground = FindGroundTilemap(activeScene);
        if (ground == null)
        {
            Debug.LogError($"HomeGroundAutotiler: could not find tilemap '{GroundTilemapName}' in {StageHomeScenePath}.");
            return;
        }

        Dictionary<int, TileBase> tiles = LoadRequiredTiles();
        if (tiles == null)
            return;

        TileBase centerTile = tiles[CenterTileIndex];
        var replacements = new List<TileReplacement>();
        int centerTilesKept = 0;

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
        {
            Debug.Log($"HomeGroundAutotiler: no {GroundTilemapName} center tiles needed replacement. Kept {centerTilesKept} center tile(s).");
            return;
        }

        Undo.RegisterCompleteObjectUndo(ground, "Autotile Stage Home Ground");

        for (int i = 0; i < replacements.Count; i++)
            ground.SetTile(replacements[i].Position, replacements[i].Tile);

        EditorUtility.SetDirty(ground);
        EditorSceneManager.MarkSceneDirty(activeScene);

        Debug.Log($"HomeGroundAutotiler: replaced {replacements.Count} center tile(s) on {GroundTilemapName}; kept {centerTilesKept} center tile(s).");
    }

    [MenuItem("Ducks Afar/Tilemaps/Autotile Stage Home Ground #p", true)]
    private static bool CanAutotileStageHomeGround()
    {
        return SceneManager.GetActiveScene().path == StageHomeScenePath;
    }

    private static Tilemap FindGroundTilemap(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Tilemap[] tilemaps = root.GetComponentsInChildren<Tilemap>(includeInactive: true);
            for (int i = 0; i < tilemaps.Length; i++)
            {
                if (tilemaps[i].name == GroundTilemapName)
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
}
