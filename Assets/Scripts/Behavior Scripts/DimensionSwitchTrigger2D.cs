using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class DimensionSwitchTrigger2D : MonoBehaviour
{
    [Header("Switch")]
    [SerializeField] private DimensionWorldGridSwitcher switcher;
    [SerializeField] private DimensionSwitchAction action = DimensionSwitchAction.Toggle;
    [SerializeField] private string playerTag = "Player";

    [Header("One-Shot Save")]
    [SerializeField] private bool oneShot = true;
    [SerializeField] private PersistentId persistentId;
    [SerializeField] private string triggeredFlagId;
    [SerializeField] private bool saveActiveSlotOnTrigger = true;
    [SerializeField] private bool disableAfterTriggered = true;

    [Header("Events")]
    [SerializeField] private GameEvent triggeredEvent;
    [SerializeField] private GameEvent alreadyTriggeredEvent;

    private Collider2D triggerCollider;
    private bool hasTriggeredThisSession;

    private void Reset()
    {
        triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;
        persistentId = GetComponent<PersistentId>();
    }

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;

        if (persistentId == null)
            persistentId = GetComponent<PersistentId>();
    }

    private void Start()
    {
        if (oneShot && WasTriggeredInSave())
            ApplyAlreadyTriggeredState();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || !other.CompareTag(playerTag))
            return;

        TryTrigger();
    }

    public void TryTrigger()
    {
        if (oneShot && (hasTriggeredThisSession || WasTriggeredInSave()))
        {
            alreadyTriggeredEvent?.Raise();
            ApplyAlreadyTriggeredState();
            return;
        }

        if (switcher == null)
            switcher = FindAnyObjectByType<DimensionWorldGridSwitcher>();

        if (switcher == null)
        {
            Debug.LogWarning($"{nameof(DimensionSwitchTrigger2D)} '{name}' has no switcher.", this);
            return;
        }

        hasTriggeredThisSession = true;
        switcher.ApplyAction(action);
        MarkTriggeredInSave();
        triggeredEvent?.Raise();

        if (disableAfterTriggered)
            gameObject.SetActive(false);
    }

    private void MarkTriggeredInSave()
    {
        if (!oneShot || !TryResolveTriggeredFlagId(out string flagId))
            return;

        if (!Game.IsReady || Game.Ctx?.SaveState == null)
            return;

        bool changed = Game.Ctx.SaveState.SetWorldState(flagId);
        if (changed && saveActiveSlotOnTrigger)
            DimensionWorldGridSwitcher.SaveActiveSlotCapturingPosition();
    }

    private bool WasTriggeredInSave()
    {
        return oneShot
            && TryResolveTriggeredFlagId(out string flagId)
            && Game.IsReady
            && Game.Ctx?.SaveState != null
            && Game.Ctx.SaveState.HasWorldState(flagId);
    }

    private void ApplyAlreadyTriggeredState()
    {
        hasTriggeredThisSession = true;

        if (disableAfterTriggered)
            gameObject.SetActive(false);
    }

    private bool TryResolveTriggeredFlagId(out string flagId)
    {
        flagId = string.IsNullOrWhiteSpace(triggeredFlagId) ? string.Empty : triggeredFlagId.Trim();

        if (flagId.Length == 0 && persistentId != null)
            persistentId.TryGetId(out flagId);

        return flagId.Length > 0;
    }
}
