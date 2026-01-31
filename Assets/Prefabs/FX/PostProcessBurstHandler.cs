using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessBurstHandler : MonoBehaviour
{
    [Header("Volume Reference")]
    [SerializeField] private Volume volume;

    [Header("Burst Timing")]
    [SerializeField] private float rampUpTime = 0.05f;
    [SerializeField] private float rampDownTime = 0.25f;

    [Header("Burst Peaks")]
    [Range(0f, 1f)][SerializeField] private float chromaticPeak = 1f;
    [Range(0f, 1f)][SerializeField] private float vignettePeak = 0.45f;

    [Header("Optional Peaks (set 0 to disable)")]
    [SerializeField] private float bloomPeak = 2.0f;     // 0 disables
    [Range(0f, 1f)][SerializeField] private float grainPeak = 0.4f; // 0 disables

    private ChromaticAberration chromatic;
    private Vignette vignette;
    private Bloom bloom;
    private FilmGrain grain;

    private Coroutine burstRoutine;

    private void Awake()
    {
        if (volume == null)
        {
            Debug.LogError($"{nameof(PostProcessBurstHandler)}: Volume reference is missing.", this);
            enabled = false;
            return;
        }

        // Note: profile could be shared; that's fine for a simple global effect.
        // If you want per-instance safety, instantiate a copy of the profile at runtime.
        VolumeProfile profile = volume.profile;

        profile.TryGet(out chromatic);
        profile.TryGet(out vignette);
        profile.TryGet(out bloom);
        profile.TryGet(out grain);

        // Ensure the key ones exist
        if (chromatic == null) Debug.LogWarning("ChromaticAberration override not found in Volume profile.");
        if (vignette == null) Debug.LogWarning("Vignette override not found in Volume profile.");

        ResetToIdle();
    }

    public void Burst()
    {
        if (burstRoutine != null) StopCoroutine(burstRoutine);
        burstRoutine = StartCoroutine(BurstRoutine());
    }

    public void ResetToIdle()
    {
        SetChromatic(0f);
        SetVignette(0f);

        if (bloom != null) bloom.intensity.value = 0f;
        if (grain != null) grain.intensity.value = 0f;
    }

    private IEnumerator BurstRoutine()
    {
        // Ramp up fast
        yield return LerpEffects(0f, 1f, rampUpTime);

        // Ramp down slower
        yield return LerpEffects(1f, 0f, rampDownTime);

        burstRoutine = null;
    }

    private IEnumerator LerpEffects(float a, float b, float duration)
    {
        if (duration <= 0f)
        {
            ApplyT(b);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // use unscaled so it still works if you mess with timeScale
            float k = Mathf.Clamp01(t / duration);
            float v = Mathf.Lerp(a, b, k);
            ApplyT(v);
            yield return null;
        }

        ApplyT(b);
    }

    private void ApplyT(float t01)
    {
        // t01 is "how much of the burst" [0..1]
        SetChromatic(t01 * chromaticPeak);
        SetVignette(t01 * vignettePeak);

        if (bloom != null && bloomPeak > 0f)
            bloom.intensity.value = t01 * bloomPeak;

        if (grain != null && grainPeak > 0f)
            grain.intensity.value = t01 * grainPeak;
    }

    private void SetChromatic(float intensity)
    {
        if (chromatic == null) return;
        chromatic.intensity.value = intensity;
    }

    private void SetVignette(float intensity)
    {
        if (vignette == null) return;
        vignette.intensity.value = intensity;
    }
}
