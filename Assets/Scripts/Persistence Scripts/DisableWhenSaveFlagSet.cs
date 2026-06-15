using UnityEngine;

public sealed class DisableWhenSaveFlagSet : MonoBehaviour
{
    [Header("Flag")]
    [SerializeField] private PersistentId persistentId;
    [SerializeField] private string flagId;

    [Header("Targets")]
    [SerializeField, HideInInspector] private GameObject target;
    [SerializeField] private GameObject[] targets;
    [SerializeField] private bool evaluateOnStart = true;

    [Header("Game Events")]
    [SerializeField] private GameEvent refreshEvent;

    private void Awake()
    {
        if (persistentId == null)
            persistentId = GetComponent<PersistentId>();
    }

    private void OnEnable()
    {
        if (refreshEvent != null)
            refreshEvent.RegisterRuntimeListener(Refresh);
    }

    private void Start()
    {
        if (evaluateOnStart)
            Refresh();
    }

    private void OnDisable()
    {
        if (refreshEvent != null)
            refreshEvent.UnregisterRuntimeListener(Refresh);
    }

    public void Refresh()
    {
        if (!TryResolveFlagId(out string resolvedFlagId))
            return;

        bool shouldDisable = Game.IsReady
            && Game.Ctx?.SaveState != null
            && Game.Ctx.SaveState.HasWorldState(resolvedFlagId);

        ApplyToTargets(!shouldDisable);
    }

    private void ApplyToTargets(bool isActive)
    {
        bool applied = false;

        if (targets != null)
        {
            foreach (GameObject currentTarget in targets)
            {
                if (currentTarget == null)
                    continue;

                currentTarget.SetActive(isActive);
                applied = true;
            }
        }

        if (target != null)
        {
            target.SetActive(isActive);
            applied = true;
        }

        if (!applied)
            gameObject.SetActive(isActive);
    }

    private bool TryResolveFlagId(out string resolvedFlagId)
    {
        resolvedFlagId = string.IsNullOrWhiteSpace(flagId) ? string.Empty : flagId.Trim();

        if (resolvedFlagId.Length == 0 && persistentId != null)
            persistentId.TryGetId(out resolvedFlagId);

        if (resolvedFlagId.Length > 0)
            return true;

        Debug.LogWarning($"{name}: save flag id is empty.", this);
        return false;
    }
}
