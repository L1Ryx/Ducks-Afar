using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PauseInputController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference pauseAction;
    [SerializeField] private bool useEscapeFallback = true;

    private void OnEnable()
    {
        if (pauseAction == null || pauseAction.action == null)
            return;

        pauseAction.action.Enable();
        pauseAction.action.performed += HandlePausePerformed;
    }

    private void OnDisable()
    {
        if (pauseAction == null || pauseAction.action == null)
            return;

        pauseAction.action.performed -= HandlePausePerformed;
    }

    private void Update()
    {
        if (useEscapeFallback && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();
    }

    private void HandlePausePerformed(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        TogglePause();
    }

    public void TogglePause()
    {
        if (!Game.IsReady || Game.Ctx?.Pause == null)
            return;

        Game.Ctx.Pause.Toggle();
    }
}
