using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InventoryBarSlotUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image itemFrameImage;              // e.g. "Item 1 Frame UI" Image
    [SerializeField] private RectTransform itemFrameRect;

    [SerializeField] private Image itemIconImage;               // e.g. "Item 1 Icon UI" Image
    [SerializeField] private RectTransform itemIconRect;

    [SerializeField] private Image titleBackgroundImage;        // e.g. "Item 1 Title Background UI" Image
    [SerializeField] private TMP_Text titleText;                // e.g. "Item 1 Title UI" TMP

    [Header("Count")]
    [SerializeField] private TMP_Text numberText;   // "Number Text" under the slot
    [SerializeField, Range(0f, 1f)] private float numberVisibleAlpha = 1f;
    [SerializeField] private float numberFadeSeconds = 0.10f;
    [SerializeField] private Ease numberFadeEase = Ease.OutQuad;

    private int itemCount = 0;
    private bool ShouldShowCount => hasItem && itemCount >= 2;
    [Header("Selection Visuals")]
    [SerializeField] private float selectedScaleMultiplier = 1.10f;
    [SerializeField] private float selectTweenSeconds = 0.10f;
    [SerializeField] private Ease selectEase = Ease.OutCubic;
    [SerializeField] private float deselectTweenSeconds = 0.10f;
    [SerializeField] private Ease deselectEase = Ease.OutCubic;

    [Header("Selected Colors (Full)")]
    [SerializeField] private Color selectedFrameColor = Color.white;
    [SerializeField] private Color selectedIconColor = Color.white;

    [Header("Title Fade")]
    [SerializeField, Range(0f, 1f)] private float titleVisibleAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float titleBgVisibleAlpha = 1f;
    [SerializeField] private float titleFadeSeconds = 0.10f;
    [SerializeField] private Ease titleFadeEase = Ease.OutQuad;
    [Header("Floaty")]
    [SerializeField] private UIFloatyJuice titleFloaty;
    [SerializeField] private UIFloatyJuice numberFloaty;


    // Cached authored “base” state (dim/low alpha) from scene/prefab
    private Vector3 baseFrameScale;
    private Vector3 baseIconScale;
    private Color baseFrameColor;
    private Color baseIconColor;

    private Color baseTitleBgColor;
    private float baseTitleTextAlpha;

    private string currentItemId;
    public string CurrentItemId => currentItemId;

    private bool based;
    private bool hasItem;
    private bool IsEmpty => !hasItem;



    private void Awake()
    {
        if (itemFrameRect == null && itemFrameImage != null) itemFrameRect = itemFrameImage.rectTransform;
        if (itemIconRect == null && itemIconImage != null) itemIconRect = itemIconImage.rectTransform;

        if (itemFrameRect != null) baseFrameScale = itemFrameRect.localScale;
        if (itemIconRect != null) baseIconScale = itemIconRect.localScale;

        if (itemFrameImage != null) baseFrameColor = itemFrameImage.color;
        if (itemIconImage != null) baseIconColor = itemIconImage.color;

        if (titleBackgroundImage != null) baseTitleBgColor = titleBackgroundImage.color;

        if (titleText != null) baseTitleTextAlpha = titleText.alpha;
        if (titleBackgroundImage != null) baseTitleBgColor = titleBackgroundImage.color;
        
        if (titleBackgroundImage != null) baseTitleBgColor = titleBackgroundImage.color;
        SetTitleVisibleImmediate(false);

        // Ensure title starts hidden (your spec: only visible when selected)
        SetTitleVisibleImmediate(false);
        UpdateCountImmediate();
        
        if (titleFloaty == null && titleText != null)
            titleFloaty = titleText.GetComponent<UIFloatyJuice>();

        if (numberFloaty == null && numberText != null)
            numberFloaty = numberText.GetComponent<UIFloatyJuice>();
        
        if (itemFrameImage == null) Debug.LogError($"{name}: itemFrameImage NULL");
        if (itemIconImage == null) Debug.LogError($"{name}: itemIconImage NULL");
        if (titleText == null) Debug.LogWarning($"{name}: titleText NULL"); // less critical

    }
    
    private static void SetFloaty(UIFloatyJuice juice, bool enabled)
    {
        if (juice != null)
            juice.enabled = enabled;
    }


    public void SetEmpty()
    {
        KillTweens();
        hasItem = false;
        itemCount = 0;
        currentItemId = null;

        if (itemIconImage != null)
        {
            itemIconImage.sprite = null;
            var c = itemIconImage.color;
            c.a = 0f;
            itemIconImage.color = c;
        }

        if (titleText != null) titleText.text = string.Empty;
        SetTitleVisibleImmediate(false);
        UpdateCountImmediate();
    }


    public void BindItem(ItemDefinition def, int quantity)
    {
        KillTweens();
        hasItem = (def != null);
        itemCount = hasItem ? Mathf.Max(0, quantity) : 0;
        currentItemId = def != null ? def.itemId : null; // only if ItemDefinition really has itemId

        if (itemIconImage != null)
        {
            itemIconImage.sprite = def != null ? def.icon : null;

            // occupied slots: restore authored inactive color (including its alpha)
            itemIconImage.color = baseIconColor;
        }

        if (titleText != null)
            titleText.text = def != null ? def.displayName : string.Empty;

        UpdateCountImmediate();
        // SetTitleVisibleImmediate(false);
        // ApplyDeselectedImmediate();
    }
    
    private void UpdateCountImmediate()
    {
        if (numberText == null) return;

        if (ShouldShowCount)
            numberText.text = $"{itemCount}";
        else
            numberText.text = string.Empty;

        numberText.alpha = ShouldShowCount ? numberVisibleAlpha : 0f;
    }

    private void UpdateCountTweened()
    {
        if (numberText == null) return;

        DOTween.Kill(numberText);

        if (ShouldShowCount)
            numberText.text = $"{itemCount}";
        else
            numberText.text = string.Empty;

        numberText.DOFade(ShouldShowCount ? numberVisibleAlpha : 0f, numberFadeSeconds)
            .SetEase(numberFadeEase)
            .SetUpdate(true);
    }

    
    public void Rebase()
    {
        if (itemFrameRect != null) baseFrameScale = itemFrameRect.localScale;
        if (itemIconRect != null) baseIconScale = itemIconRect.localScale;
        

        // Force “title hidden unless selected”
        SetTitleVisibleImmediate(false);
        based = true;
    }
    
    private void OnEnable()
    {
        // Let layout finish this frame
        StartCoroutine(RebaseNextFrame());
    }

    private IEnumerator RebaseNextFrame()
    {
        yield return null;
        Rebase();
    }


    public void SetSelected(bool selected)
    {
        if (IsEmpty)
        {
            ApplyDeselectedImmediate();
            return;
        }

        if (selected) ApplySelectedTweened();
        else ApplyDeselectedTweened();
    }


    private void ApplySelectedTweened()
    {
        KillTweens();

        if (itemFrameRect != null)
            itemFrameRect.DOScale(baseFrameScale * selectedScaleMultiplier, selectTweenSeconds)
                .SetEase(selectEase).SetUpdate(true);

        if (itemIconRect != null)
            itemIconRect.DOScale(baseIconScale * selectedScaleMultiplier, selectTweenSeconds)
                .SetEase(selectEase).SetUpdate(true);

        if (itemFrameImage != null)
            itemFrameImage.DOColor(selectedFrameColor, selectTweenSeconds)
                .SetEase(selectEase).SetUpdate(true);

        if (itemIconImage != null)
            itemIconImage.DOColor(selectedIconColor, selectTweenSeconds)
                .SetEase(selectEase).SetUpdate(true);

        if (numberText != null) DOTween.Kill(numberText);

        FadeTitle(true);
    }

    private void ApplyDeselectedTweened()
    {
        KillTweens();

        if (itemFrameRect != null)
            itemFrameRect.DOScale(baseFrameScale, deselectTweenSeconds)
                .SetEase(deselectEase).SetUpdate(true);

        if (itemIconRect != null)
            itemIconRect.DOScale(baseIconScale, deselectTweenSeconds)
                .SetEase(deselectEase).SetUpdate(true);

        if (itemFrameImage != null)
            itemFrameImage.DOColor(baseFrameColor, deselectTweenSeconds)
                .SetEase(deselectEase).SetUpdate(true);

        if (itemIconImage != null)
        {
            if (IsEmpty)
            {
                var c = itemIconImage.color; c.a = 0f;
                itemIconImage.DOColor(c, deselectTweenSeconds).SetEase(deselectEase).SetUpdate(true);
            }
            else
            {
                itemIconImage.DOColor(baseIconColor, deselectTweenSeconds).SetEase(deselectEase).SetUpdate(true);
            }
        }


        FadeTitle(false);
    }

    private void ApplyDeselectedImmediate()
    {
        KillTweens();

        if (itemFrameRect != null) itemFrameRect.localScale = baseFrameScale;
        if (itemIconRect != null) itemIconRect.localScale = baseIconScale;

        if (itemFrameImage != null) itemFrameImage.color = baseFrameColor;

        if (itemIconImage != null)
        {
            if (IsEmpty)
            {
                var c = itemIconImage.color;
                c.a = 0f;
                itemIconImage.color = c;
            }
            else
            {
                itemIconImage.color = baseIconColor;
            }
        }

        SetTitleVisibleImmediate(false);
    }


    private void FadeTitle(bool visible)
    {
        if (IsEmpty)
            visible = false;

        float targetTextAlpha = visible ? titleVisibleAlpha : 0f;
        float bgAlpha = visible ? titleBgVisibleAlpha : 0f;

        if (titleBackgroundImage != null)
            titleBackgroundImage.DOFade(bgAlpha, titleFadeSeconds)
                .SetEase(titleFadeEase).SetUpdate(true);

        if (titleText != null)
            titleText.DOFade(targetTextAlpha, titleFadeSeconds)
                .SetEase(titleFadeEase).SetUpdate(true);
    }


    private void SetTitleVisibleImmediate(bool visible)
    {
        float textAlpha = visible ? titleVisibleAlpha : 0f;
        float bgAlpha = visible ? titleBgVisibleAlpha : 0f;

        if (titleBackgroundImage != null)
        {
            var bg = titleBackgroundImage.color;
            bg.a = bgAlpha;
            titleBackgroundImage.color = bg;
        }

        if (titleText != null) titleText.alpha = textAlpha;
    }

    private void KillTweens()
    {
        // Kill tweens that target these transforms/images specifically
        if (itemFrameRect != null) DOTween.Kill(itemFrameRect);
        if (itemIconRect != null) DOTween.Kill(itemIconRect);
        if (itemFrameImage != null) DOTween.Kill(itemFrameImage);
        if (itemIconImage != null) DOTween.Kill(itemIconImage);
        if (titleText != null) DOTween.Kill(titleText);
        if (titleBackgroundImage != null) DOTween.Kill(titleBackgroundImage);
    }
}
