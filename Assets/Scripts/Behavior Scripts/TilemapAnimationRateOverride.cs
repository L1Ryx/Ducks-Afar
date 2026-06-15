using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Tilemap))]
public sealed class TilemapAnimationRateOverride : MonoBehaviour
{
    [SerializeField, Min(0f)] private float editModeAnimationFrameRate = 0f;
    [SerializeField, Min(0f)] private float playModeAnimationFrameRate = 1f;

    private Tilemap tilemap;

    private void Awake()
    {
        ApplyAnimationFrameRate();
    }

    private void OnEnable()
    {
        ApplyAnimationFrameRate();
    }

    private void OnValidate()
    {
        ApplyAnimationFrameRate();
    }

    private void ApplyAnimationFrameRate()
    {
        if (tilemap == null)
            tilemap = GetComponent<Tilemap>();

        if (tilemap == null)
            return;

        tilemap.animationFrameRate = Application.isPlaying
            ? playModeAnimationFrameRate
            : editModeAnimationFrameRate;
    }
}
