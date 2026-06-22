using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class PostProcessEffectToolbox : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField] private Volume volume;
    [SerializeField] private bool cloneProfileAtRuntime = true;

    [Header("Game Event Triggers")]
    [SerializeField] private GameEvent glitchBurstEvent;
    [SerializeField] private GameEvent creepyVignetteEvent;
    [SerializeField] private GameEvent dreamBloomEvent;
    [SerializeField] private GameEvent desaturateEvent;
    [SerializeField] private GameEvent resetEvent;

    [Header("Glitch Burst")]
    [SerializeField, Min(0f)] private float burstRampUp = 0.05f;
    [SerializeField, Min(0f)] private float burstRampDown = 0.45f;
    [SerializeField, Range(0f, 1f)] private float burstChromatic = 1f;
    [SerializeField, Range(0f, 1f)] private float burstVignette = 0.45f;
    [SerializeField, Min(0f)] private float burstBloom = 10f;
    [SerializeField, Range(0f, 1f)] private float burstGrain = 0.6f;
    [SerializeField] private AudioCue burstCue;

    [Header("Creepy Vignette")]
    [SerializeField, Range(0f, 1f)] private float creepyVignetteIntensity = 0.58f;
    [SerializeField, Min(0f)] private float creepyFadeSeconds = 0.45f;

    [Header("Dream Bloom")]
    [SerializeField, Min(0f)] private float dreamBloomIntensity = 3f;
    [SerializeField, Range(-100f, 100f)] private float dreamSaturation = 12f;
    [SerializeField, Min(0f)] private float dreamFadeSeconds = 0.5f;

    [Header("Desaturate")]
    [SerializeField, Range(-100f, 100f)] private float desaturateSaturation = -75f;
    [SerializeField, Range(-100f, 100f)] private float desaturateContrast = 16f;
    [SerializeField, Min(0f)] private float desaturateFadeSeconds = 0.5f;

    private ChromaticAberration chromatic;
    private Vignette vignette;
    private Bloom bloom;
    private FilmGrain grain;
    private ColorAdjustments colorAdjustments;
    private Coroutine activeRoutine;

    private void Awake()
    {
        EnsureVolume();
        ResetAllImmediate();
    }

    private void OnEnable()
    {
        glitchBurstEvent?.RegisterRuntimeListener(PlayGlitchBurst);
        creepyVignetteEvent?.RegisterRuntimeListener(SetCreepyVignette);
        dreamBloomEvent?.RegisterRuntimeListener(SetDreamBloom);
        desaturateEvent?.RegisterRuntimeListener(SetDesaturate);
        resetEvent?.RegisterRuntimeListener(ResetAll);
    }

    private void OnDisable()
    {
        glitchBurstEvent?.UnregisterRuntimeListener(PlayGlitchBurst);
        creepyVignetteEvent?.UnregisterRuntimeListener(SetCreepyVignette);
        dreamBloomEvent?.UnregisterRuntimeListener(SetDreamBloom);
        desaturateEvent?.UnregisterRuntimeListener(SetDesaturate);
        resetEvent?.UnregisterRuntimeListener(ResetAll);
    }

    public void PlayGlitchBurst()
    {
        StopActiveRoutine();
        ProjectAudio.PlayGlobal(burstCue);
        activeRoutine = StartCoroutine(GlitchBurstRoutine());
    }

    public void SetCreepyVignette()
    {
        StopActiveRoutine();
        activeRoutine = StartCoroutine(FadeTo(
            chromaticTarget: 0f,
            vignetteTarget: creepyVignetteIntensity,
            bloomTarget: 0f,
            grainTarget: 0.12f,
            saturationTarget: 0f,
            contrastTarget: 0f,
            creepyFadeSeconds));
    }

    public void SetDreamBloom()
    {
        StopActiveRoutine();
        activeRoutine = StartCoroutine(FadeTo(
            chromaticTarget: 0.06f,
            vignetteTarget: 0.16f,
            bloomTarget: dreamBloomIntensity,
            grainTarget: 0.08f,
            saturationTarget: dreamSaturation,
            contrastTarget: 0f,
            dreamFadeSeconds));
    }

    public void SetDesaturate()
    {
        StopActiveRoutine();
        activeRoutine = StartCoroutine(FadeTo(
            chromaticTarget: 0f,
            vignetteTarget: 0.28f,
            bloomTarget: 0f,
            grainTarget: 0.18f,
            saturationTarget: desaturateSaturation,
            contrastTarget: desaturateContrast,
            desaturateFadeSeconds));
    }

    public void ResetAll()
    {
        StopActiveRoutine();
        activeRoutine = StartCoroutine(FadeTo(0f, 0f, 0f, 0f, 0f, 0f, 0.25f));
    }

    public void ResetAllImmediate()
    {
        ApplyValues(0f, 0f, 0f, 0f, 0f, 0f);
    }

    public void SetVignetteHigh()
    {
        SetCreepyVignette();
    }

    public void ClearVignette()
    {
        StopActiveRoutine();
        activeRoutine = StartCoroutine(FadeTo(
            GetChromatic(),
            0f,
            GetBloom(),
            GetGrain(),
            GetSaturation(),
            GetContrast(),
            creepyFadeSeconds));
    }

    private IEnumerator GlitchBurstRoutine()
    {
        yield return LerpBurst(0f, 1f, burstRampUp);
        yield return LerpBurst(1f, 0f, burstRampDown);
        activeRoutine = null;
    }

    private IEnumerator LerpBurst(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            ApplyBurst(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            ApplyBurst(Mathf.Lerp(from, to, t));
            yield return null;
        }

        ApplyBurst(to);
    }

    private void ApplyBurst(float t)
    {
        ApplyValues(
            t * burstChromatic,
            t * burstVignette,
            t * burstBloom,
            t * burstGrain,
            GetSaturation(),
            GetContrast());
    }

    private IEnumerator FadeTo(
        float chromaticTarget,
        float vignetteTarget,
        float bloomTarget,
        float grainTarget,
        float saturationTarget,
        float contrastTarget,
        float duration)
    {
        float chromaticStart = GetChromatic();
        float vignetteStart = GetVignette();
        float bloomStart = GetBloom();
        float grainStart = GetGrain();
        float saturationStart = GetSaturation();
        float contrastStart = GetContrast();

        if (duration <= 0f)
        {
            ApplyValues(chromaticTarget, vignetteTarget, bloomTarget, grainTarget, saturationTarget, contrastTarget);
            activeRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);

            ApplyValues(
                Mathf.Lerp(chromaticStart, chromaticTarget, t),
                Mathf.Lerp(vignetteStart, vignetteTarget, t),
                Mathf.Lerp(bloomStart, bloomTarget, t),
                Mathf.Lerp(grainStart, grainTarget, t),
                Mathf.Lerp(saturationStart, saturationTarget, t),
                Mathf.Lerp(contrastStart, contrastTarget, t));

            yield return null;
        }

        ApplyValues(chromaticTarget, vignetteTarget, bloomTarget, grainTarget, saturationTarget, contrastTarget);
        activeRoutine = null;
    }

    private void EnsureVolume()
    {
        if (volume == null)
            volume = GetComponent<Volume>();

        if (volume == null)
            volume = gameObject.AddComponent<Volume>();

        volume.isGlobal = true;

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

        chromatic = GetOrAdd<ChromaticAberration>(profile);
        vignette = GetOrAdd<Vignette>(profile);
        bloom = GetOrAdd<Bloom>(profile);
        grain = GetOrAdd<FilmGrain>(profile);
        colorAdjustments = GetOrAdd<ColorAdjustments>(profile);

        chromatic.intensity.overrideState = true;
        vignette.intensity.overrideState = true;
        vignette.smoothness.overrideState = true;
        vignette.rounded.overrideState = true;
        bloom.intensity.overrideState = true;
        grain.intensity.overrideState = true;
        colorAdjustments.saturation.overrideState = true;
        colorAdjustments.contrast.overrideState = true;

        vignette.smoothness.value = 0.7f;
        vignette.rounded.value = true;
    }

    private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
    {
        return profile.TryGet(out T component) ? component : profile.Add<T>(true);
    }

    private void ApplyValues(float chromaticValue, float vignetteValue, float bloomValue, float grainValue, float saturationValue, float contrastValue)
    {
        if (chromatic != null) chromatic.intensity.value = chromaticValue;
        if (vignette != null) vignette.intensity.value = vignetteValue;
        if (bloom != null) bloom.intensity.value = bloomValue;
        if (grain != null) grain.intensity.value = grainValue;
        if (colorAdjustments != null)
        {
            colorAdjustments.saturation.value = saturationValue;
            colorAdjustments.contrast.value = contrastValue;
        }
    }

    private float GetChromatic() => chromatic != null ? chromatic.intensity.value : 0f;
    private float GetVignette() => vignette != null ? vignette.intensity.value : 0f;
    private float GetBloom() => bloom != null ? bloom.intensity.value : 0f;
    private float GetGrain() => grain != null ? grain.intensity.value : 0f;
    private float GetSaturation() => colorAdjustments != null ? colorAdjustments.saturation.value : 0f;
    private float GetContrast() => colorAdjustments != null ? colorAdjustments.contrast.value : 0f;

    private void StopActiveRoutine()
    {
        if (activeRoutine == null)
            return;

        StopCoroutine(activeRoutine);
        activeRoutine = null;
    }
}
