using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class LevelSelectLevelCardView : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("State")]
    [SerializeField] private LevelSelectTwoStateSymbol completedSymbol;
    [SerializeField] private Transform artifactSymbolParent;
    [SerializeField] private LevelSelectTwoStateSymbol artifactSymbolPrefab;
    [SerializeField] private CanvasGroup lockedCanvasGroup;

    [Header("Input")]
    [SerializeField] private Button button;

    [Header("Tween")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private UIFloatyJuice floatyJuice;

    private LevelDefinition level;
    private float visibleAlpha = 1f;
    private Tween fadeTween;

    public UnityEvent<LevelDefinition> OnLevelSelected { get; } = new();
    public LevelDefinition BoundLevel => level;
    public bool CanSelect => level != null && (button == null || button.interactable);

    private void Reset()
    {
        button = GetComponent<Button>();
        fadeCanvasGroup = GetComponent<CanvasGroup>();
        floatyJuice = GetComponent<UIFloatyJuice>();
    }

    private void Awake()
    {
        if (fadeCanvasGroup == null)
            fadeCanvasGroup = GetComponent<CanvasGroup>();

        if (floatyJuice == null)
            floatyJuice = GetComponent<UIFloatyJuice>();

        if (button != null)
            button.onClick.AddListener(HandleClicked);
    }

    private void OnDestroy()
    {
        fadeTween?.Kill();

        if (button != null)
            button.onClick.RemoveListener(HandleClicked);
    }

    public void Bind(LevelDefinition levelDefinition, SaveStateModel saveState)
    {
        level = levelDefinition;

        bool hasLevel = level != null;
        string levelId = hasLevel ? level.LevelId : string.Empty;
        bool unlocked = hasLevel && (saveState == null || saveState.IsLevelUnlocked(levelId));
        bool completed = hasLevel && saveState != null && saveState.IsLevelCompleted(levelId);

        if (titleText != null)
            titleText.text = hasLevel ? level.DisplayName : "Unknown Level";

        if (descriptionText != null)
            descriptionText.text = hasLevel ? level.Description : string.Empty;

        if (completedSymbol != null)
            completedSymbol.SetState(completed);

        if (button != null)
            button.interactable = unlocked;

        visibleAlpha = unlocked ? 1f : 0.55f;

        if (lockedCanvasGroup != null && lockedCanvasGroup != fadeCanvasGroup)
            lockedCanvasGroup.alpha = visibleAlpha;
        else if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = visibleAlpha;

        RebuildArtifactSymbols(level, saveState);
    }

    public void PlayFadeIn(float duration, float delay)
    {
        if (fadeCanvasGroup == null)
            return;

        fadeTween?.Kill();
        fadeCanvasGroup.alpha = 0f;
        fadeTween = fadeCanvasGroup
            .DOFade(visibleAlpha, duration)
            .SetDelay(delay)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    public Tween PlayFadeOut(float duration)
    {
        if (fadeCanvasGroup == null)
            return null;

        fadeTween?.Kill();
        fadeTween = fadeCanvasGroup
            .DOFade(0f, duration)
            .SetEase(Ease.InQuad)
            .SetUpdate(true);

        return fadeTween;
    }

    public void RebaseFloatyJuice()
    {
        if (floatyJuice != null)
            floatyJuice.Rebase();
    }

    private void RebuildArtifactSymbols(LevelDefinition levelDefinition, SaveStateModel saveState)
    {
        if (artifactSymbolParent == null)
            return;

        for (int i = artifactSymbolParent.childCount - 1; i >= 0; i--)
        {
            Destroy(artifactSymbolParent.GetChild(i).gameObject);
        }

        if (levelDefinition == null || artifactSymbolPrefab == null)
            return;

        foreach (LevelArtifactDefinition artifact in levelDefinition.Artifacts)
        {
            if (artifact == null)
                continue;

            LevelSelectTwoStateSymbol symbol = Instantiate(artifactSymbolPrefab, artifactSymbolParent);
            symbol.transform.SetAsFirstSibling();

            bool discovered = saveState != null && saveState.IsArtifactDiscovered(artifact.ArtifactId);
            symbol.SetState(discovered, artifact.Icon);
        }
    }

    private void HandleClicked()
    {
        if (level == null)
            return;

        OnLevelSelected.Invoke(level);
    }
}
