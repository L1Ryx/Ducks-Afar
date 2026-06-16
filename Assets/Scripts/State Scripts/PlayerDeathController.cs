using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class PlayerDeathController : MonoBehaviour
{
    private bool isDying;
    private bool ownsInteractionLock;

    public bool IsDying => isDying;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnlockPlayer();
    }

    public bool KillPlayer(GameObject player, GameObject source = null)
    {
        if (isDying)
            return false;

        if (!Game.IsReady || Game.Ctx == null)
            return false;

        isDying = true;
        LockPlayer();
        StopPlayer(player);

        if (!TryForceRestart())
        {
            UnlockPlayer();
            isDying = false;
            return false;
        }

        StopEnemy(source);
        return true;
    }

    private void LockPlayer()
    {
        if (Game.Ctx?.InteractionLock == null)
            return;

        Game.Ctx.InteractionLock.Acquire();
        ownsInteractionLock = true;
    }

    private void UnlockPlayer()
    {
        if (!ownsInteractionLock || Game.Ctx?.InteractionLock == null)
            return;

        Game.Ctx.InteractionLock.Release();
        ownsInteractionLock = false;
    }

    private static void StopPlayer(GameObject player)
    {
        if (player != null && player.TryGetComponent(out Rigidbody2D body))
            body.linearVelocity = Vector2.zero;
    }

    private static void StopEnemy(GameObject source)
    {
        if (source == null)
            return;

        if (source.TryGetComponent(out CameraRoomAStarChaser2D chaser))
            chaser.FreezeMovement();

        if (source.TryGetComponent(out Rigidbody2D body))
            body.linearVelocity = Vector2.zero;
    }

    private bool TryForceRestart()
    {
        HoldToResetController reset = GetComponent<HoldToResetController>();
        if (reset == null)
            reset = Game.Ctx.GetComponent<HoldToResetController>();

        return reset != null && reset.ForceReset();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UnlockPlayer();
        isDying = false;
    }
}
