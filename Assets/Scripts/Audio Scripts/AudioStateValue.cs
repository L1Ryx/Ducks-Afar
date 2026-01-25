using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Audio State Value", fileName = "AudioStateValue_")]
public sealed class AudioStateValue : ScriptableObject
{
    [Header("Wwise")]
    [Tooltip("Exact State Group name in Wwise (case-sensitive).")]
    public string groupName;

    [Tooltip("Exact State value name in Wwise (case-sensitive).")]
    public string valueName;

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(groupName) &&
        !string.IsNullOrWhiteSpace(valueName);
}