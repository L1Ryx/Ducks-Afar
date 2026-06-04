using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SceneFlowPreset", menuName = "Scene Flow/Preset")]
public class SceneFlowPresetSO : ScriptableObject
{
    [SerializeField] private string presetId = "default";
    [TextArea]
    [SerializeField] private string description;
    [SerializeField] private string firstSceneName;
    [SerializeField] private List<SceneFlowTransition> transitions = new();

    public string PresetId => presetId;
    public string Description => description;
    public string FirstSceneName => firstSceneName;
    public IReadOnlyList<SceneFlowTransition> Transitions => transitions;

    public bool TryGetTransition(string fromSceneName, out SceneFlowTransition transition)
    {
        transition = null;

        if (string.IsNullOrWhiteSpace(fromSceneName))
            return false;

        foreach (SceneFlowTransition candidate in transitions)
        {
            if (candidate.MatchesFromScene(fromSceneName))
            {
                transition = candidate;
                return true;
            }
        }

        return false;
    }
}

[Serializable]
public sealed class SceneFlowTransition
{
    [SerializeField] private string fromSceneName;
    [SerializeField] private string toSceneName;
    [SerializeField] private string loadingMessage;

    public string FromSceneName => fromSceneName;
    public string ToSceneName => toSceneName;
    public string LoadingMessage => loadingMessage;

    public bool MatchesFromScene(string sceneName)
    {
        return string.Equals(fromSceneName, sceneName, StringComparison.Ordinal);
    }
}
