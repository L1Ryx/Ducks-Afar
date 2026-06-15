using System.Collections;
using UnityEngine;

public sealed class SavedSpawnPoint : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private PersistentId persistentId;
    [SerializeField] private string spawnPointId;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool applyOnStart = true;
    [SerializeField, Min(1)] private int maxWaitFramesForPlayer = 30;

    [Header("Saving")]
    [SerializeField] private bool saveActiveSlotWhenMarked = true;

    [Header("Game Events")]
    [SerializeField] private GameEvent appliedSpawnEvent;
    [SerializeField] private GameEvent markedSpawnEvent;

    private void Awake()
    {
        if (persistentId == null)
            persistentId = GetComponent<PersistentId>();
    }

    private void Start()
    {
        if (applyOnStart)
            StartCoroutine(ApplyIfCurrentLocationWhenReady());
    }

    public void MarkAsCurrentLocation()
    {
        if (!TryResolveSpawnPointId(out string resolvedSpawnPointId))
            return;

        if (!Game.IsReady || Game.Ctx?.SaveState == null)
        {
            Debug.LogWarning($"{name}: cannot mark spawn point because GameContext is not ready.", this);
            return;
        }

        Game.Ctx.SaveState.CurrentLocation = resolvedSpawnPointId;
        markedSpawnEvent?.Raise();

        if (saveActiveSlotWhenMarked)
            Game.Ctx.Saves?.SaveToActiveSlot(captureSceneCheckpoint: false);
    }

    public void ApplyIfCurrentLocation()
    {
        if (!TryResolveSpawnPointId(out string resolvedSpawnPointId))
            return;

        if (!Game.IsReady || Game.Ctx?.SaveState == null)
            return;

        if (!IsCurrentLocation(resolvedSpawnPointId))
            return;

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null)
            return;

        ApplyToPlayer(player);
    }

    private IEnumerator ApplyIfCurrentLocationWhenReady()
    {
        for (int i = 0; i < maxWaitFramesForPlayer; i++)
        {
            ApplyIfCurrentLocation();

            if (TryResolveSpawnPointId(out string resolvedSpawnPointId)
                && Game.IsReady
                && Game.Ctx?.SaveState != null
                && IsCurrentLocation(resolvedSpawnPointId)
                && GameObject.FindGameObjectWithTag(playerTag) != null)
            {
                yield break;
            }

            yield return null;
        }
    }

    private void ApplyToPlayer(GameObject player)
    {
        player.transform.SetPositionAndRotation(transform.position, player.transform.rotation);

        if (player.TryGetComponent<Rigidbody2D>(out var body))
            body.linearVelocity = Vector2.zero;

        appliedSpawnEvent?.Raise();
    }

    private bool TryResolveSpawnPointId(out string resolvedSpawnPointId)
    {
        resolvedSpawnPointId = string.IsNullOrWhiteSpace(spawnPointId) ? string.Empty : spawnPointId.Trim();

        if (resolvedSpawnPointId.Length == 0 && persistentId != null)
            persistentId.TryGetId(out resolvedSpawnPointId);

        if (resolvedSpawnPointId.Length > 0)
            return true;

        Debug.LogWarning($"{name}: spawn point id is empty.", this);
        return false;
    }

    private static bool IsCurrentLocation(string spawnPointId)
    {
        string currentLocation = Game.Ctx.SaveState.CurrentLocation;
        currentLocation = string.IsNullOrWhiteSpace(currentLocation) ? string.Empty : currentLocation.Trim();
        return currentLocation == spawnPointId;
    }
}
