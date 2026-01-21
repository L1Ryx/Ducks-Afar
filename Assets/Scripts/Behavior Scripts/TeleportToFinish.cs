using UnityEngine;

public class TeleportToFinish : MonoBehaviour
{
    [Header("Finish Data")] [SerializeField] private Vector3 finishPosition = Vector3.zero;
    
    public void Teleport()
    {
        // CAUTION: NO TWEENING HERE! 
        this.gameObject.transform.position = finishPosition;
    }
}
