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

    private LevelDefinition level;

    public UnityEvent<LevelDefinition> OnLevelSelected { get; } = new();
    public LevelDefinition BoundLevel => level;
    public bool CanSelect => level != null && (button == null || button.interactable);

    private void Reset()
    {
        button = GetComponent<Button>();
    }

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(HandleClicked);
    }

    private void OnDestroy()
    {
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

        if (lockedCanvasGroup != null)
            lockedCanvasGroup.alpha = unlocked ? 1f : 0.55f;

        RebuildArtifactSymbols(level, saveState);
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
