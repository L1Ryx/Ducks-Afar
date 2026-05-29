using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class ScreenSpaceFollowWorld : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private Transform worldAnchor;

    [Header("Offset")]
    [SerializeField] private Vector2 screenOffsetPx = new Vector2(0f, 24f);

    [Header("Clamping")]
    [SerializeField] private bool clampToCanvas = true;
    [SerializeField] private bool applyXOffsetWhenClamped = true;

    [Tooltip("Horizontal offset applied when vertical clamping occurs.")]
    [SerializeField] private float clampedXOffsetPx = -24f;

    [Tooltip("If true, offset is applied to the right. If false, to the left.")]
    [SerializeField] private bool preferRightSide = true;

    [Tooltip("Padding from canvas edges in pixels. X = left/right, Y = bottom/top.")]
    [SerializeField] private Vector2 edgePaddingPx = new Vector2(8f, 8f);

    [Tooltip("If true, when panel would go off the top, flip it below the anchor (and vice versa).")]
    [SerializeField] private bool flipVerticallyToFit = false;

    [Tooltip("Extra vertical gap applied when flipping.")]
    [SerializeField] private float flipExtraGapPx = 6f;

    private RectTransform rt;

    private void Awake()
    {
        rt = (RectTransform)transform;
        if (worldCamera == null) worldCamera = Camera.main;
    }

    private void LateUpdate()
    {
        FollowAnchor();
    }

    public void FollowAnchor()
    {
        if (rt == null)
            rt = (RectTransform)transform;

        if (worldCamera == null || canvasRect == null || worldAnchor == null)
            return;

        // World -> Screen
        Vector3 screen = worldCamera.WorldToScreenPoint(worldAnchor.position);
        if (screen.z < 0f) return;

        // Screen -> Canvas local point
        screen.x += screenOffsetPx.x;
        screen.y += screenOffsetPx.y;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screen, null, out Vector2 desiredLocalPos
        );

        if (clampToCanvas)
        {
            bool wasVerticallyClamped;
            desiredLocalPos = ClampLocalPointToCanvas(
                desiredLocalPos,
                ref screenOffsetPx,
                out wasVerticallyClamped
            );

            if (applyXOffsetWhenClamped && wasVerticallyClamped)
            {
                float dir = preferRightSide ? 1f : -1f;
                desiredLocalPos.x += dir * clampedXOffsetPx;
            }

        }

        rt.anchoredPosition = desiredLocalPos;
    }

    private Vector2 ClampLocalPointToCanvas(
        Vector2 desiredLocalPos,
        ref Vector2 usedOffset,
        out bool wasVerticallyClamped
    )
    {
        wasVerticallyClamped = false;
        // Canvas rect in local space
        Rect canvas = canvasRect.rect;

        // Our rect size (in local space units)
        Vector2 size = rt.rect.size;

        // Consider pivot: anchoredPosition places the pivot at that point
        float leftExtent   = size.x * rt.pivot.x;
        float rightExtent  = size.x * (1f - rt.pivot.x);
        float bottomExtent = size.y * rt.pivot.y;
        float topExtent    = size.y * (1f - rt.pivot.y);

        // Compute allowed range for the pivot point
        float minX = canvas.xMin + edgePaddingPx.x + leftExtent;
        float maxX = canvas.xMax - edgePaddingPx.x - rightExtent;

        float minY = canvas.yMin + edgePaddingPx.y + bottomExtent;
        float maxY = canvas.yMax - edgePaddingPx.y - topExtent;

        float originalY = desiredLocalPos.y;
        
        if (flipVerticallyToFit)
        {
            if (desiredLocalPos.y > maxY)
            {
                desiredLocalPos.y = desiredLocalPos.y - (usedOffset.y * 2f) - flipExtraGapPx;
                wasVerticallyClamped = true;
            }
            else if (desiredLocalPos.y < minY)
            {
                desiredLocalPos.y = desiredLocalPos.y - (usedOffset.y * 2f) + flipExtraGapPx;
                wasVerticallyClamped = true;
            }
        }
        
        float clampedY = Mathf.Clamp(desiredLocalPos.y, minY, maxY);
        if (!Mathf.Approximately(clampedY, desiredLocalPos.y))
        {
            wasVerticallyClamped = true;
            desiredLocalPos.y = clampedY;
        }
        else
        {
            desiredLocalPos.y = clampedY;
        }

        
        desiredLocalPos.x = Mathf.Clamp(desiredLocalPos.x, minX, maxX);

        return desiredLocalPos;
    }

    public void Init(Camera cam, RectTransform canvas, Transform anchor)
    {
        if (rt == null)
            rt = (RectTransform)transform;

        worldCamera = cam;
        canvasRect = canvas;
        worldAnchor = anchor;
    }

    // Optional: allow runtime tuning from spawners
    public void SetOffset(Vector2 offsetPx) => screenOffsetPx = offsetPx;
    public void SetClamp(bool clamp, Vector2 paddingPx)
    {
        clampToCanvas = clamp;
        edgePaddingPx = paddingPx;
    }

    public void SetApplyXOffsetWhenClamped(bool apply)
    {
        applyXOffsetWhenClamped = apply;
    }
}
