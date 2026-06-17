using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class RavenFlyby : MonoBehaviour
{
    private const float MinimumFrameDuration = 0.01f;

    [Header("Animation")]
    [SerializeField] private Sprite[] flightFrames;
    [SerializeField, Min(0.01f)] private float secondsPerFrame = 0.25f;

    [Header("Visual Alignment")]
    [SerializeField] private bool alignVisualToDirection = true;
    [SerializeField] private float spriteForwardAngle = 180f;

    private SpriteRenderer spriteRenderer;
    private Vector2 direction;
    private float speed;
    private float offscreenPadding;
    private float bobAmplitude;
    private float bobFrequency;
    private float age;
    private float frameTimer;
    private int frameIndex;
    private Camera targetCamera;
    private Vector3 bobOffsetPrevious;
    private bool hasEnteredCameraView;

    public void Initialize(
        Vector2 flightDirection,
        float flightSpeed,
        float cleanupPadding,
        float sineBobAmplitude,
        float sineBobFrequency,
        Camera cameraToUse)
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        direction = flightDirection.sqrMagnitude > 0f ? flightDirection.normalized : Vector2.left;
        speed = Mathf.Max(0f, flightSpeed);
        offscreenPadding = Mathf.Max(0f, cleanupPadding);
        bobAmplitude = Mathf.Max(0f, sineBobAmplitude);
        bobFrequency = Mathf.Max(0f, sineBobFrequency);
        targetCamera = cameraToUse != null ? cameraToUse : Camera.main;
        age = 0f;
        frameTimer = 0f;
        frameIndex = 0;
        bobOffsetPrevious = Vector3.zero;
        hasEnteredCameraView = false;

        if (flightFrames != null && flightFrames.Length > 0)
            spriteRenderer.sprite = flightFrames[0];

        if (alignVisualToDirection)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - spriteForwardAngle);
        }
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        age += dt;

        AdvanceAnimation(dt);
        Move(dt);

        if (IsPastCameraBounds())
            Destroy(gameObject);
    }

    private void AdvanceAnimation(float dt)
    {
        if (flightFrames == null || flightFrames.Length == 0)
            return;

        frameTimer += dt;
        float frameDuration = Mathf.Max(MinimumFrameDuration, secondsPerFrame);
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex = (frameIndex + 1) % flightFrames.Length;
            spriteRenderer.sprite = flightFrames[frameIndex];
        }
    }

    private void Move(float dt)
    {
        Vector3 forwardStep = (Vector3)(direction * speed * dt);
        Vector3 bobOffset = Vector3.zero;

        if (bobAmplitude > 0f && bobFrequency > 0f)
        {
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            bobOffset = perpendicular * (Mathf.Sin(age * bobFrequency * Mathf.PI * 2f) * bobAmplitude);
        }

        transform.position += forwardStep + bobOffset - bobOffsetPrevious;
        bobOffsetPrevious = bobOffset;
    }

    private bool IsPastCameraBounds()
    {
        if (targetCamera == null)
            return age > 20f;

        Vector3 viewportPoint = targetCamera.WorldToViewportPoint(transform.position);
        float padding = offscreenPadding / Mathf.Max(0.01f, targetCamera.orthographicSize * 2f);
        bool insideCameraView = viewportPoint.x >= 0f
            && viewportPoint.x <= 1f
            && viewportPoint.y >= 0f
            && viewportPoint.y <= 1f;

        hasEnteredCameraView |= insideCameraView;
        if (!hasEnteredCameraView)
            return false;

        return viewportPoint.x < -padding
            || viewportPoint.x > 1f + padding
            || viewportPoint.y < -padding
            || viewportPoint.y > 1f + padding;
    }
}
