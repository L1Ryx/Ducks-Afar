using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class CheckpointSaver : MonoBehaviour
{
    [Header("Save Data")]
    [SerializeField] private string locationName;

    [Header("Behavior")]
    [SerializeField] private bool saveImmediately = true;

    /// <summary>
    /// Sets the current runtime save location only.
    /// Does not write to disk.
    /// </summary>
    public void SetLocationOnly()
    {
        if (!Game.IsReady || Game.Ctx == null)
        {
            Debug.LogWarning("CheckpointSaver.SetLocationOnly failed: GameContext is not ready.");
            return;
        }

        if (string.IsNullOrWhiteSpace(locationName))
        {
            Debug.LogWarning($"{name}: CheckpointSaver has no locationName assigned.");
            return;
        }

        StampCurrentSceneAndLocation();
        Debug.Log($"{name}: Set runtime save location -> {locationName}");
    }

    /// <summary>
    /// Sets the runtime save location and immediately saves to the active slot.
    /// </summary>
    public void SetLocationAndSave()
    {
        if (!Game.IsReady || Game.Ctx == null)
        {
            Debug.LogWarning("CheckpointSaver.SetLocationAndSave failed: GameContext is not ready.");
            return;
        }

        if (string.IsNullOrWhiteSpace(locationName))
        {
            Debug.LogWarning($"{name}: CheckpointSaver has no locationName assigned.");
            return;
        }

        StampCurrentSceneAndLocation();
        Debug.Log($"{name}: Set runtime save location -> {locationName}");

        Game.Ctx.Saves.SaveToActiveSlot();
    }

    /// <summary>
    /// Convenience entry point for event listeners.
    /// Uses the inspector flag to decide whether to save immediately.
    /// </summary>
    public void TriggerCheckpoint()
    {
        if (saveImmediately)
        {
            SetLocationAndSave();
            return;
        }

        SetLocationOnly();
    }

    /// <summary>
    /// Optional helper for manual testing from other scripts.
    /// </summary>
    public void SetLocationName(string newLocationName)
    {
        locationName = newLocationName;
    }

    /// <summary>
    /// Optional getter for debugging/inspection.
    /// </summary>
    public string GetLocationName()
    {
        return locationName;
    }

    private void StampCurrentSceneAndLocation()
    {
        Game.Ctx.SaveState.CurrentSceneName = SceneManager.GetActiveScene().name;
        Game.Ctx.SaveState.CurrentLocation = locationName;
    }
}
