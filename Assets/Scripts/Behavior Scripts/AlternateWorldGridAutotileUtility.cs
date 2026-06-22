using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public sealed class AlternateWorldGridAutotileUtility : MonoBehaviour
{
    [Serializable]
    public sealed class TileMapping
    {
        public TileBase source;
        public TileBase target;
    }

    [Serializable]
    public sealed class PrefabMapping
    {
        public GameObject source;
        public GameObject target;
    }

    [Header("World Grids")]
    public Transform sourceWorldGrid;
    public Transform targetWorldGrid;
    public string sourceWorldGridName = "World Grid";
    public string targetWorldGridName = "Alternate World Grid";
    public bool targetWorldGridActiveAfterBuild;

    [Header("Layer Names")]
    public string groundLayerName = "Ground (NC)";
    public string oceanLayerName = "Ocean (NC)";
    public string shorelineLayerName = "Shoreline (C)";

    [Header("Source Layers")]
    public Tilemap sourceGround;
    public Tilemap sourceOcean;
    public Tilemap sourceShoreline;

    [Header("Target Layers")]
    public Tilemap targetGround;
    public Tilemap targetOcean;
    public Tilemap targetShoreline;

    [Header("Ground Conversion")]
    public List<TileMapping> groundTileMappings = new();

    [Header("Ocean Conversion")]
    [Tooltip("Optional. When blank, every source ocean tile is converted to Target Ocean Tile.")]
    public TileBase sourceOceanTile;
    public TileBase targetOceanTile;

    [Header("Flowers")]
    public Transform sourceFlowersParent;
    public Transform targetFlowersParent;
    public string sourceFlowersParentName = "Flowers";
    public string targetFlowersParentName = "Yellow Flowers";
    public List<PrefabMapping> flowerPrefabMappings = new();
    public bool includeInactiveFlowers = true;
    public bool clearTargetFlowersBeforeCopy = true;

    [Header("Build")]
    public bool clearTargetLayersBeforeBuild = true;

    private void Reset()
    {
        ResolveSceneReferences();
    }

    public void ResolveSceneReferences()
    {
        if (sourceWorldGrid == null)
            sourceWorldGrid = FindSceneTransform(sourceWorldGridName);

        if (targetWorldGrid == null)
            targetWorldGrid = FindSceneTransform(targetWorldGridName);

        if (sourceGround == null)
            sourceGround = FindTilemapUnder(sourceWorldGrid, groundLayerName);

        if (sourceOcean == null)
            sourceOcean = FindTilemapUnder(sourceWorldGrid, oceanLayerName);

        if (sourceShoreline == null)
            sourceShoreline = FindTilemapUnder(sourceWorldGrid, shorelineLayerName);

        if (targetGround == null)
            targetGround = FindTilemapUnder(targetWorldGrid, groundLayerName);

        if (targetOcean == null)
            targetOcean = FindTilemapUnder(targetWorldGrid, oceanLayerName);

        if (targetShoreline == null)
            targetShoreline = FindTilemapUnder(targetWorldGrid, shorelineLayerName);

        if (sourceFlowersParent == null)
            sourceFlowersParent = FindSceneTransform(sourceFlowersParentName);

        if (targetFlowersParent == null)
            targetFlowersParent = FindChildByName(targetWorldGrid, targetFlowersParentName);
    }

    public static Transform FindSceneTransform(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
                return transforms[i];
        }

        return null;
    }

    public static Tilemap FindTilemapUnder(Transform parent, string tilemapName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(tilemapName))
            return null;

        Tilemap[] tilemaps = parent.GetComponentsInChildren<Tilemap>(includeInactive: true);
        for (int i = 0; i < tilemaps.Length; i++)
        {
            if (tilemaps[i].name == tilemapName)
                return tilemaps[i];
        }

        return null;
    }

    public static Transform FindChildByName(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;
        }

        return null;
    }
}
