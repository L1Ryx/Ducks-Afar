using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class EditModePausedAnimatedTile : AnimatedTile
{
    public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
    {
        if (!Application.isPlaying)
            return false;

        return base.GetTileAnimationData(position, tilemap, ref tileAnimationData);
    }
}
