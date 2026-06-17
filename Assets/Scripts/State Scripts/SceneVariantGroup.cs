using UnityEngine;

public sealed class SceneVariantGroup : MonoBehaviour
{
    [Header("Variant")]
    [SerializeField] private SceneVariantDataSO variant;

    [Header("Targets")]
    [Tooltip("If empty, this GameObject is enabled/disabled.")]
    [SerializeField] private GameObject[] targets;

    [SerializeField] private bool invert = false;

    public SceneVariantDataSO Variant => variant;
    public string VariantId => variant != null ? variant.VariantId : string.Empty;

    public void Apply(SceneVariantDataSO activeVariant)
    {
        bool shouldBeActive = Matches(activeVariant);
        if (invert)
            shouldBeActive = !shouldBeActive;

        ApplyActive(shouldBeActive);
    }

    private bool Matches(SceneVariantDataSO activeVariant)
    {
        if (variant == null || activeVariant == null)
            return false;

        if (variant == activeVariant)
            return true;

        return string.Equals(variant.VariantId, activeVariant.VariantId, System.StringComparison.Ordinal);
    }

    private void ApplyActive(bool isActive)
    {
        bool applied = false;

        if (targets != null)
        {
            foreach (GameObject target in targets)
            {
                if (target == null)
                    continue;

                target.SetActive(isActive);
                applied = true;
            }
        }

        if (!applied)
            gameObject.SetActive(isActive);
    }
}
