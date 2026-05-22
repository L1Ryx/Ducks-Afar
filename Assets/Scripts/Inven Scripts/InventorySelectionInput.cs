using UnityEngine;
using UnityEngine.InputSystem;

public class InventorySelectionInput : MonoBehaviour
{
    public void OnCycleNext(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (PauseUtility.IsPaused) return;
        if (!Game.IsReady || Game.Ctx.InventorySelection == null) return;

        Game.Ctx.InventorySelection.CycleNext();
    }
}
