using UnityEngine;

[DisallowMultipleComponent]
public sealed class UntaintedSoilPickupEffect : MonoBehaviour, IPickupSuccessEffect
{
    [Header("Room Look")]
    [SerializeField] private GameplayCameraRoom targetRoom;
    [SerializeField] private string targetRoomId = "auralis_11";
    [SerializeField, Range(0f, 100f)] private float creepyVignette = 100f;
    [SerializeField, Range(0f, 100f)] private float desaturate = 100f;
    [SerializeField] private bool preserveRoomDreamBloom = true;

    [Header("Dimension")]
    [SerializeField] private DimensionWorldGridSwitcher dimensionSwitcher;

    [Header("Temporary NPC Spawn")]
    [SerializeField] private GameObject npcPrefab;
    [SerializeField] private Transform npcParent;
    [SerializeField] private Vector3 npcSpawnOffset;

    private bool hasRun;

    public void OnPickupSucceeded(GameObject pickup, GameObject interactor)
    {
        if (hasRun)
            return;

        hasRun = true;

        GameplayCameraRoom room = ResolveRoom();
        if (room != null)
        {
            room.SetPostProcessLook(
                creepyVignette,
                0f,
                desaturate,
                keepCurrentDreamBloom: preserveRoomDreamBloom);
        }

        ResolveSwitcher()?.SwitchToPrimaryWithoutSaving();
        SpawnNpc(room);
    }

    private GameplayCameraRoom ResolveRoom()
    {
        if (targetRoom != null)
            return targetRoom;

        GameplayCameraRoom[] rooms = FindObjectsByType<GameplayCameraRoom>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < rooms.Length; i++)
        {
            GameplayCameraRoom room = rooms[i];
            if (room != null && room.RoomId == targetRoomId)
            {
                targetRoom = room;
                return targetRoom;
            }
        }

        Debug.LogWarning($"{nameof(UntaintedSoilPickupEffect)} could not find camera room '{targetRoomId}'.", this);
        return null;
    }

    private DimensionWorldGridSwitcher ResolveSwitcher()
    {
        if (dimensionSwitcher == null)
            dimensionSwitcher = FindAnyObjectByType<DimensionWorldGridSwitcher>();

        return dimensionSwitcher;
    }

    private void SpawnNpc(GameplayCameraRoom room)
    {
        if (npcPrefab == null || room == null)
            return;

        Vector3 spawnPosition = room.Bounds.center + npcSpawnOffset;
        spawnPosition.z = npcPrefab.transform.position.z;

        Transform parent = npcParent != null ? npcParent : room.transform;
        Instantiate(npcPrefab, spawnPosition, npcPrefab.transform.rotation, parent);
    }
}
