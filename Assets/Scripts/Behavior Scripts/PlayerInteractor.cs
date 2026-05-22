using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactRadius = 1.25f;
    [SerializeField] private LayerMask interactableMask;

    [Header("Debug")]
    [SerializeField] private bool drawGizmo = true;

    // Called by PlayerInput (Invoke Unity Events) OR can be polled manually.
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (PauseUtility.IsPaused) return;
        TryInteract();
    }

    public bool TryInteract()
    {
        if (PauseUtility.IsPaused) return false;

        var hits = Physics2D.OverlapCircleAll(transform.position, interactRadius, interactableMask);
        if (hits == null || hits.Length == 0) return false;

        Collider2D best = null;
        float bestDistSq = float.PositiveInfinity;
        Vector2 p = transform.position;

        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (col == null) continue;

            // Must have an IInteractable somewhere on this object (or parent).
            var interactable = col.GetComponentInParent<IInteractable>();
            if (interactable == null) continue;

            // Use closest point on collider for better accuracy than transform distance.
            Vector2 closest = col.ClosestPoint(p);
            float dSq = (closest - p).sqrMagnitude;

            if (dSq < bestDistSq)
            {
                bestDistSq = dSq;
                best = col;
            }
        }

        if (best == null) return false;

        var target = best.GetComponentInParent<IInteractable>();
        target.Interact(gameObject);
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmo) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
