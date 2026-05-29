using UnityEngine;
using UnityEngine.UI;

public sealed class LevelSelectTwoStateSymbol : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private Sprite filledSprite;
    [SerializeField] private Sprite unfilledSprite;
    [SerializeField] private Color filledColor = Color.white;
    [SerializeField] private Color unfilledColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private GameObject filledObject;
    [SerializeField] private GameObject unfilledObject;

    private void Reset()
    {
        image = GetComponent<Image>();
    }

    public void SetState(bool filled)
    {
        SetState(filled, null);
    }

    public void SetState(bool filled, Sprite fallbackSprite)
    {
        if (filledObject != null)
            filledObject.SetActive(filled);

        if (unfilledObject != null)
            unfilledObject.SetActive(!filled);

        if (image == null)
            return;

        Sprite stateSprite = filled ? filledSprite : unfilledSprite;
        image.sprite = stateSprite != null ? stateSprite : fallbackSprite;
        image.color = filled ? filledColor : unfilledColor;
    }
}
