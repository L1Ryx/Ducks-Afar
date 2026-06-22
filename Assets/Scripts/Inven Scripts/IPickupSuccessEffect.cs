using UnityEngine;

public interface IPickupSuccessEffect
{
    void OnPickupSucceeded(GameObject pickup, GameObject interactor);
}
