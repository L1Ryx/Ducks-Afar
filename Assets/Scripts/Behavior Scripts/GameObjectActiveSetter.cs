using System.Collections.Generic;
using UnityEngine;

public class GameObjectActiveSetter : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> targetObjects = new List<GameObject>();

    [SerializeField]
    private bool setInactiveOnStart = true;

    private void Start()
    {
        if (setInactiveOnStart)
        {
            SetInactive();
        }
    }

    public void SetActive()
    {
        foreach (GameObject obj in targetObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
    }

    public void SetInactive()
    {
        foreach (GameObject obj in targetObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }
}