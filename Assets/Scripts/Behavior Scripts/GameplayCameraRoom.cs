using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class GameplayCameraRoom : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string roomId;

    [Header("Refs")]
    [SerializeField] private GameplayCameraAnchorController controller;
    [SerializeField] private string playerTag = "Player";

    [Header("Camera")]
    [SerializeField] private Vector2 cameraOffset;

    private BoxCollider2D roomTrigger;

    public string RoomId => string.IsNullOrWhiteSpace(roomId) ? name : roomId;

    public Bounds Bounds
    {
        get
        {
            EnsureTrigger();
            return roomTrigger != null ? roomTrigger.bounds : new Bounds(transform.position, Vector3.zero);
        }
    }

    private void Reset()
    {
        roomId = name;
        EnsureTrigger();

        if (roomTrigger != null)
            roomTrigger.isTrigger = true;
    }

    private void Awake()
    {
        EnsureTrigger();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryActivateFor(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryActivateFor(other);
    }

    public Vector3 GetCameraPosition(float z)
    {
        Vector3 position = transform.position;
        position.x += cameraOffset.x;
        position.y += cameraOffset.y;
        position.z = z;
        return position;
    }

    public bool Contains(Vector2 worldPoint)
    {
        EnsureTrigger();
        return roomTrigger != null && roomTrigger.OverlapPoint(worldPoint);
    }

    public float DistanceSquaredTo(Vector2 worldPoint)
    {
        Bounds bounds = Bounds;
        Vector2 closest = bounds.ClosestPoint(worldPoint);
        return (worldPoint - closest).sqrMagnitude;
    }

    private void TryActivateFor(Collider2D other)
    {
        if (other == null || !other.CompareTag(playerTag))
            return;

        Vector2 playerPosition = other.attachedRigidbody != null
            ? other.attachedRigidbody.position
            : other.transform.position;

        if (!Contains(playerPosition))
            return;

        EnsureController();
        controller?.RequestRoom(this);
    }

    private void EnsureTrigger()
    {
        if (roomTrigger == null)
            roomTrigger = GetComponent<BoxCollider2D>();
    }

    private void EnsureController()
    {
        if (controller != null)
            return;

        controller = Object.FindAnyObjectByType<GameplayCameraAnchorController>();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureTrigger();

        if (roomTrigger != null)
            roomTrigger.isTrigger = true;
    }

    private void OnDrawGizmos()
    {
        EnsureTrigger();

        if (roomTrigger == null)
            return;

        Bounds bounds = roomTrigger.bounds;
        Gizmos.color = new Color(1f, 0.76f, 0.15f, 0.18f);
        Gizmos.DrawCube(bounds.center, bounds.size);
        Gizmos.color = new Color(1f, 0.76f, 0.15f, 0.9f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        Vector3 cameraPosition = GetCameraPosition(transform.position.z);
        Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.95f);
        Gizmos.DrawWireSphere(cameraPosition, 0.35f);
    }
#endif
}
