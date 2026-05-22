using System;
using UnityEngine;

[Serializable]
public sealed class PlayerSettingsData
{
    public int version = 1;

    [Range(0f, 100f)] public float masterVolume = 100f;
    [Range(0f, 100f)] public float mxVolume = 100f;
    [Range(0f, 100f)] public float sfxVolume = 100f;

    public bool fullscreen = true;

    public static PlayerSettingsData CreateDefault()
    {
        return new PlayerSettingsData
        {
            version = 1,
            masterVolume = 100f,
            mxVolume = 100f,
            sfxVolume = 100f,
            fullscreen = true
        };
    }

    public void Sanitize()
    {
        if (version <= 0)
            version = 1;

        masterVolume = Mathf.Clamp(masterVolume, 0f, 100f);
        mxVolume = Mathf.Clamp(mxVolume, 0f, 100f);
        sfxVolume = Mathf.Clamp(sfxVolume, 0f, 100f);
    }
}
