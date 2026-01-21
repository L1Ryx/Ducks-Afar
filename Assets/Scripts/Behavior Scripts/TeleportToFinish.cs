using UnityEngine;

public class TeleportToFinish : MonoBehaviour
{
    [Header("Finish Data")] [SerializeField] private Vector3 finishPosition = Vector3.zero;
    [SerializeField] private bool shouldTeleportToFinish = false;
    
    public void Teleport()
    {
        if (!shouldTeleportToFinish)
        {
            return;
        }
        // CAUTION: NO TWEENING HERE! 
        this.gameObject.transform.position = finishPosition;
    }
}
