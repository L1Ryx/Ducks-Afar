using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyGateKeyInteractable : InventoryCostInteractable
{
    [Header("Gate")]
    [SerializeField] private EnemyGateController gate;

    private void Awake()
    {
        if (gate == null)
            gate = GetComponent<EnemyGateController>();
    }

    protected override bool CanInteractNow(GameObject interactor)
    {
        return gate != null && gate.CanOpen;
    }

    protected override void OnPaymentSucceeded(GameObject interactor)
    {
        gate?.OpenGate();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (gate == null)
            gate = GetComponent<EnemyGateController>();
    }
#endif
}
