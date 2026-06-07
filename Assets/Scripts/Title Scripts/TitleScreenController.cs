using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public sealed class TitleScreenController : MonoBehaviour
{
    private enum TitlePanel
    {
        None,
        Intro,
        Main,
        FileSelect,
        Options
    }

    [Header("Panels")]
    [SerializeField] private CanvasGroup mainPanel;
    [SerializeField] private CanvasGroup fileSelectPanel;
    [SerializeField] private SettingsMenuPanel optionsPanel;
    [SerializeField] private CanvasGroup introPanel;

    [Header("Panel Fades")]
    [SerializeField] private bool animatePanelTransitions = true;

    [Header("File Select")]
    [SerializeField] private TitleFileSelectPanel fileSelect;
    [SerializeField] private string newGameSceneName = "Demo Begin";
    [SerializeField] private string newGameStartLocation = "auralis_0";

    [Header("Audio")]
    [SerializeField] private bool playMainMenuThemeOnCurtainUnveil = true;
    [SerializeField] private AudioCue mainMenuThemeCueOverride;

    private CanvasGroupFade mainPanelFade;
    private CanvasGroupFade fileSelectPanelFade;
    private CanvasGroupFade optionsPanelFade;
    private CanvasGroupFade introPanelFade;
    private Coroutine panelTransitionRoutine;
    private TitlePanel currentPanel = TitlePanel.None;
    private bool mainMenuThemeStarted;

    private void Awake()
    {
        ResolvePanelFades();

        if (fileSelect != null)
            fileSelect.SetController(this);

        ShowPanelInstant(TitlePanel.Intro);
    }

    private void OnEnable()
    {
        if (optionsPanel != null)
            optionsPanel.OnBackRequested += HandleOptionsBackRequested;

        FadeInOnLevelStart.FadeFromBlackStarted += HandleFadeFromBlackStarted;
    }

    private void OnDisable()
    {
        if (optionsPanel != null)
            optionsPanel.OnBackRequested -= HandleOptionsBackRequested;

        FadeInOnLevelStart.FadeFromBlackStarted -= HandleFadeFromBlackStarted;
        StopMainMenuTheme();

        if (panelTransitionRoutine != null)
        {
            StopCoroutine(panelTransitionRoutine);
            panelTransitionRoutine = null;
        }

        KillPanelTweens();
    }

    private void Update()
    {
        if (currentPanel == TitlePanel.FileSelect && Input.GetKeyDown(KeyCode.Escape))
            ShowMain();
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void ShowIntro()
    {
        ShowPanel(TitlePanel.Intro);
    }

    public void introClick()
    {
        ShowMain();
    }

    public void ShowMain()
    {
        ShowPanel(TitlePanel.Main);
    }

    public void ShowFileSelect()
    {
        ShowPanel(TitlePanel.FileSelect);
    }

    public void ShowOptions()
    {
        ShowPanel(TitlePanel.Options);
    }

    public void UseSaveSlot(int slotIndex)
    {
        if (!Game.IsReady || Game.Ctx?.Saves == null)
        {
            Debug.LogWarning("UseSaveSlot failed: save system is not ready.");
            return;
        }

        if (fileSelect != null && fileSelect.TryGetCachedSlot(slotIndex, out SaveSlotData cachedSlot))
        {
            UseSaveSlot(cachedSlot);
            return;
        }

        SaveSlotData slot = Game.Ctx.Saves.ReadSlot(slotIndex);

        if (slot.hasData)
        {
            if (Game.Ctx.Saves.LoadFromSlotAndEnterScene(slotIndex))
                StopMainMenuTheme();

            return;
        }

        if (Game.Ctx.Saves.StartNewGameInSlotAndEnterScene(
                slotIndex,
                newGameSceneName,
                newGameStartLocation))
        {
            StopMainMenuTheme();
        }
    }

    public void UseSaveSlot(SaveSlotData slot)
    {
        if (!Game.IsReady || Game.Ctx?.Saves == null)
        {
            Debug.LogWarning("UseSaveSlot failed: save system is not ready.");
            return;
        }

        if (slot != null && slot.hasData)
        {
            if (Game.Ctx.Saves.LoadSlotDataAndEnterScene(slot))
                StopMainMenuTheme();

            return;
        }

        int slotIndex = slot != null ? slot.slotIndex : 0;

        if (Game.Ctx.Saves.StartNewGameInSlotAndEnterScene(
                slotIndex,
                newGameSceneName,
                newGameStartLocation))
        {
            StopMainMenuTheme();
        }
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
        StopMainMenuTheme();
        ApplicationExitUtility.ExitApplication();
    }

    private void HandleFadeFromBlackStarted(FadeInOnLevelStart fade)
    {
        if (!playMainMenuThemeOnCurtainUnveil)
            return;

        if (fade == null || fade.gameObject.scene != gameObject.scene)
            return;

        if (SceneManager.GetActiveScene() != gameObject.scene)
            return;

        PlayMainMenuTheme();
    }

    private void PlayMainMenuTheme()
    {
        if (mainMenuThemeStarted)
            return;

        AudioCue cue = mainMenuThemeCueOverride != null
            ? mainMenuThemeCueOverride
            : ProjectAudio.Config != null ? ProjectAudio.Config.MainMenuThemeCue : null;

        if (cue == null || !cue.HasPlayEvent)
            return;

        ProjectAudio.SetGlobalMusic(cue);
        mainMenuThemeStarted = true;
    }

    private void StopMainMenuTheme()
    {
        if (!mainMenuThemeStarted)
            return;

        ProjectAudio.StopGlobalMusic(immediate: false);
        mainMenuThemeStarted = false;
    }

    private bool HandleOptionsBackRequested()
    {
        if (currentPanel != TitlePanel.Options)
            return false;

        ShowMain();
        return true;
    }

    private void ShowPanel(TitlePanel panel)
    {
        if (currentPanel == panel)
            return;

        if (!animatePanelTransitions)
        {
            ShowPanelInstant(panel);
            return;
        }

        if (panelTransitionRoutine != null)
            return;

        panelTransitionRoutine = StartCoroutine(ShowPanelRoutine(panel));
    }

    private IEnumerator ShowPanelRoutine(TitlePanel panel)
    {
        TitlePanel outgoingPanel = currentPanel;
        CanvasGroupFade outgoingFade = GetPanelFade(outgoingPanel);

        if (outgoingFade != null)
        {
            Tween fadeOut = outgoingFade.FadeOut();
            if (fadeOut != null)
                yield return fadeOut.WaitForCompletion();
        }

        HidePanelAfterFade(outgoingPanel);
        PreparePanelForShow(panel);

        CanvasGroupFade incomingFade = GetPanelFade(panel);
        if (incomingFade != null)
        {
            incomingFade.HideInstant();
            Tween fadeIn = incomingFade.FadeIn();
            if (fadeIn != null)
                yield return fadeIn.WaitForCompletion();
        }
        else
        {
            SetCanvasGroupVisible(GetPanelCanvasGroup(panel), true);
        }

        currentPanel = panel;
        panelTransitionRoutine = null;
    }

    private void ShowPanelInstant(TitlePanel panel)
    {
        if (panelTransitionRoutine != null)
        {
            StopCoroutine(panelTransitionRoutine);
            panelTransitionRoutine = null;
            KillPanelTweens();
        }

        HidePanelInstant(TitlePanel.Intro);
        HidePanelInstant(TitlePanel.Main);
        HidePanelInstant(TitlePanel.FileSelect);
        HidePanelInstant(TitlePanel.Options);

        PreparePanelForShow(panel);
        CanvasGroupFade fade = GetPanelFade(panel);

        if (fade != null)
            fade.ShowInstant();
        else
            SetCanvasGroupVisible(GetPanelCanvasGroup(panel), true);

        currentPanel = panel;
    }

    private void PreparePanelForShow(TitlePanel panel)
    {
        switch (panel)
        {
            case TitlePanel.FileSelect:
                fileSelect?.Refresh();
                break;
            case TitlePanel.Options:
                optionsPanel?.Show();
                break;
        }
    }

    private void HidePanelAfterFade(TitlePanel panel)
    {
        if (panel == TitlePanel.Options)
            optionsPanel?.Hide();

        if (panel == TitlePanel.None)
            return;

        CanvasGroupFade fade = GetPanelFade(panel);

        if (fade != null)
            fade.HideInstant();
        else
            SetCanvasGroupVisible(GetPanelCanvasGroup(panel), false);
    }

    private void HidePanelInstant(TitlePanel panel)
    {
        if (panel == TitlePanel.Options)
            optionsPanel?.Hide();

        CanvasGroupFade fade = GetPanelFade(panel);

        if (fade != null)
            fade.HideInstant();
        else
            SetCanvasGroupVisible(GetPanelCanvasGroup(panel), false);
    }

    private void ResolvePanelFades()
    {
        mainPanelFade = ResolvePanelFade(mainPanel);
        fileSelectPanelFade = ResolvePanelFade(fileSelectPanel);
        introPanelFade = ResolvePanelFade(introPanel);
        optionsPanelFade = ResolvePanelFade(optionsPanel != null ? optionsPanel.RootCanvasGroup : null);
    }

    private void KillPanelTweens()
    {
        mainPanelFade?.KillActiveTween(false);
        fileSelectPanelFade?.KillActiveTween(false);
        optionsPanelFade?.KillActiveTween(false);
        introPanelFade?.KillActiveTween(false);
    }

    private static CanvasGroupFade ResolvePanelFade(CanvasGroup group)
    {
        if (group == null)
            return null;

        if (!group.TryGetComponent(out CanvasGroupFade fade))
            fade = group.gameObject.AddComponent<CanvasGroupFade>();

        return fade;
    }

    private CanvasGroupFade GetPanelFade(TitlePanel panel)
    {
        return panel switch
        {
            TitlePanel.Intro => introPanelFade,
            TitlePanel.Main => mainPanelFade,
            TitlePanel.FileSelect => fileSelectPanelFade,
            TitlePanel.Options => optionsPanelFade,
            _ => null
        };
    }

    private CanvasGroup GetPanelCanvasGroup(TitlePanel panel)
    {
        return panel switch
        {
            TitlePanel.Intro => introPanel,
            TitlePanel.Main => mainPanel,
            TitlePanel.FileSelect => fileSelectPanel,
            TitlePanel.Options => optionsPanel != null ? optionsPanel.RootCanvasGroup : null,
            _ => null
        };
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
