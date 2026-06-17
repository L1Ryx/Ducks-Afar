using UnityEngine;

public sealed class SceneSettings : MonoBehaviour
{
    [Header("Pause")]
    [SerializeField] private bool isPausable = true;
    [SerializeField] private bool allowHoldToRestart = false;
    [TextArea] [SerializeField] private string reason;

    [Header("Currency UI")]
    [SerializeField] private bool allowGoldwormCurrencyUI = false;

    public bool IsPausable => isPausable;
    public bool AllowHoldToRestart => allowHoldToRestart;
    public string Reason => reason;
    public bool AllowGoldwormCurrencyUI => allowGoldwormCurrencyUI;

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
