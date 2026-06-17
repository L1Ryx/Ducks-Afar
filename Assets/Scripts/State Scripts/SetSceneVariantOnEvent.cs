using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SetSceneVariantOnEvent : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private string sceneName;
    [SerializeField] private SceneVariantSequenceSO sequence;
    [SerializeField] private SceneVariantDataSO variant;

    [Header("Saving")]
    [SerializeField] private bool saveActiveSlotOnChange = true;
    [SerializeField] private GameEvent savedEvent;

    [Header("Game Events")]
    [SerializeField] private GameEvent triggerEvent;
    [SerializeField] private GameEvent changedEvent;

    private void OnEnable()
    {
        triggerEvent?.RegisterRuntimeListener(SetVariant);
    }

    private void OnDisable()
    {
        triggerEvent?.UnregisterRuntimeListener(SetVariant);
    }

    public void SetVariant()
    {
        if (variant == null || variant.VariantId.Length == 0)
        {
            Debug.LogWarning($"{name}: cannot set an empty scene variant.", this);
            return;
        }

        if (!Game.IsReady || Game.Ctx?.SaveState == null)
        {
            Debug.LogWarning($"{name}: cannot set scene variant because GameContext is not ready.", this);
            return;
        }

        string resolvedSceneName = ResolveSceneName();
        if (resolvedSceneName.Length == 0)
        {
            Debug.LogWarning($"{name}: cannot set scene variant because scene name is empty.", this);
            return;
        }

        bool changed = Game.Ctx.SaveState.SetSceneVariant(resolvedSceneName, variant.VariantId);
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
