using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class AdvanceSceneVariantOnEvent : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private string sceneName;
    [SerializeField] private SceneVariantSequenceSO sequence;

    [Header("Behavior")]
    [SerializeField] private bool repeatLastVariantWhenAdvancing = true;

    [Header("Saving")]
    [SerializeField] private bool saveActiveSlotOnChange = true;
    [SerializeField] private GameEvent savedEvent;

    [Header("Game Events")]
    [SerializeField] private GameEvent triggerEvent;
    [SerializeField] private GameEvent changedEvent;

    private void OnEnable()
    {
        triggerEvent?.RegisterRuntimeListener(AdvanceVariant);
    }

    private void OnDisable()
    {
        triggerEvent?.UnregisterRuntimeListener(AdvanceVariant);
    }

    public void AdvanceVariant()
    {
        if (sequence == null)
        {
            Debug.LogWarning($"{name}: cannot advance scene variant because no sequence is assigned.", this);
            return;
        }

        if (!Game.IsReady || Game.Ctx?.SaveState == null)
        {
            Debug.LogWarning($"{name}: cannot advance scene variant because GameContext is not ready.", this);
            return;
        }

        string resolvedSceneName = ResolveSceneName();
        string currentVariantId = string.Empty;
        Game.Ctx.SaveState.TryGetSceneVariant(resolvedSceneName, out currentVariantId);

        if (!sequence.TryGetNextVariant(
                currentVariantId,
                repeatLastVariantWhenAdvancing,
                out SceneVariantDataSO nextVariant))
        {
            Debug.LogWarning($"{name}: no next scene variant exists after '{currentVariantId}'.", this);
            return;
        }

        bool changed = Game.Ctx.SaveState.SetSceneVariant(resolvedSceneName, nextVariant.VariantId);
        if (!changed)
            return;

        changedEvent?.Raise();

        if (saveActiveSlotOnChange
            && Game.Ctx.SaveState.HasActiveSlot
            && Game.Ctx.Saves != null
            && Game.Ctx.Saves.SaveToActiveSlot(captureSceneCheckpoint: false))
        {
            savedEvent?.Raise();
        }
    }

    private string ResolveSceneName()
    {
        string resolvedSceneName = SceneVariantDataSO.Normalize(sceneName);
        if (resolvedSceneName.Length > 0)
            return resolvedSceneName;

        resolvedSceneName = sequence != null ? sequence.SceneName : string.Empty;
        if (resolvedSceneName.Length > 0)
            return resolvedSceneName;

        Scene activeScene = SceneManager.GetActiveScene();
        return activeScene.IsValid() ? activeScene.name : string.Empty;
    }
}
