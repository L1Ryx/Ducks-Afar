using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIRaycastDebug : MonoBehaviour
{
    private readonly List<RaycastResult> results = new();
    private PointerEventData pointerData;

    private void Awake()
    {
        pointerData = new PointerEventData(EventSystem.current);
    }

    private void Update()
    {
        if (EventSystem.current == null)
            return;

        pointerData.Reset();
        pointerData.position = Input.mousePosition;

        results.Clear();
        EventSystem.current.RaycastAll(pointerData, results);

        if (results.Count == 0)
            return;

        Debug.Log(
            "UI Hover Stack:\n" +
            string.Join("\n", results.ConvertAll(r => $"{r.gameObject.name} ({r.module})"))
        );
    }
}