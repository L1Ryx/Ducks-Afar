using System;
using UnityEngine;

public sealed class PauseStateModel
{
    public bool IsPaused { get; private set; }
    public bool IsScenePausable { get; private set; }
    public string ScenePauseReason { get; private set; } = string.Empty;

    public event Action<bool> OnPauseChanged;
    public event Action<bool> OnScenePausableChanged;

    public bool CanPause => IsScenePausable;

    public void SetScenePausable(bool pausable, string reason = "")
    {
        bool changed = IsScenePausable != pausable;
        IsScenePausable = pausable;
        ScenePauseReason = reason ?? string.Empty;

        if (changed)
            OnScenePausableChanged?.Invoke(IsScenePausable);

        if (!IsScenePausable && IsPaused)
            Resume();
    }

    public bool TryPause()
    {
        if (!CanPause)
        {
            Debug.Log($"Pause ignored: current scene is not pausable. {ScenePauseReason}");
            return false;
        }

        Pause();
        return true;
    }

    public void Pause()
    {
        if (IsPaused)
            return;

        IsPaused = true;
        OnPauseChanged?.Invoke(true);
    }

    public void Resume()
    {
        if (!IsPaused)
            return;

        IsPaused = false;
        OnPauseChanged?.Invoke(false);
    }

    public bool Toggle()
    {
        if (IsPaused)
        {
            Resume();
            return true;
        }

        return TryPause();
    }
}
