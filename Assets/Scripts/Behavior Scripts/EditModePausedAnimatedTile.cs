using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class EditModePausedAnimatedTile : AnimatedTile
{
    public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
    {
#if UNITY_EDITOR
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
            return false;
#else
        if (!Application.isPlaying)
            return false;
#endif

        return base.GetTileAnimationData(position, tilemap, ref tileAnimationData);
    }
}
