using System;
using System.IO;
using UnityEngine;

public sealed class SettingsSystem
{
    private const string SettingsDirectoryName = "Settings";
    private const string SettingsFileName = "player_settings.json";

    private const string MasterVolumeRtpcName = "Volume_Master";
    private const string MxVolumeRtpcName = "Volume_MX";
    private const string SfxVolumeRtpcName = "Volume_SFX";

    private readonly GameContext ctx;

    public PlayerSettingsData Data { get; private set; } = PlayerSettingsData.CreateDefault();

    public event Action OnChanged;

    public SettingsSystem(GameContext ctx)
    {
        this.ctx = ctx;
    }

    public string SettingsDirectoryPath => Path.Combine(Application.persistentDataPath, SettingsDirectoryName);
    public string SettingsFilePath => Path.Combine(SettingsDirectoryPath, SettingsFileName);

    public void LoadOrCreate()
    {
        EnsureSettingsDirectoryExists();

        if (!File.Exists(SettingsFilePath))
        {
            Data = PlayerSettingsData.CreateDefault();
            Save();
            return;
        }

        try
        {
            string json = File.ReadAllText(SettingsFilePath);
            Data = JsonUtility.FromJson<PlayerSettingsData>(json) ?? PlayerSettingsData.CreateDefault();
            Data.Sanitize();
        }
        catch (Exception ex)
        {
            Debug.LogError($"SettingsSystem: failed to load settings. Using defaults. {ex}");
            Data = PlayerSettingsData.CreateDefault();
        }
    }

    public void Save()
    {
        EnsureSettingsDirectoryExists();
        Data.Sanitize();

        try
        {
            string json = JsonUtility.ToJson(Data, true);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"SettingsSystem: failed to save settings. {ex}");
        }
    }

    public void ApplyAll()
    {
        ApplyAudio();
        ApplyVisual();
    }

    public void SetMasterVolume(float value)
    {
        float clamped = Mathf.Clamp(value, 0f, 100f);
        if (Mathf.Approximately(Data.masterVolume, clamped))
            return;

        Data.masterVolume = clamped;
        ApplyChangedSettings(ApplyAudio);
    }

    public void SetMxVolume(float value)
    {
        float clamped = Mathf.Clamp(value, 0f, 100f);
        if (Mathf.Approximately(Data.mxVolume, clamped))
            return;

        Data.mxVolume = clamped;
        ApplyChangedSettings(ApplyAudio);
    }

    public void SetSfxVolume(float value)
    {
        float clamped = Mathf.Clamp(value, 0f, 100f);
        if (Mathf.Approximately(Data.sfxVolume, clamped))
            return;

        Data.sfxVolume = clamped;
        ApplyChangedSettings(ApplyAudio);
    }

    public void SetFullscreen(bool fullscreen)
    {
        if (Data.fullscreen == fullscreen)
            return;

        Data.fullscreen = fullscreen;
        ApplyVisual();
        Save();
        OnChanged?.Invoke();
    }

    public void ResetToDefaults()
    {
        Data = PlayerSettingsData.CreateDefault();
        ApplyAll();
        Save();
        OnChanged?.Invoke();
    }

    private void ApplyChangedSettings(Action apply)
    {
        apply?.Invoke();
        Save();
        OnChanged?.Invoke();
    }

    private void ApplyAudio()
    {
        if (ctx?.Audio == null)
            return;

        ctx.Audio.SetGlobalRtpc(MasterVolumeRtpcName, Data.masterVolume);
        ctx.Audio.SetGlobalRtpc(MxVolumeRtpcName, Data.mxVolume);
        ctx.Audio.SetGlobalRtpc(SfxVolumeRtpcName, Data.sfxVolume);
    }

    private void ApplyVisual()
    {
        DesktopFullscreen.Apply(Data.fullscreen);
    }

    private void EnsureSettingsDirectoryExists()
    {
        Directory.CreateDirectory(SettingsDirectoryPath);
    }
}
