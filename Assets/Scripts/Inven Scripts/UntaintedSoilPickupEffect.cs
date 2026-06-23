using UnityEngine;

[DisallowMultipleComponent]
public sealed class UntaintedSoilPickupEffect : MonoBehaviour, IPickupSuccessEffect
{
    [System.Serializable]
    private sealed class RoomLookTarget
    {
        public GameplayCameraRoom room;
        public string roomId = "auralis_11";
        [Range(0f, 100f)] public float creepyVignette = 100f;
        [Range(0f, 100f)] public float dreamBloom;
        [Range(0f, 100f)] public float desaturate = 100f;
        public bool preserveCurrentDreamBloom = true;
    }

    [Header("Room Look")]
    [SerializeField] private RoomLookTarget[] roomLookTargets =
    {
        new RoomLookTarget()
    };

    [Header("Dimension")]
    [SerializeField] private DimensionWorldGridSwitcher dimensionSwitcher;

    [Header("Spawn")]
    [SerializeField] private GameObject spawnPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform spawnParent;

    private bool hasRun;

    public void OnPickupSucceeded(GameObject pickup, GameObject interactor)
    {
        if (hasRun)
            return;

        hasRun = true;

        ApplyRoomLooks();

        ResolveSwitcher()?.SwitchToPrimaryWithoutSaving();
        SpawnConfiguredPrefab();
    }

    private GameplayCameraRoom ApplyRoomLooks()
    {
        GameplayCameraRoom firstResolvedRoom = null;

        if (roomLookTargets == null)
            return null;

        for (int i = 0; i < roomLookTargets.Length; i++)
        {
            RoomLookTarget target = roomLookTargets[i];
            GameplayCameraRoom room = ResolveRoom(target);
            if (room == null)
                continue;

            firstResolvedRoom ??= room;
            room.SetPostProcessLook(
                target.creepyVignette,
                target.dreamBloom,
                target.desaturate,
                keepCurrentDreamBloom: target.preserveCurrentDreamBloom);
        }

        return firstResolvedRoom;
    }

    private GameplayCameraRoom ResolveRoom(RoomLookTarget target)
    {
        if (target == null)
            return null;

        if (target.room != null)
            return target.room;

        GameplayCameraRoom[] rooms = FindObjectsByType<GameplayCameraRoom>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < rooms.Length; i++)
        {
            GameplayCameraRoom room = rooms[i];
            if (room != null && room.RoomId == target.roomId)
            {
                target.room = room;
                return target.room;
            }
        }

        Debug.LogWarning($"{nameof(UntaintedSoilPickupEffect)} could not find camera room '{target.roomId}'.", this);
        return null;
    }

    private DimensionWorldGridSwitcher ResolveSwitcher()
    {
        if (dimensionSwitcher == null)
            dimensionSwitcher = FindAnyObjectByType<DimensionWorldGridSwitcher>();

        return dimensionSwitcher;
    }

    private void SpawnConfiguredPrefab()
    {
        if (spawnPrefab == null || spawnPoint == null)
            return;

        Transform parent = spawnParent != null ? spawnParent : spawnPoint.parent;
        Instantiate(spawnPrefab, spawnPoint.position, spawnPoint.rotation, parent);
    }
}
