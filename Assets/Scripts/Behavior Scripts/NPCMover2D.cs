using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public sealed class NPCMover2D : MonoBehaviour
{
    [Header("Important")] [SerializeField] private bool shouldMove = false;
    [Header("References")]
    [Tooltip("Child object that owns the SpriteRenderer + Animator.")]
    [SerializeField] private Transform visualRoot;

    [Tooltip("Animator on the visualRoot (optional; auto-found if null).")]
    [SerializeField] private Animator animator;

    [Tooltip("Waypoint transform to move to when MoveToDestination() is called.")]
    [SerializeField] private Transform destination;

    [Header("Movement")]
    [SerializeField, Min(0.01f)] private float moveSpeed = 0.75f;
    [SerializeField, Min(0f)] private float arriveDistance = 0.02f;

    [Header("Animator Params")]
    [Tooltip("Bool parameter that is true when moving.")]
    [SerializeField] private string isMovingParam = "IsMoving";

    [Header("Events")] [SerializeField] private UnityEvent onNPCDestinationReached;

    private Coroutine moveRoutine;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        if (animator == null)
            animator = visualRoot.GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// Moves the NPC root to the configured destination transform.
    /// </summary>
    public void MoveToDestination()
    {
        if (!shouldMove)
        {
            return;
        }
        if (destination == null)
        {
            Debug.LogWarning($"{name}: No destination assigned.");
            return;
        }

        MoveTo(destination.position);
    }

    /// <summary>
    /// Moves the NPC root to a world position.
    /// </summary>
    public void MoveTo(Vector3 worldPosition)
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveRoutine(worldPosition));
    }

    public void StopMoving()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        SetMoving(false);
    }

    private IEnumerator MoveRoutine(Vector3 target)
    {
        SetMoving(true);

        // Move the NPC root; visualRoot stays as a child.
        while (Vector2.Distance(transform.position, target) > arriveDistance)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                target,
                moveSpeed * Time.deltaTime
            );

            // Optional facing logic (if you want it):
            // FaceTarget(target);

            yield return null;
        }

        transform.position = target;
        onNPCDestinationReached?.Invoke();

        SetMoving(false);
        moveRoutine = null;
    }

    private void SetMoving(bool moving)
    {
        if (animator != null && !string.IsNullOrWhiteSpace(isMovingParam))
            animator.SetBool(isMovingParam, moving);
    }

    // Optional: if your sprites need flipping.
    // private void FaceTarget(Vector3 target)
    // {
    //     if (visualRoot == null) return;
    //     Vector3 scale = visualRoot.localScale;
    //     scale.x = Mathf.Abs(scale.x) * (target.x >= transform.position.x ? 1f : -1f);
    //     visualRoot.localScale = scale;
    // }
}
