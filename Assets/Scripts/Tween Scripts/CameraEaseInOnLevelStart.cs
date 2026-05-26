using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

[RequireComponent(typeof(Camera))]
public class CameraEaseInOnLevelStart : MonoBehaviour
{
    [Header("Position Ease")]
    [SerializeField] private bool easePosition = true;
    [SerializeField] private Vector3 startOffset = new Vector3(0f, 0.25f, 0f);

    [Header("Ortho Zoom Ease")]
    [SerializeField] private bool easeOrthoSize = true;
    [SerializeField] private float orthoSizeOffset = 0.35f;

    [Header("Timing")]
    [SerializeField] private float duration = 0.9f;
    [SerializeField] private float startDelay = 0.0f;
    [SerializeField] private Ease ease = Ease.OutCubic;
    [SerializeField] private bool useUnscaledTime = true;
    
    [Header("Events")]
    [SerializeField] private UnityEvent onPanComplete;

    [SerializeField] private UnityEvent onUnlockInteractions;

    private Camera cam;
    private Tween posTween;
    private Tween sizeTween;
    private Sequence seq;

    private Vector3 targetPos;
    private float targetOrthoSize;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        targetPos = transform.position;
        targetOrthoSize = cam.orthographicSize;
    }

    private void OnDisable()
    {
        KillTweens();
    }

    private void Update()
    {
        ApplyPauseState(PauseUtility.IsPaused);
    }

    // Hook this to LevelStartedEvent via GameEventListener.Response
    public void Play()
    {
        posTween?.Kill();
        sizeTween?.Kill();
        seq?.Kill();

        // Reset targets in case something moved camera pre-start (e.g., spawn system).
        targetPos = transform.position;
        targetOrthoSize = cam.orthographicSize;

        bool willTweenPos = easePosition;
        bool willTweenSize = easeOrthoSize && cam.orthographic;

        // If nothing will tween, consider the "pan" complete immediately.
        if (!willTweenPos && !willTweenSize)
        {
            onPanComplete?.Invoke();
            return;
        }

        // Build a single sequence so completion is deterministic.
        seq = DOTween.Sequence()
            .SetDelay(startDelay)
            .SetEase(ease)
            .SetUpdate(useUnscaledTime)
            .OnComplete(() => onPanComplete?.Invoke());

        if (willTweenPos)
        {
            transform.position = targetPos + startOffset;

            posTween = transform.DOMove(targetPos, duration);

            // Join so both tweens run concurrently and the sequence completes after both.
            seq.Join(posTween);
        }

        if (willTweenSize)
        {
            cam.orthographicSize = targetOrthoSize + orthoSizeOffset;

            sizeTween = cam.DOOrthoSize(targetOrthoSize, duration);

            seq.Join(sizeTween);
        }

        ApplyPauseState(PauseUtility.IsPaused);
    }

    public void UnlockInteractions()
    {
        onUnlockInteractions?.Invoke();
    }

    public void SnapToFinishedState()
    {
        if (cam == null)
            cam = GetComponent<Camera>();

        KillTweens();

        transform.position = targetPos;

        if (cam.orthographic)
            cam.orthographicSize = targetOrthoSize;
    }

    private void ApplyPauseState(bool paused)
    {
        if (seq == null || !seq.IsActive())
            return;

        if (paused && seq.IsPlaying())
        {
            seq.Pause();
            return;
        }

        if (!paused && !seq.IsPlaying())
            seq.Play();
    }

    private void KillTweens()
    {
        posTween?.Kill();
        sizeTween?.Kill();
        seq?.Kill();

        posTween = null;
        sizeTween = null;
        seq = null;
    }
}
