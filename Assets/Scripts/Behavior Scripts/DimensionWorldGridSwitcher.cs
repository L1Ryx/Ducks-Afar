using UnityEngine;
using System.Collections;

public enum DimensionGridState
{
    Primary,
    Alternate
}

public enum DimensionSwitchAction
{
    Toggle,
    SwitchToPrimary,
    SwitchToAlternate
}

public sealed class DimensionWorldGridSwitcher : MonoBehaviour
{
    [Header("World Grids")]
    [SerializeField] private GameObject primaryWorldGrid;
    [SerializeField] private GameObject alternateWorldGrid;
    [SerializeField] private DimensionGridState defaultState = DimensionGridState.Primary;

    [Header("Saved State")]
    [Tooltip("If set, this world-state flag means the alternate grid is active.")]
    [SerializeField] private string alternateActiveFlagId;
    [SerializeField] private bool restoreFromSaveOnStart = true;
    [SerializeField] private bool saveActiveSlotOnSwitch = true;

    [Header("Transition")]
    [SerializeField] private GameEvent zoneTransitionEffectEvent;
    [SerializeField] private bool playTransitionEffect = true;
    [SerializeField, Min(0f)] private float switchDelaySeconds = 0.08f;

    [Header("Events")]
    [SerializeField] private GameEvent switchedEvent;
    [SerializeField] private GameEvent switchedToPrimaryEvent;
    [SerializeField] private GameEvent switchedToAlternateEvent;

    private DimensionGridState currentState;
    private Coroutine switchRoutine;

    public DimensionGridState CurrentState => currentState;
    public bool IsAlternateActive => currentState == DimensionGridState.Alternate;

    private void Start()
    {
        RefreshFromSave();
    }

    public void RefreshFromSave()
    {
        DimensionGridState state = defaultState;

        if (restoreFromSaveOnStart
            && !string.IsNullOrWhiteSpace(alternateActiveFlagId)
            && Game.IsReady
            && Game.Ctx?.SaveState != null
            && Game.Ctx.SaveState.HasWorldState(alternateActiveFlagId))
        {
            state = DimensionGridState.Alternate;
        }

        ApplyState(state, playEffects: false, saveState: false, force: true);
    }

    public void Toggle()
    {
        Switch(currentState == DimensionGridState.Primary
            ? DimensionGridState.Alternate
            : DimensionGridState.Primary);
    }

    public void SwitchToPrimary()
    {
        Switch(DimensionGridState.Primary);
    }

    public void SwitchToAlternate()
    {
        Switch(DimensionGridState.Alternate);
    }

    public void ApplyAction(DimensionSwitchAction action)
    {
        switch (action)
        {
            case DimensionSwitchAction.SwitchToPrimary:
                SwitchToPrimary();
                break;
            case DimensionSwitchAction.SwitchToAlternate:
                SwitchToAlternate();
                break;
            default:
                Toggle();
                break;
        }
    }

    public void Switch(DimensionGridState nextState)
    {
        if (currentState == nextState)
            return;

        if (switchRoutine != null)
            StopCoroutine(switchRoutine);

        if (playTransitionEffect && switchDelaySeconds > 0f)
        {
            switchRoutine = StartCoroutine(SwitchAfterEffect(nextState));
            return;
        }

        ApplyState(nextState, playTransitionEffect, saveActiveSlotOnSwitch, force: false);
    }

    private void ApplyState(DimensionGridState nextState, bool playEffects, bool saveState, bool force)
    {
        if (!force && currentState == nextState)
            return;

        if (playEffects)
            zoneTransitionEffectEvent?.Raise();

        currentState = nextState;
        ApplyWorldGrids();
        ApplySavedState(saveState);

        switchedEvent?.Raise();
        if (currentState == DimensionGridState.Alternate)
            switchedToAlternateEvent?.Raise();
        else
            switchedToPrimaryEvent?.Raise();
    }

    private IEnumerator SwitchAfterEffect(DimensionGridState nextState)
    {
        zoneTransitionEffectEvent?.Raise();
        yield return new WaitForSecondsRealtime(switchDelaySeconds);

        ApplyState(nextState, playEffects: false, saveActiveSlotOnSwitch, force: false);
        switchRoutine = null;
    }

    private void ApplyWorldGrids()
    {
        if (primaryWorldGrid != null)
            primaryWorldGrid.SetActive(currentState == DimensionGridState.Primary);

        if (alternateWorldGrid != null)
            alternateWorldGrid.SetActive(currentState == DimensionGridState.Alternate);
    }

    private void ApplySavedState(bool saveState)
    {
        if (string.IsNullOrWhiteSpace(alternateActiveFlagId)
            || !Game.IsReady
            || Game.Ctx?.SaveState == null)
        {
            return;
        }

        string flagId = alternateActiveFlagId.Trim();
        bool changed = currentState == DimensionGridState.Alternate
            ? Game.Ctx.SaveState.SetWorldState(flagId)
            : Game.Ctx.SaveState.ClearWorldState(flagId);

        if (!changed || !saveState)
            return;

        SaveActiveSlotWithoutCapturingPosition();
    }

    public static void SaveActiveSlotWithoutCapturingPosition()
    {
        if (!Game.IsReady || Game.Ctx?.Saves == null || Game.Ctx.SaveState == null)
            return;

        if (!Game.Ctx.SaveState.HasActiveSlot)
            return;

        Game.Ctx.Saves.SaveToActiveSlot(captureSceneCheckpoint: false);
    }
}
