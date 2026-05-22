using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdditionOutputHoverPanelView : MonoBehaviour
{
    [Header("Core")]
    public RectTransform panelTransform;
    public CanvasGroup canvasGroup;

    [Header("Text")]
    public TMP_Text titleText;
    public TMP_Text descText;

    [Header("Result")]
    public Image resultIcon;
    public TMP_Text resultName;

    [Header("Symbol")]
    public Image symbolImage;

    private void Awake()
    {
        NonInteractiveUiUtility.DisableRaycasts(gameObject, canvasGroup);
    }
}
