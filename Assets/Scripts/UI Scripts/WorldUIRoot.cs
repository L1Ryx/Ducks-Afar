using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class WorldUIRoot : MonoBehaviour
{
    [Header("Input (Input Action Asset)")]
    [SerializeField] private InputActionReference leftClickAction;

    [Header("Events")]
    [SerializeField] private UnityEvent OnLeftClickPress;

    private void OnEnable()
    {
        if (leftClickAction == null || leftClickAction.action == null)
        {
            Debug.LogError($"{nameof(WorldUIRoot)}: LeftClick InputActionReference is not assigned.", this);
            return;
        }
        
        leftClickAction.action.Enable();
        leftClickAction.action.performed += HandleLeftClick;
    }

    private void OnDisable()
    {
        if (leftClickAction == null || leftClickAction.action == null) return;

        leftClickAction.action.performed -= HandleLeftClick;
    }

    private void HandleLeftClick(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (PauseUtility.IsPaused) return;
        if (Game.Ctx.InteractionLock.IsLocked) return;

        OnLeftClickPress?.Invoke();
    }
}
