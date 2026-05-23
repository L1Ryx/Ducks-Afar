using UnityEngine;

public sealed class TitleScreenController : MonoBehaviour
{
    private enum TitlePanel
    {
        Main,
        FileSelect,
        Options
    }

    [Header("Panels")]
    [SerializeField] private CanvasGroup mainPanel;
    [SerializeField] private CanvasGroup fileSelectPanel;
    [SerializeField] private SettingsMenuPanel optionsPanel;

    [Header("File Select")]
    [SerializeField] private TitleFileSelectPanel fileSelect;
    [SerializeField] private string newGameSceneName = "Demo Begin";
    [SerializeField] private string newGameStartLocation = "auralis_0";

    private TitlePanel currentPanel;

    private void Awake()
    {
        if (fileSelect != null)
            fileSelect.SetController(this);

        ShowMain();
    }

    private void OnEnable()
    {
        if (optionsPanel != null)
            optionsPanel.OnBackRequested += HandleOptionsBackRequested;
    }

    private void OnDisable()
    {
        if (optionsPanel != null)
            optionsPanel.OnBackRequested -= HandleOptionsBackRequested;
    }

    private void Update()
    {
        if (currentPanel == TitlePanel.FileSelect && Input.GetKeyDown(KeyCode.Escape))
            ShowMain();
    }

    public void ShowMain()
    {
        currentPanel = TitlePanel.Main;
        SetCanvasGroupVisible(mainPanel, true);
        SetCanvasGroupVisible(fileSelectPanel, false);
        optionsPanel?.Hide();
    }

    public void ShowFileSelect()
    {
        currentPanel = TitlePanel.FileSelect;
        fileSelect?.Refresh();
        SetCanvasGroupVisible(mainPanel, false);
        SetCanvasGroupVisible(fileSelectPanel, true);
        optionsPanel?.Hide();
    }

    public void ShowOptions()
    {
        currentPanel = TitlePanel.Options;
        SetCanvasGroupVisible(mainPanel, false);
        SetCanvasGroupVisible(fileSelectPanel, false);
        optionsPanel?.Show();
    }

    public void UseSaveSlot(int slotIndex)
    {
        if (!Game.IsReady || Game.Ctx?.Saves == null)
        {
            Debug.LogWarning("UseSaveSlot failed: save system is not ready.");
            return;
        }

        SaveSlotData slot = Game.Ctx.Saves.ReadSlot(slotIndex);

        if (slot.hasData)
        {
            Game.Ctx.Saves.LoadFromSlotAndEnterScene(slotIndex);
            return;
        }

        Game.Ctx.Saves.StartNewGameInSlotAndEnterScene(
            slotIndex,
            newGameSceneName,
            newGameStartLocation);
    }

    public void DeleteSaveSlot(int slotIndex)
    {
        if (!Game.IsReady || Game.Ctx?.Saves == null)
        {
            Debug.LogWarning("DeleteSaveSlot failed: save system is not ready.");
            return;
        }

        Game.Ctx.Saves.DeleteSlot(slotIndex);
        fileSelect?.Refresh();
    }

    public void Quit()
    {
        ApplicationExitUtility.ExitApplication();
    }

    private void HandleOptionsBackRequested()
    {
        if (currentPanel == TitlePanel.Options)
            ShowMain();
    }

    private static void SetCanvasGroupVisible(CanvasGroup group, bool visible)
    {
        if (group == null)
            return;

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }
}
