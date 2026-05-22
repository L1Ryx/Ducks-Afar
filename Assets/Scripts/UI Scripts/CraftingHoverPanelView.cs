using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingHoverPanelView : MonoBehaviour
{
    [Header("Core")]
    public RectTransform panelTransform;
    public CanvasGroup canvasGroup;

    [Header("Main Text")]
    public TMP_Text titleText;
    public TMP_Text descText;

    [Header("Input")]
    public Image inputIcon;
    public TMP_Text inputText;

    [Header("Output")]
    public Image outputIcon;
    public TMP_Text outputText;

    [Header("Symbol")]
    public Image symbolImage;

    private void Awake()
    {
        NonInteractiveUiUtility.DisableRaycasts(gameObject, canvasGroup);
    }
}
