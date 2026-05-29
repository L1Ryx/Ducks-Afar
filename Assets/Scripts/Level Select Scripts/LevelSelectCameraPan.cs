using UnityEngine;

public sealed class LevelSelectCameraPan : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform panTarget;

    [Header("Authoring Bounds")]
    [SerializeField] private float minX = -8f;
    [SerializeField] private float maxX = 8f;

    [Header("Mouse Edge Pan")]
    [Range(0.05f, 0.45f)] [SerializeField] private float edgeActivationPercent = 0.18f;
    [SerializeField] private float maxPanSpeed = 10f;
    [SerializeField] private float accelerationPower = 1.6f;
    [SerializeField] private float smoothing = 16f;

    private float currentVelocity;

    public float MinX
    {
        get => minX;
        set => minX = value;
    }

    public float MaxX
    {
        get => maxX;
        set => maxX = value;
    }

    private void Reset()
    {
        panTarget = transform;
    }

    private void Awake()
    {
        if (panTarget == null)
            panTarget = transform;
    }

    private void Update()
    {
        if (panTarget == null || PauseUtility.IsPaused)
            return;

        if (LevelSelectController.IsAnyCardDeckOpen())
        {
            currentVelocity = 0f;
            return;
        }

        float desiredVelocity = GetDesiredVelocity();
        currentVelocity = Mathf.Lerp(
            currentVelocity,
            desiredVelocity,
            1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime));

        Vector3 position = panTarget.position;
        position.x = Mathf.Clamp(position.x + currentVelocity * Time.unscaledDeltaTime, minX, maxX);
        panTarget.position = position;
    }

    public void SetPanBounds(float leftX, float rightX)
    {
        minX = Mathf.Min(leftX, rightX);
        maxX = Mathf.Max(leftX, rightX);
    }

    private float GetDesiredVelocity()
    {
        if (Screen.width <= 0)
            return 0f;

        float edgeWidth = Mathf.Max(1f, Screen.width * edgeActivationPercent);
        float mouseX = Input.mousePosition.x;
        float direction = 0f;

        if (mouseX <= edgeWidth)
            direction = -Mathf.InverseLerp(edgeWidth, 0f, mouseX);
        else if (mouseX >= Screen.width - edgeWidth)
            direction = Mathf.InverseLerp(Screen.width - edgeWidth, Screen.width, mouseX);

        if (Mathf.Approximately(direction, 0f))
            return 0f;

        float magnitude = Mathf.Pow(Mathf.Abs(direction), accelerationPower);
        return Mathf.Sign(direction) * magnitude * maxPanSpeed;
    }

    private void OnValidate()
    {
        if (minX > maxX)
        {
            (minX, maxX) = (maxX, minX);
        }

        maxPanSpeed = Mathf.Max(0f, maxPanSpeed);
        smoothing = Mathf.Max(0f, smoothing);
        accelerationPower = Mathf.Max(0.1f, accelerationPower);
    }
}
