using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryCostHoverPanelView : MonoBehaviour
{
    [Header("Core")]
    public RectTransform panelTransform;
    public CanvasGroup canvasGroup;

    [Header("Main Text")]
    public TMP_Text titleText;
    public TMP_Text descText;

    [Header("Requirement")]
    public Image requiredIcon;
    public TMP_Text requiredText;

    [Header("Symbol")]
    public Image symbolImage;

    private void Awake()
    {
        NonInteractiveUiUtility.DisableRaycasts(gameObject, canvasGroup);
    }
}
