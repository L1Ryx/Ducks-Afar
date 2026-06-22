using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneVariantController : MonoBehaviour
{
    [Header("Variant Data")]
    [SerializeField] private SceneVariantSequenceSO sequence;
    [SerializeField] private string sceneNameOverride;
    [SerializeField] private Transform groupRoot;

#if UNITY_EDITOR
    [Header("Editor Testing")]
    [Tooltip("Editor-only. Forces this variant in Play Mode, even if no save is loaded or a save has another variant.")]
    [SerializeField] private bool useEditorVariantOverride = false;
    [SerializeField] private SceneVariantDataSO editorVariantOverride;
#endif

    [Header("Behavior")]
    [SerializeField] private bool applyOnStart = true;
    [SerializeField] private bool repeatLastVariantWhenAdvancing = true;
    [SerializeField] private bool trackFirstAppliedVariants = true;

    [Header("Saving")]
    [SerializeField] private bool saveActiveSlotWhenVariantChanges = true;
    [SerializeField] private bool saveActiveSlotWhenFirstApplied = true;
    [SerializeField] private GameEvent savedEvent;

    [Header("Events")]
    [SerializeField] private GameEvent variantAppliedEvent;
    [SerializeField] private GameEvent variantChangedEvent;

    public SceneVariantDataSO CurrentVariant { get; private set; }
    public string CurrentVariantId => CurrentVariant != null ? CurrentVariant.VariantId : string.Empty;
    public bool HasAppliedCurrentVariant { get; private set; }
    public bool LastApplyWasFirstApplication { get; private set; }
    public bool TracksFirstAppliedVariants => trackFirstAppliedVariants;

    private Coroutine startRoutine;

    private void OnEnable()
    {
        if (applyOnStart && sequence != null)
            startRoutine = StartCoroutine(ApplyWhenReady());
    }

    private void OnDisable()
    {
        if (startRoutine != null)
        {
            StopCoroutine(startRoutine);
            startRoutine = null;
        }
    }

    public void ApplyCurrentVariant()
    {
        if (sequence == null)
            return;

        SceneVariantDataSO activeVariant = ResolveCurrentVariant();
        if (activeVariant == null)
        {
            Debug.LogWarning($"{name}: scene variant sequence '{sequence.name}' has no usable variants.", this);
            return;
        }

        CurrentVariant = activeVariant;
        ApplyGroups(activeVariant);
        RaiseEvents(activeVariant.OnAppliedEvents);
        LastApplyWasFirstApplication = RaiseFirstAppliedEventsIfNeeded(activeVariant);
        HasAppliedCurrentVariant = true;
        variantAppliedEvent?.Raise();
    }

    public void AdvanceVariant()
    {
        if (sequence == null)
        {
            Debug.LogWarning($"{name}: cannot advance scene variant because no sequence is assigned.", this);
            return;
        }

        string sceneName = ResolveSceneName();
        string currentVariantId = CurrentVariantId;

        if (Game.IsReady
            && Game.Ctx?.SaveState != null
            && Game.Ctx.SaveState.TryGetSceneVariant(sceneName, out string savedVariantId))
        {
            currentVariantId = savedVariantId;
        }

        if (!sequence.TryGetNextVariant(
                currentVariantId,
                repeatLastVariantWhenAdvancing,
                out SceneVariantDataSO nextVariant))
        {
            Debug.LogWarning($"{name}: no next scene variant exists after '{currentVariantId}'.", this);
            return;
        }

        SetVariant(nextVariant);
    }

    public void SetVariant(SceneVariantDataSO variant)
    {
        if (variant == null || variant.VariantId.Length == 0)
        {
            Debug.LogWarning($"{name}: cannot set an empty scene variant.", this);
            return;
        }

        string sceneName = ResolveSceneName();
        bool changed = false;

        if (Game.IsReady && Game.Ctx?.SaveState != null)
            changed = Game.Ctx.SaveState.SetSceneVariant(sceneName, variant.VariantId);

        CurrentVariant = variant;
        ApplyGroups(variant);
        RaiseEvents(variant.OnAppliedEvents);
        LastApplyWasFirstApplication = RaiseFirstAppliedEventsIfNeeded(variant);
        HasAppliedCurrentVariant = true;
        variantAppliedEvent?.Raise();

        if (changed)
        {
            variantChangedEvent?.Raise();
            SaveActiveSlotIfWanted(saveActiveSlotWhenVariantChanges);
        }
    }

    private IEnumerator ApplyWhenReady()
    {
        while (!Game.IsReady || Game.Ctx?.SaveState == null)
            yield return null;

        ApplyCurrentVariant();
        startRoutine = null;
    }

    private SceneVariantDataSO ResolveCurrentVariant()
    {
#if UNITY_EDITOR
        if (useEditorVariantOverride
            && editorVariantOverride != null
            && editorVariantOverride.VariantId.Length > 0)
        {
            return editorVariantOverride;
        }
#endif

        string sceneName = ResolveSceneName();

        if (Game.IsReady
            && Game.Ctx?.SaveState != null
            && Game.Ctx.SaveState.TryGetSceneVariant(sceneName, out string savedVariantId))
        {
            return sequence.ResolveVariant(savedVariantId);
        }

        return sequence.FirstVariant;
    }

    private void ApplyGroups(SceneVariantDataSO activeVariant)
    {
        SceneVariantGroup[] groups = groupRoot != null
            ? groupRoot.GetComponentsInChildren<SceneVariantGroup>(true)
            : Object.FindObjectsByType<SceneVariantGroup>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        foreach (SceneVariantGroup group in groups)
            group.Apply(activeVariant);
    }

    private bool RaiseFirstAppliedEventsIfNeeded(SceneVariantDataSO variant)
    {
        if (!trackFirstAppliedVariants || !Game.IsReady || Game.Ctx?.SaveState == null)
            return false;

        string flagId = BuildSeenFlagId(ResolveSceneName(), variant.VariantId);
        if (Game.Ctx.SaveState.HasWorldState(flagId))
            return false;

        RaiseEvents(variant.OnFirstAppliedEvents);
        Game.Ctx.SaveState.SetWorldState(flagId);
        SaveActiveSlotIfWanted(saveActiveSlotWhenFirstApplied);
        return true;
    }

    private string ResolveSceneName()
    {
        string sceneName = SceneVariantDataSO.Normalize(sceneNameOverride);
        if (sceneName.Length > 0)
            return sceneName;

        sceneName = sequence != null ? sequence.SceneName : string.Empty;
        if (sceneName.Length > 0)
            return sceneName;

        Scene activeScene = SceneManager.GetActiveScene();
        return activeScene.IsValid() ? activeScene.name : string.Empty;
    }

    private void SaveActiveSlotIfWanted(bool shouldSave)
    {
        if (!shouldSave
            || !Game.IsReady
            || Game.Ctx?.Saves == null
            || Game.Ctx.SaveState == null
            || !Game.Ctx.SaveState.HasActiveSlot)
        {
            return;
        }

        if (Game.Ctx.Saves.SaveToActiveSlot(captureSceneCheckpoint: false))
            savedEvent?.Raise();
    }

    private static void RaiseEvents(System.Collections.Generic.IReadOnlyList<GameEvent> events)
    {
        if (events == null)
            return;

        foreach (GameEvent gameEvent in events)
            gameEvent?.Raise();
    }

    private static string BuildSeenFlagId(string sceneName, string variantId)
    {
        return $"scene_variant_seen:{sceneName}:{variantId}";
    }
}
