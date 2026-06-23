using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ExaminePanelView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image examinedImage;
    [SerializeField] private RectTransform examinedImageRect;
    [SerializeField] private Button doneButton;

    [Header("Defaults")]
    [SerializeField, Min(1f)] private float defaultWidth = 1000f;
    [SerializeField] private bool lockInteractionsWhileOpen = true;

    private Action onDone;
    private bool ownsInteractionLock;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        ResolveRefs();
        doneButton?.onClick.AddListener(HandleDoneClicked);
        SetVisible(false);
    }

    private void OnDestroy()
    {
        doneButton?.onClick.RemoveListener(HandleDoneClicked);
        ReleaseInteractionLock();
    }

    private void OnDisable()
    {
        ReleaseInteractionLock();
    }

    public void Show(Sprite sprite, float width, Action doneCallback = null)
    {
        Show(sprite, width, doneCallback, lockInteractionsWhileOpen);
    }

    public void Show(Sprite sprite, float width, Action doneCallback, bool lockInteractions)
    {
        ResolveRefs();

        onDone = doneCallback;
        ApplyImage(sprite, width > 0f ? width : defaultWidth);
        SetVisible(true);

        if (lockInteractions)
            AcquireInteractionLock();
    }

    public void Hide()
    {
        Hide(invokeCallback: false);
    }

    public void Hide(bool invokeCallback)
    {
        Action callback = onDone;
        onDone = null;

        ReleaseInteractionLock();
        SetVisible(false);

        if (invokeCallback)
            callback?.Invoke();
    }

    private void HandleDoneClicked()
    {
        Hide(invokeCallback: true);
    }

    private void ApplyImage(Sprite sprite, float width)
    {
        if (examinedImage == null)
            return;

        examinedImage.sprite = sprite;
        examinedImage.preserveAspect = true;

        RectTransform rect = examinedImageRect != null ? examinedImageRect : examinedImage.rectTransform;
        if (rect == null)
            return;

        float aspect = 1f;
        if (sprite != null && sprite.rect.height > 0f)
            aspect = sprite.rect.width / sprite.rect.height;

        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, width / Mathf.Max(0.001f, aspect));
    }

    private void SetVisible(bool visible)
    {
        IsOpen = visible;

        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private void AcquireInteractionLock()
    {
        if (ownsInteractionLock || !Game.IsReady || Game.Ctx?.InteractionLock == null)
            return;

        Game.Ctx.InteractionLock.Acquire();
        ownsInteractionLock = true;
    }

    private void ReleaseInteractionLock()
    {
        if (!ownsInteractionLock || !Game.IsReady || Game.Ctx?.InteractionLock == null)
        {
            ownsInteractionLock = false;
            return;
        }

        Game.Ctx.InteractionLock.Release();
        ownsInteractionLock = false;
    }

    private void ResolveRefs()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (examinedImage == null)
        {
            Transform imageTransform = transform.Find("Examined Thing");
            if (imageTransform != null)
                examinedImage = imageTransform.GetComponent<Image>();
        }

        if (examinedImageRect == null && examinedImage != null)
            examinedImageRect = examinedImage.rectTransform;

        if (doneButton == null)
        {
            Transform doneTransform = transform.Find("Done");
            if (doneTransform != null)
                doneButton = doneTransform.GetComponent<Button>();
        }
    }
}
