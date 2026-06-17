using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SceneVariantSequence", menuName = "Scene Variants/Sequence")]
public sealed class SceneVariantSequenceSO : ScriptableObject
{
    [Header("Scene")]
    [SerializeField] private string sceneName;

    [Header("Ordered Variants")]
    [SerializeField] private List<SceneVariantDataSO> variants = new();

    public string SceneName => SceneVariantDataSO.Normalize(sceneName);
    public IReadOnlyList<SceneVariantDataSO> Variants => variants;

    public SceneVariantDataSO FirstVariant
    {
        get
        {
            foreach (SceneVariantDataSO variant in variants)
            {
                if (variant != null && variant.VariantId.Length > 0)
                    return variant;
            }

            return null;
        }
    }

    public SceneVariantDataSO ResolveVariant(string variantId)
    {
        if (TryGetVariant(variantId, out SceneVariantDataSO variant))
            return variant;

        return FirstVariant;
    }

    public bool TryGetVariant(string variantId, out SceneVariantDataSO variant)
    {
        string normalizedVariantId = SceneVariantDataSO.Normalize(variantId);

        foreach (SceneVariantDataSO candidate in variants)
        {
            if (candidate == null)
                continue;

            if (string.Equals(candidate.VariantId, normalizedVariantId, StringComparison.Ordinal))
            {
                variant = candidate;
                return true;
            }
        }

        variant = null;
        return false;
    }

    public bool TryGetNextVariant(
        string currentVariantId,
        bool repeatLastVariant,
        out SceneVariantDataSO nextVariant)
    {
        nextVariant = null;

        if (variants == null || variants.Count == 0)
            return false;

        string normalizedCurrentId = SceneVariantDataSO.Normalize(currentVariantId);
        int currentIndex = normalizedCurrentId.Length == 0 ? FindFirstVariantIndex() : -1;

        if (currentIndex < 0)
        {
            for (int i = 0; i < variants.Count; i++)
            {
                SceneVariantDataSO candidate = variants[i];
                if (candidate == null)
                    continue;

                if (string.Equals(candidate.VariantId, normalizedCurrentId, StringComparison.Ordinal))
                {
                    currentIndex = i;
                    break;
                }
            }
        }

        if (currentIndex < 0)
        {
            nextVariant = FirstVariant;
            return nextVariant != null;
        }

        for (int i = currentIndex + 1; i < variants.Count; i++)
        {
            SceneVariantDataSO candidate = variants[i];
            if (candidate == null || candidate.VariantId.Length == 0)
                continue;

            nextVariant = candidate;
            return true;
        }

        if (!repeatLastVariant)
            return false;

        nextVariant = variants[currentIndex];
        return nextVariant != null;
    }

    private int FindFirstVariantIndex()
    {
        if (variants == null)
            return -1;

        for (int i = 0; i < variants.Count; i++)
        {
            SceneVariantDataSO candidate = variants[i];
            if (candidate != null && candidate.VariantId.Length > 0)
                return i;
        }

        return -1;
    }
}
