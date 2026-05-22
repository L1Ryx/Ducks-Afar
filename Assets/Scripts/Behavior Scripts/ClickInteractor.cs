using UnityEngine;
using UnityEngine.InputSystem;

public class ClickInteractor : MonoBehaviour
{
    [SerializeField] private MouseInteractTargeter2D targeter;

    private void Reset()
    {
        targeter = GetComponent<MouseInteractTargeter2D>();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (PauseUtility.IsPaused) return;
        TryInteract();
    }

    private void TryInteract()
    {
        var target = targeter != null ? targeter.CurrentTarget : null;
        if (target == null) return;
        if (PauseUtility.IsPaused) return;
        if (Game.Ctx.InteractionLock.IsLocked)
        {
            return;
        }
        target.Interact(gameObject);
    }
}
