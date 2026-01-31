using UnityEngine;

public class Spaceship : InventoryCostInteractable
{
    [SerializeField] private AudioCue ac;
    protected override void OnPaymentSucceeded(GameObject interactor)
    {
        Game.Ctx.Audio.PlayCueGlobal(ac);
    }
}
