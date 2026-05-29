using UnityEngine;

public class PetBowl : InventoryCostInteractable
{
    [SerializeField] private LevelProgressReward levelProgressReward;

    protected override void OnPaymentSucceeded(GameObject interactor)
    {
        levelProgressReward?.Apply();
    }
}
