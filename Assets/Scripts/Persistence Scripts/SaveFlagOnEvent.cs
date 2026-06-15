using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum SaveFlagEventAction
{
    Set,
    Clear
}

public sealed class SaveFlagOnEvent : MonoBehaviour
{
    [Header("Flag")]
    [SerializeField] private PersistentId persistentId;
    [SerializeField] private string flagId;
    [SerializeField] private SaveFlagEventAction actionOnGameEvent = SaveFlagEventAction.Set;

    [Header("Saving")]
    [SerializeField] private bool saveActiveSlotOnChange = true;
    [SerializeField] private GameEvent savedEvent;

    [Header("Game Events")]
    [SerializeField] private GameEvent triggerEvent;
    [SerializeField] private GameEvent changedEvent;

    private void Awake()
    {
        if (persistentId == null)
            persistentId = GetComponent<PersistentId>();
    }

    private void OnEnable()
    {
        if (triggerEvent != null)
            triggerEvent.RegisterRuntimeListener(ApplyConfiguredAction);
    }

    private void OnDisable()
    {
        if (triggerEvent != null)
            triggerEvent.UnregisterRuntimeListener(ApplyConfiguredAction);
    }

    public void ApplyConfiguredAction()
    {
        if (actionOnGameEvent == SaveFlagEventAction.Clear)
            ClearFlag();
        else
            SetFlag();
    }

    public void SetFlag()
    {
        ApplyFlag(true);
    }

    public void ClearFlag()
    {
        ApplyFlag(false);
    }

    private void ApplyFlag(bool isSet)
    {
        if (!TryResolveFlagId(out string resolvedFlagId))
            return;

        if (!Game.IsReady || Game.Ctx?.SaveState == null)
        {
            Debug.LogWarning($"{name}: cannot update save flag '{resolvedFlagId}' because GameContext is not ready.", this);
            return;
        }

        bool changed = isSet
            ? Game.Ctx.SaveState.SetWorldState(resolvedFlagId)
            : Game.Ctx.SaveState.ClearWorldState(resolvedFlagId);

        if (!changed)
            return;

        changedEvent?.Raise();

        if (saveActiveSlotOnChange)
        {
            if (Game.Ctx.Saves != null && Game.Ctx.Saves.SaveCurrentGameToActiveSlot())
                savedEvent?.Raise();
        }
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

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (savedEvent != null)
            return;

        savedEvent = AssetDatabase.LoadAssetAtPath<GameEvent>("Assets/SOs/Events/OnGameSaved.asset");
    }
#endif
}
