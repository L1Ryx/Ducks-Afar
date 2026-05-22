using UnityEngine;

public class MouseInteractTargeter2D : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private LayerMask interactableMask;
    [SerializeField] private float interactRadius = 1.25f;

    [Header("Debug")]
    [SerializeField] private bool drawDebug = false;

    // Click target (only valid when in range)
    private IInteractable currentTarget;

    // Hovered object (regardless of range)
    private IProximityHighlightable currentHighlight;
    private IHoverInfoUI currentHoverUI;

    // Track range state for hovered object
    private bool currentHoverInRange;
    private bool wasLocked;

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    private void Update()
    {
        if (PauseUtility.IsPaused)
        {
            if (!wasLocked) ClearHover();
            wasLocked = true;
            return;
        }

        bool locked = Game.IsReady && Game.Ctx?.InteractionLock?.IsLocked == true;

        if (locked)
        {
            if (!wasLocked) ClearHover();
            wasLocked = true;
            return;
        }

        wasLocked = false;
        UpdateHoverTarget();
    }
    
    private void ClearHover()
    {
        // Turn off highlight
        currentHighlight?.SetIsClosest(false);

        // Hide the hover panel (important)
        currentHoverUI?.SetHoverState(false, false);

        // Clear cached refs/state
        currentHighlight = null;
        currentHoverUI = null;
        currentTarget = null;
        currentHoverInRange = false;
    }


    public IInteractable CurrentTarget => currentTarget;

    private void UpdateHoverTarget()
    {
        if (worldCamera == null)
            return;

        // Convert mouse position to world
        Vector3 mouseWorld3 = worldCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mouseWorld = new Vector2(mouseWorld3.x, mouseWorld3.y);

        // Find collider under cursor (hover is NOT range-gated)
        Collider2D hit = Physics2D.OverlapPoint(mouseWorld, interactableMask);

        // Next hovered references
        IProximityHighlightable nextHighlight = null;
        IHoverInfoUI nextHoverUI = null;

        // Next click target references
        IInteractable nextTarget = null;

        bool nextInRange = false;

        if (hit != null)
        {
            // Hovered object: we can still show UI even if too far.
            nextHighlight = hit.GetComponentInParent<IProximityHighlightable>();
            nextHoverUI = hit.GetComponentInParent<IHoverInfoUI>();

            // Range check determines whether click is valid.
            Vector2 p = transform.position;
            Vector2 closest = hit.ClosestPoint(p);
            float distSq = (closest - p).sqrMagnitude;
            nextInRange = distSq <= interactRadius * interactRadius;

            if (nextInRange)
            {
                // Only set CurrentTarget when in range
                var candidate = hit.GetComponentInParent<IInteractable>();
                if (candidate != null)
                    nextTarget = candidate;
            }
        }

        // If hovered object OR hover range state changed, update presentation hooks
        bool hoverChanged = nextHighlight != currentHighlight || nextHoverUI != currentHoverUI;
        bool rangeChanged = nextInRange != currentHoverInRange;

        if (hoverChanged || rangeChanged)
        {
            // Turn off old hover effects
            if (currentHighlight != null && hoverChanged)
                currentHighlight.SetIsClosest(false);

            if (currentHoverUI != null)
                currentHoverUI.SetHoverState(false, false);

            // Apply new hover effects
            if (nextHighlight != null)
                nextHighlight.SetIsClosest(true);

            if (nextHoverUI != null)
                nextHoverUI.SetHoverState(true, nextInRange);

            currentHighlight = nextHighlight;
            currentHoverUI = nextHoverUI;
            currentHoverInRange = nextInRange;
        }

        // Update click target (can change even if hover didn’t)
        currentTarget = nextTarget;

        if (drawDebug)
        {
            string hoveredName = hit != null ? hit.GetComponentInParent<MonoBehaviour>()?.name : "none";
            Debug.Log($"Hover: {hoveredName}, InRange: {nextInRange}, ClickTarget: {(currentTarget != null ? ((MonoBehaviour)currentTarget).name : "none")}");
        }
    }

    private void OnDisable()
    {
        currentHighlight?.SetIsClosest(false);
        currentHoverUI?.SetHoverState(false, false);

        currentHighlight = null;
        currentHoverUI = null;
        currentTarget = null;
        currentHoverInRange = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
