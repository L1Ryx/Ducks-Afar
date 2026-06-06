using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class MenuButtonAudioTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISubmitHandler
{
    [SerializeField] private bool suppressAudio;

    private Button button;
    private MenuButtonAudioSystem audioSystem;
    private bool pointerInside;
    private bool pointerPressedHere;
    private bool suppressHoverExitUntilNextEnter;

    public void Initialize(MenuButtonAudioSystem owner, Button targetButton)
    {
        audioSystem = owner;
        button = targetButton != null ? targetButton : GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    private void OnDisable()
    {
        pointerInside = false;
        pointerPressedHere = false;
        suppressHoverExitUntilNextEnter = false;
    }

    private void OnDestroy()
    {
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (pointerInside)
            return;

        pointerInside = true;
        suppressHoverExitUntilNextEnter = false;
        PlayHoverBegin();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!pointerInside)
            return;

        pointerInside = false;

        if (suppressHoverExitUntilNextEnter)
            return;

        PlayHoverEnd();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerPressedHere = eventData.button == PointerEventData.InputButton.Left &&
                             pointerInside &&
                             audioSystem != null &&
                             audioSystem.CanPlayFor(button);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        bool shouldActivate = eventData.button == PointerEventData.InputButton.Left &&
                              pointerPressedHere &&
                              pointerInside;

        pointerPressedHere = false;

        if (!shouldActivate)
            return;

        PlaySelection();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        PlaySelection();
    }

    private void PlaySelection()
    {
        if (suppressAudio || audioSystem == null || !audioSystem.CanPlayFor(button))
            return;

        suppressHoverExitUntilNextEnter = true;
        audioSystem.PlaySelection(gameObject);
    }

    private void PlayHoverBegin()
    {
        if (suppressAudio || audioSystem == null || !audioSystem.CanPlayFor(button))
            return;

        audioSystem.PlayHoverBegin(gameObject);
    }

    private void PlayHoverEnd()
    {
        if (suppressAudio || audioSystem == null || !audioSystem.CanPlayFor(button))
            return;

        audioSystem.PlayHoverEnd(gameObject);
    }
}
