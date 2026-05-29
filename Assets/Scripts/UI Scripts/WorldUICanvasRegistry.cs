using UnityEngine;

[RequireComponent(typeof(Canvas))]
public sealed class WorldUICanvasRegistry : MonoBehaviour
{
    public static RectTransform CurrentCanvasRect { get; private set; }
    public static Canvas CurrentCanvas { get; private set; }

    private Canvas canvas;
    private RectTransform rectTransform;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (canvas == null)
            canvas = GetComponent<Canvas>();

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        CurrentCanvas = canvas;
        CurrentCanvasRect = rectTransform;
    }

    private void OnDisable()
    {
        if (CurrentCanvas == canvas)
            CurrentCanvas = null;

        if (CurrentCanvasRect == rectTransform)
            CurrentCanvasRect = null;
    }
}
