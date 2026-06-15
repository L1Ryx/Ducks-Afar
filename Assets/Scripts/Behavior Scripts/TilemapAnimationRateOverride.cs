using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Tilemap))]
public sealed class TilemapAnimationRateOverride : MonoBehaviour
{
    [SerializeField, Min(0f)] private float editModeAnimationFrameRate = 0f;
    [SerializeField, Min(0f)] private float playModeAnimationFrameRate = 1f;
    [SerializeField] private bool rebindAnimatedTilesOnPlay = true;

    private Tilemap tilemap;

    private void Awake()
    {
        ApplyAnimationFrameRate(refreshTiles: false);
    }

    private void OnEnable()
    {
        ApplyAnimationFrameRate(refreshTiles: false);
    }

    private void OnValidate()
    {
        ApplyAnimationFrameRate(refreshTiles: false);
    }

    private void Start()
    {
        ApplyAnimationFrameRate(refreshTiles: false);

        if (Application.isPlaying && rebindAnimatedTilesOnPlay)
            StartCoroutine(RebindAnimatedTilesAfterStartup());
    }

    private void ApplyAnimationFrameRate(bool refreshTiles)
    {
        if (tilemap == null)
            tilemap = GetComponent<Tilemap>();

        if (tilemap == null)
            return;

        tilemap.animationFrameRate = Application.isPlaying
            ? playModeAnimationFrameRate
            : editModeAnimationFrameRate;

        if (refreshTiles && Application.isPlaying)
            tilemap.RefreshAllTiles();
    }

    private IEnumerator RebindAnimatedTilesAfterStartup()
    {
        yield return null;

        if (tilemap == null)
            tilemap = GetComponent<Tilemap>();

        if (tilemap == null)
            yield break;

        BoundsInt bounds = tilemap.cellBounds;
        for (int y = bounds.yMin; y < bounds.yMax; y++)
        {
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                var position = new Vector3Int(x, y, 0);
                TileBase tile = tilemap.GetTile(position);

                if (tile is AnimatedTile)
                {
                    tilemap.SetTile(position, null);
                    tilemap.SetTile(position, tile);
                }
            }
        }
    }
}
