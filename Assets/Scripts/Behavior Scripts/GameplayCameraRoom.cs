using UnityEngine;

public enum GameplayCameraRoomMusicMode
{
    KeepCurrent,
    Silence,
    PlayCue
}

public enum GameplayCameraRoomAlternateMusicMode
{
    SameAsPrimary,
    KeepCurrent,
    Silence,
    PlayCue
}

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

    [Header("Post Processing")]
    [SerializeField, Range(0f, 100f)] private float creepyVignette;
    [SerializeField, Range(0f, 100f)] private float dreamBloom;
    [SerializeField, Range(0f, 100f)] private float desaturate;
    [SerializeField, Min(0f)] private float postProcessFadeSeconds = 0.5f;

    [Header("Music")]
    [Tooltip("What this room should do to the current music while the primary dimension is active.")]
    [SerializeField] private GameplayCameraRoomMusicMode primaryMusicMode = GameplayCameraRoomMusicMode.KeepCurrent;
    [SerializeField] private AudioCue primaryMusicCue;

    [Tooltip("Defaults to Same As Primary so alternate dimensions inherit the room's normal music setup until overridden.")]
    [SerializeField] private GameplayCameraRoomAlternateMusicMode alternateMusicMode = GameplayCameraRoomAlternateMusicMode.SameAsPrimary;
    [SerializeField] private AudioCue alternateMusicCue;

    private BoxCollider2D roomTrigger;

    public string RoomId => string.IsNullOrWhiteSpace(roomId) ? name : roomId;
    public float CreepyVignettePercent => creepyVignette;
    public float DreamBloomPercent => dreamBloom;
    public float DesaturatePercent => desaturate;
    public float PostProcessFadeSeconds => postProcessFadeSeconds;

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

        if (Application.isPlaying && controller != null && controller.CurrentRoom == this)
            controller.ApplyCurrentRoomLook();
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

    public void SetPostProcessLook(float creepyVignettePercent, float dreamBloomPercent, float desaturatePercent, bool keepCurrentDreamBloom = false)
    {
        creepyVignette = Mathf.Clamp(creepyVignettePercent, 0f, 100f);
        dreamBloom = keepCurrentDreamBloom ? dreamBloom : Mathf.Clamp(dreamBloomPercent, 0f, 100f);
        desaturate = Mathf.Clamp(desaturatePercent, 0f, 100f);

        if (Application.isPlaying && controller != null && controller.CurrentRoom == this)
            controller.ApplyCurrentRoomLook();
    }

    public bool TryGetMusic(DimensionGridState dimension, out AudioCue musicCue, out bool silenceMusic)
    {
        GameplayCameraRoomMusicMode mode = GetMusicMode(dimension);
        musicCue = GetMusicCue(dimension);
        silenceMusic = mode == GameplayCameraRoomMusicMode.Silence;

        return mode == GameplayCameraRoomMusicMode.PlayCue && musicCue != null && musicCue.HasPlayEvent;
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

    private GameplayCameraRoomMusicMode GetMusicMode(DimensionGridState dimension)
    {
        if (dimension == DimensionGridState.Primary)
            return primaryMusicMode;

        return alternateMusicMode switch
        {
            GameplayCameraRoomAlternateMusicMode.KeepCurrent => GameplayCameraRoomMusicMode.KeepCurrent,
            GameplayCameraRoomAlternateMusicMode.Silence => GameplayCameraRoomMusicMode.Silence,
            GameplayCameraRoomAlternateMusicMode.PlayCue => GameplayCameraRoomMusicMode.PlayCue,
            _ => primaryMusicMode
        };
    }

    private AudioCue GetMusicCue(DimensionGridState dimension)
    {
        if (dimension == DimensionGridState.Alternate
            && alternateMusicMode == GameplayCameraRoomAlternateMusicMode.PlayCue)
        {
            return alternateMusicCue;
        }

        return primaryMusicCue;
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
