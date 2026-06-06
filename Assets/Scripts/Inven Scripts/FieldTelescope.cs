using UnityEngine;
using UnityEngine.Events;

public enum FieldTelescopeRevealEndMode
{
    Timed,
    UntilPlayerMoves
}

public class FieldTelescope : MonoBehaviour, IInteractable
{
    [Header("Reveal")]
    [SerializeField] private Sprite visualDotSprite;
    [SerializeField] private FieldTelescopeRevealEndMode revealEndMode = FieldTelescopeRevealEndMode.UntilPlayerMoves;
    [Min(0.1f)] [SerializeField] private float revealDuration = 2.5f;
    [SerializeField] private GameEvent playerMovedEvent;

    [Header("Events (Optional)")]
    [SerializeField] private UnityEvent onUsed;

    public Sprite VisualDotSprite => visualDotSprite;
    public FieldTelescopeRevealEndMode RevealEndMode => revealEndMode;
    public float RevealDuration => revealDuration;
    public GameEvent PlayerMovedEvent => playerMovedEvent;

    public void Interact(GameObject interactor)
    {
        ProjectAudio.PlayOn(ProjectAudio.Config != null ? ProjectAudio.Config.TelescopeOpenCue : null, gameObject);
        FieldTelescopeVisionSystem.Reveal(visualDotSprite, revealDuration, revealEndMode, playerMovedEvent, gameObject);
        onUsed?.Invoke();
    }
}
