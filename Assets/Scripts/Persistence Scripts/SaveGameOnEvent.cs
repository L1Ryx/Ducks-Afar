using UnityEngine;

public sealed class SaveGameOnEvent : MonoBehaviour
{
    [Header("Game Events")]
    [SerializeField] private GameEvent triggerEvent;
    [SerializeField] private GameEvent savedEvent;

    private void OnEnable()
    {
        if (triggerEvent != null)
            triggerEvent.RegisterRuntimeListener(Save);
    }

    private void OnDisable()
    {
        if (triggerEvent != null)
            triggerEvent.UnregisterRuntimeListener(Save);
    }

    public void Save()
    {
        if (!Game.IsReady || Game.Ctx?.Saves == null)
        {
            Debug.LogWarning($"{name}: cannot save current game because GameContext is not ready.", this);
            return;
        }

        if (Game.Ctx.Saves.SaveCurrentGameToActiveSlot())
            savedEvent?.Raise();
    }
}
