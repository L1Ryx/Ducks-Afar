using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyGateKeyInteractable : InventoryCostInteractable
{
    [Header("Gate")]
    [SerializeField] private EnemyGateController gate;
    [SerializeField] private InventoryCostInteractableHoverPanel hoverPanel;

    private void Awake()
    {
        if (gate == null)
            gate = GetComponent<EnemyGateController>();

        if (hoverPanel == null)
            hoverPanel = GetComponent<InventoryCostInteractableHoverPanel>();
    }

    protected override bool CanInteractNow(GameObject interactor)
    {
        return gate != null && gate.CanOpen;
    }

    protected override void OnPaymentSucceeded(GameObject interactor)
    {
        hoverPanel?.ForceHide();
        gate?.OpenGate();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (gate == null)
            gate = GetComponent<EnemyGateController>();

        if (hoverPanel == null)
            hoverPanel = GetComponent<InventoryCostInteractableHoverPanel>();
    }
#endif
}
