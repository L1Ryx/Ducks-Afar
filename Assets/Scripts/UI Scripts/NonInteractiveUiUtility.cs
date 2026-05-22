using UnityEngine;
using UnityEngine.UI;

public static class NonInteractiveUiUtility
{
    public static void DisableRaycasts(GameObject root, CanvasGroup canvasGroup)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (root == null)
            return;

        var graphics = root.GetComponentsInChildren<Graphic>(includeInactive: true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = false;
    }
}
