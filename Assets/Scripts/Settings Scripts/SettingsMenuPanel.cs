using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class SettingsMenuPanel : MonoBehaviour
{
    private static SettingsMenuPanel activePanel;

    [Header("Panel")]
    [SerializeField] private CanvasGroup root;
    [SerializeField] private bool hideOnAwake = true;
    [SerializeField] private bool bringToFrontOnShow = true;

    [Header("Audio")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider mxVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TMP_Text masterVolumeValueText;
    [SerializeField] private TMP_Text mxVolumeValueText;
    [SerializeField] private TMP_Text sfxVolumeValueText;

    [Header("Visual")]
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Events")]
    [SerializeField] private UnityEvent onBack;

    private bool suppressCallbacks;
    private bool isVisible;

    public event Func<bool> OnBackRequested;
    public CanvasGroup RootCanvasGroup => GetRootCanvasGroup();

    public static bool TryBackActivePanel()
    {
        if (activePanel == null || !activePanel.isActiveAndEnabled || !activePanel.isVisible)
            return false;

        activePanel.Back();
        return true;
    }

    private void Reset()
    {
        GetRootCanvasGroup();
    }

    private void Awake()
    {
        ConfigureSlider(masterVolumeSlider);
        ConfigureSlider(mxVolumeSlider);
        ConfigureSlider(sfxVolumeSlider);

        if (hideOnAwake)
            SetVisible(false);
    }

    private void OnEnable()
    {
        masterVolumeSlider?.onValueChanged.AddListener(HandleMasterVolumeChanged);
        mxVolumeSlider?.onValueChanged.AddListener(HandleMxVolumeChanged);
        sfxVolumeSlider?.onValueChanged.AddListener(HandleSfxVolumeChanged);
        fullscreenToggle?.onValueChanged.AddListener(HandleFullscreenChanged);

        RefreshFromSettings();
    }

    private void OnDisable()
    {
        masterVolumeSlider?.onValueChanged.RemoveListener(HandleMasterVolumeChanged);
        mxVolumeSlider?.onValueChanged.RemoveListener(HandleMxVolumeChanged);
        sfxVolumeSlider?.onValueChanged.RemoveListener(HandleSfxVolumeChanged);
        fullscreenToggle?.onValueChanged.RemoveListener(HandleFullscreenChanged);

        if (activePanel == this)
            activePanel = null;
    }

    public void Show()
    {
        RefreshFromSettings();
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    public void Back()
    {
        bool handled = RaiseBackRequested();
        if (handled)
            return;

        Hide();
        onBack?.Invoke();
    }

    public void RefreshFromSettings()
    {
        if (!Game.IsReady || Game.Ctx?.Settings == null)
            return;

        var data = Game.Ctx.Settings.Data;
        suppressCallbacks = true;

        SetSliderWithoutNotify(masterVolumeSlider, data.masterVolume);
        SetSliderWithoutNotify(mxVolumeSlider, data.mxVolume);
        SetSliderWithoutNotify(sfxVolumeSlider, data.sfxVolume);

        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(data.fullscreen);

        suppressCallbacks = false;
        RefreshValueText();
    }

    private void SetVisible(bool visible)
    {
        CanvasGroup targetRoot = GetRootCanvasGroup();

        if (targetRoot == null)
        {
            Debug.LogWarning($"{nameof(SettingsMenuPanel)} has no CanvasGroup root assigned.", this);
            return;
        }

        targetRoot.alpha = visible ? 1f : 0f;
        targetRoot.interactable = visible;
        targetRoot.blocksRaycasts = visible;
        isVisible = visible;

        if (visible)
        {
            if (bringToFrontOnShow)
                targetRoot.transform.SetAsLastSibling();

            activePanel = this;
        }
        else if (activePanel == this)
        {
            activePanel = null;
        }
    }

    private CanvasGroup GetRootCanvasGroup()
    {
        if (root == null)
            root = GetComponentInChildren<CanvasGroup>(true);

        return root;
    }

    private bool RaiseBackRequested()
    {
        if (OnBackRequested == null)
            return false;

        bool handled = false;
        foreach (Func<bool> handler in OnBackRequested.GetInvocationList())
        {
            if (handler())
                handled = true;
        }

        return handled;
    }

    private static void ConfigureSlider(Slider slider)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.wholeNumbers = true;
    }

    private static void SetSliderWithoutNotify(Slider slider, float value)
    {
        if (slider != null)
            slider.SetValueWithoutNotify(Mathf.Clamp(value, 0f, 100f));
    }

    private void HandleMasterVolumeChanged(float value)
    {
        if (suppressCallbacks) return;
        Game.Ctx.Settings.SetMasterVolume(value);
        RefreshValueText();
    }

    private void HandleMxVolumeChanged(float value)
    {
        if (suppressCallbacks) return;
        Game.Ctx.Settings.SetMxVolume(value);
        RefreshValueText();
    }

    private void HandleSfxVolumeChanged(float value)
    {
        if (suppressCallbacks) return;
        Game.Ctx.Settings.SetSfxVolume(value);
        RefreshValueText();
    }

    private void HandleFullscreenChanged(bool fullscreen)
    {
        if (suppressCallbacks) return;
        Game.Ctx.Settings.SetFullscreen(fullscreen);
    }

    private void RefreshValueText()
    {
        SetValueText(masterVolumeValueText, masterVolumeSlider);
        SetValueText(mxVolumeValueText, mxVolumeSlider);
        SetValueText(sfxVolumeValueText, sfxVolumeSlider);
    }

    private static void SetValueText(TMP_Text text, Slider slider)
    {
        if (text == null || slider == null)
            return;

        text.text = Mathf.RoundToInt(slider.value).ToString();
    }
}
