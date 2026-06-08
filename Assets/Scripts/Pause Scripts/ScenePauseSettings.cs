using UnityEngine;

public sealed class ScenePauseSettings : MonoBehaviour
{
    [SerializeField] private bool isPausable = true;
    [SerializeField] private bool allowHoldToRestart = false;
    [TextArea] [SerializeField] private string reason;

    public bool IsPausable => isPausable;
    public bool AllowHoldToRestart => allowHoldToRestart;
    public string Reason => reason;

    private void OnEnable()
    {
        Apply();
    }

    public void Apply()
    {
        if (!Game.IsReady || Game.Ctx?.Pause == null)
            return;

        Game.Ctx.Pause.SetScenePausable(isPausable, reason);

        if (Game.Ctx.TryGetComponent(out HoldToResetController holdToReset))
            holdToReset.SetSceneRestartAllowed(allowHoldToRestart);
    }
}
