using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdditionSlotHoverPanelView : MonoBehaviour
{
    [Header("Core")]
    public RectTransform panelTransform;
    public CanvasGroup canvasGroup;

    [Header("Text")]
    public TMP_Text titleText;
    public TMP_Text descText;

    [Header("Stored Item")]
    public Image itemIcon;
    public TMP_Text itemName;

    [Header("Symbol")]
    public Image symbolImage;

    private void Awake()
    {
        NonInteractiveUiUtility.DisableRaycasts(gameObject, canvasGroup);
    }
}
