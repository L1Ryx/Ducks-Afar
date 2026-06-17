using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SceneVariant", menuName = "Scene Variants/Variant")]
public sealed class SceneVariantDataSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string variantId = "default";
    [TextArea] [SerializeField] private string description;

    [Header("Events")]
    [Tooltip("Raised every time this variant is applied in its scene.")]
    [SerializeField] private List<GameEvent> onAppliedEvents = new();

    [Tooltip("Raised only the first time this variant is applied for the active save file.")]
    [SerializeField] private List<GameEvent> onFirstAppliedEvents = new();

    public string VariantId => Normalize(variantId);
    public string Description => description;
    public IReadOnlyList<GameEvent> OnAppliedEvents => onAppliedEvents;
    public IReadOnlyList<GameEvent> OnFirstAppliedEvents => onFirstAppliedEvents;

    public static string Normalize(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }
}
