using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessBurstHandler : MonoBehaviour
{
    [Header("Volume Reference")]
    [SerializeField] private Volume volume;
    [SerializeField] private bool cloneProfileAtRuntime = true;
    [Header("Burst Cooldown")]
    [SerializeField] private float burstCooldown = 0.3f; // seconds

    private float lastBurstTime = -Mathf.Infinity;


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

        VolumeProfile profile = volume.profile;
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile = profile;
        }
        else if (cloneProfileAtRuntime)
        {
            profile = Instantiate(profile);
            volume.profile = profile;
        }

        EnsureEffects(profile);

        ResetToIdle();
    }

    public void Burst()
    {
        if (Time.unscaledTime - lastBurstTime < burstCooldown)
            return;

        lastBurstTime = Time.unscaledTime;

        if (burstRoutine != null)
            StopCoroutine(burstRoutine);

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

    private void EnsureEffects(VolumeProfile profile)
    {
        if (profile == null)
            return;

        if (!profile.TryGet(out chromatic))
            chromatic = profile.Add<ChromaticAberration>(true);

        if (!profile.TryGet(out vignette))
            vignette = profile.Add<Vignette>(true);

        if (!profile.TryGet(out bloom))
            bloom = profile.Add<Bloom>(true);

        if (!profile.TryGet(out grain))
            grain = profile.Add<FilmGrain>(true);

        chromatic.intensity.overrideState = true;
        vignette.intensity.overrideState = true;
        bloom.intensity.overrideState = true;
        grain.intensity.overrideState = true;
    }
}
