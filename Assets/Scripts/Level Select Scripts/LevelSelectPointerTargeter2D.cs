using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class LevelSelectPointerTargeter2D : MonoBehaviour
{
    private static readonly List<LevelSelectPointerTargeter2D> Instances = new();

    [SerializeField] private Camera worldCamera;
    [SerializeField] private LayerMask interactableMask;

    private IHoverInfoUI currentHoverUI;
    private IInteractable currentTarget;
    private static readonly List<RaycastResult> UiRaycastResults = new();

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    private void OnEnable()
    {
        if (!Instances.Contains(this))
            Instances.Add(this);
    }

    private void OnDisable()
    {
        Instances.Remove(this);
        ClearHover();
    }

    private void Update()
    {
        if (LevelSelectController.IsAnyCardDeckOpen())
        {
            ClearHover();
            return;
        }

        if (IsPointerOverUi())
        {
            ClearHover();
            return;
        }

        if (PauseUtility.IsPaused)
        {
            ClearHover();
            return;
        }

        UpdateHoverTarget();

        if (Input.GetMouseButtonDown(0))
            currentTarget?.Interact(gameObject);
    }

    private void UpdateHoverTarget()
    {
        if (worldCamera == null)
            return;

        Vector3 mouseWorld3 = worldCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mouseWorld = new Vector2(mouseWorld3.x, mouseWorld3.y);
        Collider2D hit = Physics2D.OverlapPoint(mouseWorld, interactableMask);

        IHoverInfoUI nextHoverUI = hit != null ? hit.GetComponentInParent<IHoverInfoUI>() : null;
        IInteractable nextTarget = hit != null ? hit.GetComponentInParent<IInteractable>() : null;

        if (nextHoverUI != currentHoverUI)
        {
            currentHoverUI?.SetHoverState(false, false);
            nextHoverUI?.SetHoverState(true, true);
            currentHoverUI = nextHoverUI;
        }

        currentTarget = nextTarget;
    }

    private void ClearHover()
    {
        currentHoverUI?.SetHoverState(false, false);
        currentHoverUI = null;
        currentTarget = null;
    }

    public static void ClearAllHover()
    {
        for (int i = 0; i < Instances.Count; i++)
            Instances[i]?.ClearHover();
    }

    private static bool IsPointerOverUi()
    {
        if (LevelSelectController.IsPointerOverOpenDeck(Input.mousePosition))
            return true;

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return false;

        UiRaycastResults.Clear();
        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };

        eventSystem.RaycastAll(pointerData, UiRaycastResults);

        foreach (RaycastResult result in UiRaycastResults)
        {
            GameObject hit = result.gameObject;
            if (hit == null)
                continue;

            if (hit.GetComponentInParent<Selectable>() != null)
                return true;

            if (hit.GetComponentInParent<ISubmitHandler>() != null ||
                hit.GetComponentInParent<IPointerClickHandler>() != null)
                return true;
        }

        return false;
    }
}
