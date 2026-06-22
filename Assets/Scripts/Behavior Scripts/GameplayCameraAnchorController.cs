using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class GameplayCameraAnchorController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform panTarget;
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";

    [Header("Rooms")]
    [SerializeField] private bool autoDiscoverRooms = true;
    [SerializeField] private GameplayCameraRoom[] rooms;
    [SerializeField] private bool startAtPlayerRoom = true;

    [Header("Motion")]
    [SerializeField, Min(0f)] private float panDuration = 0.45f;
    [SerializeField] private Ease panEase = Ease.OutCubic;
    [SerializeField] private bool useUnscaledTime;
    [SerializeField] private bool lockPlayerDuringPan = true;

    private Camera worldCamera;
    private GameplayCameraRoom currentRoom;
    private Tween panTween;
    private bool ownsInteractionLock;

    public GameplayCameraRoom CurrentRoom => currentRoom;
    public bool IsPanning => panTween != null && panTween.IsActive() && panTween.IsPlaying();

    private void Reset()
    {
        panTarget = transform;
    }

    private void Awake()
    {
        EnsureRefs();
        DiscoverRoomsIfNeeded();
    }

    private void Start()
    {
        if (!startAtPlayerRoom)
            return;

        GameplayCameraRoom startingRoom = FindStartingRoom();
        if (startingRoom != null)
            FocusRoom(startingRoom, immediate: true);
    }

    private void OnDestroy()
    {
        panTween?.Kill();
        ReleasePanLock();
    }

    public void RequestRoom(GameplayCameraRoom room)
    {
        FocusRoom(room, immediate: false);
    }

    public void FocusPlayerRoom(bool immediate)
    {
        GameplayCameraRoom room = FindStartingRoom();
        if (room != null)
            FocusRoom(room, immediate);
    }

    public void FocusRoom(GameplayCameraRoom room, bool immediate)
    {
        if (room == null)
            return;

        EnsureRefs();

        if (panTarget == null)
            return;

        Vector3 targetPosition = room.GetCameraPosition(panTarget.position.z);
        if (currentRoom == room && (panTarget.position - targetPosition).sqrMagnitude <= 0.0001f)
            return;

        currentRoom = room;
        ApplyRoomPostProcessing(room);

        panTween?.Kill();

        if (immediate || panDuration <= 0f || !Application.isPlaying)
        {
            panTarget.DOKill(false);
            panTarget.position = targetPosition;
            ReleasePanLock();
            return;
        }

        AcquirePanLock();

        panTween = panTarget
            .DOMove(targetPosition, panDuration)
            .SetEase(panEase)
            .SetUpdate(useUnscaledTime)
            .OnComplete(ReleasePanLock);
    }

    public void ApplyCurrentRoomLook()
    {
        if (currentRoom != null)
            ApplyRoomPostProcessing(currentRoom);
    }

    private static void ApplyRoomPostProcessing(GameplayCameraRoom room)
    {
        if (room == null)
            return;

        PostProcessEffectToolbox.ApplyRoomLook(
            room.CreepyVignettePercent,
            room.DreamBloomPercent,
            room.DesaturatePercent,
            room.PostProcessFadeSeconds);
    }

    private void EnsureRefs()
    {
        if (worldCamera == null)
            worldCamera = GetComponent<Camera>();

        if (panTarget == null)
            panTarget = transform;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindWithTag(playerTag);
            if (playerObject != null)
                player = playerObject.transform;
        }
    }

    private void DiscoverRoomsIfNeeded()
    {
        if (!autoDiscoverRooms)
            return;

        GameplayCameraRoom[] allRooms = Object.FindObjectsByType<GameplayCameraRoom>(
            FindObjectsInactive.Include);

        int count = 0;
        for (int i = 0; i < allRooms.Length; i++)
        {
            if (allRooms[i] != null && allRooms[i].gameObject.scene == gameObject.scene)
                count++;
        }

        rooms = new GameplayCameraRoom[count];
        int write = 0;
        for (int i = 0; i < allRooms.Length; i++)
        {
            if (allRooms[i] != null && allRooms[i].gameObject.scene == gameObject.scene)
                rooms[write++] = allRooms[i];
        }
    }

    private GameplayCameraRoom FindStartingRoom()
    {
        EnsureRefs();
        DiscoverRoomsIfNeeded();

        if (rooms == null || rooms.Length == 0)
            return null;

        if (player == null)
            return rooms[0];

        Vector2 playerPosition = player.position;
        GameplayCameraRoom nearestRoom = null;
        float nearestDistance = float.PositiveInfinity;

        for (int i = 0; i < rooms.Length; i++)
        {
            GameplayCameraRoom room = rooms[i];
            if (room == null)
                continue;

            if (room.Contains(playerPosition))
                return room;

            float distance = room.DistanceSquaredTo(playerPosition);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestRoom = room;
            }
        }

        return nearestRoom;
    }

    private void AcquirePanLock()
    {
        if (!lockPlayerDuringPan || ownsInteractionLock || !Game.IsReady || Game.Ctx?.InteractionLock == null)
            return;

        Game.Ctx.InteractionLock.Acquire();
        ownsInteractionLock = true;
    }

    private void ReleasePanLock()
    {
        if (!ownsInteractionLock || !Game.IsReady || Game.Ctx?.InteractionLock == null)
        {
            ownsInteractionLock = false;
            return;
        }

        Game.Ctx.InteractionLock.Release();
        ownsInteractionLock = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (panDuration < 0f)
            panDuration = 0f;
    }
#endif
}
