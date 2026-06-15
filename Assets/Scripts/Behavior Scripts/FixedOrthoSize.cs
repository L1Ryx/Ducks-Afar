using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class FixedOrthoSize : MonoBehaviour
{
    public enum FramingMode
    {
        FitDesignedArea,
        CropToDesignedArea,
        LetterboxDesignedArea
    }

    [Tooltip("Must match your sprite Pixels Per Unit.")]
    [SerializeField] private int pixelsPerUnit = 32;

    [Tooltip("Horizontal pixel width you want designed gameplay rooms around.")]
    [SerializeField] private int targetHorizontalPixels = 960;

    [Tooltip("Vertical pixel height you want visible in gameplay (design height).")]
    [SerializeField] private int targetVerticalPixels = 540;

    [Tooltip("Fit shows at least the designed room without bars. Letterbox shows exactly the designed room with bars on mismatched screens.")]
    [SerializeField] private FramingMode framingMode = FramingMode.FitDesignedArea;

    private Camera cam;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;

    public Vector2 DesignedWorldSize => new Vector2(
        targetHorizontalPixels / (float)Mathf.Max(1, pixelsPerUnit),
        targetVerticalPixels / (float)Mathf.Max(1, pixelsPerUnit));

    private void Awake()
    {
        EnsureCamera();
        Apply();
    }

    private void OnEnable()
    {
        EnsureCamera();
        Apply();
    }

    private void Update()
    {
        if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight)
            return;

        Apply();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // OnValidate runs in the editor before Awake, so we must reacquire the Camera here.
        EnsureCamera();

        // Avoid spamming errors if inspector is mid-edit
        if (pixelsPerUnit <= 0 || targetHorizontalPixels <= 0 || targetVerticalPixels <= 0)
            return;

        Apply();
    }
#endif

    private void EnsureCamera()
    {
        if (cam == null)
            cam = GetComponent<Camera>();
    }

    private void Apply()
    {
        if (cam == null)
            return;

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        cam.orthographic = true;

        Vector2 designedWorldSize = DesignedWorldSize;
        float targetHalfHeight = designedWorldSize.y * 0.5f;
        float targetHalfWidth = designedWorldSize.x * 0.5f;
        float targetAspect = designedWorldSize.x / designedWorldSize.y;
        float screenAspect = GetScreenAspect(targetAspect);

        switch (framingMode)
        {
            case FramingMode.FitDesignedArea:
                cam.rect = new Rect(0f, 0f, 1f, 1f);
                cam.orthographicSize = Mathf.Max(targetHalfHeight, targetHalfWidth / screenAspect);
                break;

            case FramingMode.CropToDesignedArea:
                cam.rect = new Rect(0f, 0f, 1f, 1f);
                cam.orthographicSize = Mathf.Min(targetHalfHeight, targetHalfWidth / screenAspect);
                break;

            case FramingMode.LetterboxDesignedArea:
                cam.rect = CalculateLetterboxRect(screenAspect, targetAspect);
                cam.orthographicSize = targetHalfHeight;
                break;
        }
    }

    private float GetScreenAspect(float targetAspect)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject))
            return targetAspect;
#endif

        int width = Screen.width;
        int height = Screen.height;

        if (width <= 0 || height <= 0)
        {
            width = cam.pixelWidth;
            height = cam.pixelHeight;
        }

        if (width <= 0 || height <= 0)
            return 16f / 9f;

        return width / (float)height;
    }

    private static Rect CalculateLetterboxRect(float screenAspect, float targetAspect)
    {
        if (screenAspect > targetAspect)
        {
            float width = targetAspect / screenAspect;
            return new Rect((1f - width) * 0.5f, 0f, width, 1f);
        }

        float height = screenAspect / targetAspect;
        return new Rect(0f, (1f - height) * 0.5f, 1f, height);
    }
}
