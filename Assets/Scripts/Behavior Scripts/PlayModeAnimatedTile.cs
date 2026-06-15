using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class PlayModeAnimatedTile : TileBase
{
    public Sprite[] m_AnimatedSprites;
    public float m_MinSpeed = 1f;
    public float m_MaxSpeed = 1f;
    public float m_AnimationStartTime;
    public int m_AnimationStartFrame;
    public Tile.ColliderType m_TileColliderType;
    public TileAnimationFlags m_TileAnimationFlags;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.transform = Matrix4x4.identity;
        tileData.color = Color.white;

        if (m_AnimatedSprites is { Length: > 0 })
        {
            tileData.sprite = m_AnimatedSprites[0];
            tileData.colliderType = m_TileColliderType;
        }
    }

    public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
    {
        if (!Application.isPlaying || m_AnimatedSprites == null || m_AnimatedSprites.Length == 0)
            return false;

        tileAnimationData.animatedSprites = m_AnimatedSprites;
        tileAnimationData.animationSpeed = Random.Range(m_MinSpeed, m_MaxSpeed);
        tileAnimationData.animationStartTime = m_AnimationStartTime;
        tileAnimationData.flags = m_TileAnimationFlags;

        if (m_AnimationStartFrame > 0 && m_AnimationStartFrame <= m_AnimatedSprites.Length)
        {
            var tilemapComponent = tilemap.GetComponent<Tilemap>();
            if (tilemapComponent != null && tilemapComponent.animationFrameRate > 0f)
                tileAnimationData.animationStartTime = (m_AnimationStartFrame - 1) / tilemapComponent.animationFrameRate;
        }

        return true;
    }
}
