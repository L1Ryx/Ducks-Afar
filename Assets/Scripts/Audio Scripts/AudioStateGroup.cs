using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Audio State Group", fileName = "AudioStateGroup_")]
public sealed class AudioStateGroup : ScriptableObject
{
    [Header("Wwise")]
    [Tooltip("Exact State Group name in Wwise (case-sensitive).")]
    public string groupName;

    [Header("Values")]
    [Tooltip("Optional default state value asset.")]
    public AudioStateValue defaultValue;

    [Tooltip("If enabled, only values listed in Allowed Values will be accepted.")]
    public bool validateValues = true;

    [Tooltip("Whitelist of allowed values for this group.")]
    public AudioStateValue[] allowedValues;

    public bool IsValid => !string.IsNullOrWhiteSpace(groupName);

    public bool IsAllowed(AudioStateValue value)
    {
        if (!validateValues) return true;
        if (value == null || !value.IsValid) return false;
        if (value.groupName != groupName) return false;
        if (allowedValues == null || allowedValues.Length == 0) return false;

        for (int i = 0; i < allowedValues.Length; i++)
        {
            if (allowedValues[i] == value) return true;
        }

        return false;
    }

    public AudioStateValue CoalesceOrDefault(AudioStateValue value)
    {
        if (value != null) return value;
        return defaultValue;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Keep default consistent.
        if (defaultValue != null && defaultValue.groupName != groupName)
        {
            defaultValue = null;
        }

        // Optional: prune mismatched values.
        if (allowedValues != null)
        {
            for (int i = 0; i < allowedValues.Length; i++)
            {
                var v = allowedValues[i];
                if (v != null && v.groupName != groupName)
                {
                    allowedValues[i] = null;
                }
            }
        }
    }
#endif
}