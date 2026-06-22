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
    private Coroutine baseRoutine;
    private Coroutine burstRoutine;
    private static PostProcessEffectToolbox activeInstance;
    private float baseChromatic;
    private float baseVignette;
    private float baseBloom;
    private float baseGrain;
    private float baseSaturation;
    private float baseContrast;
    private float burstAmount;

    private void Awake()
    {
        activeInstance = this;
        EnsureVolume();
        ResetAllImmediate();
    }

    private void OnDestroy()
    {
        if (activeInstance == this)
            activeInstance = null;
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
        ProjectAudio.PlayGlobal(burstCue);
        PlayGlitchBurstVisual();
    }

    public void PlayGlitchBurstVisual()
    {
        StopBurstRoutine();
        burstRoutine = StartCoroutine(GlitchBurstRoutine());
    }

    public static void PlayGlitchBurstVisualGlobal()
    {
        EnsureActiveInstance();
        activeInstance?.PlayGlitchBurstVisual();
    }

    public static void PlayGlitchBurstGlobal()
    {
        EnsureActiveInstance();
        activeInstance?.PlayGlitchBurst();
    }

    public static void PlayGlitchBurstGlobal(bool playAudio)
    {
        if (playAudio)
        {
            PlayGlitchBurstGlobal();
            return;
        }

        PlayGlitchBurstVisualGlobal();
    }

    public void SetCreepyVignette()
    {
        StopBaseRoutine();
        baseRoutine = StartCoroutine(FadeTo(
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
        StopBaseRoutine();
        baseRoutine = StartCoroutine(FadeTo(
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
        StopBaseRoutine();
        baseRoutine = StartCoroutine(FadeTo(
            chromaticTarget: 0f,
            vignetteTarget: 0.28f,
            bloomTarget: 0f,
            grainTarget: 0.18f,
            saturationTarget: desaturateSaturation,
            contrastTarget: desaturateContrast,
            desaturateFadeSeconds));
    }

    public static void ApplyRoomLook(float creepyVignettePercent, float dreamBloomPercent, float desaturatePercent, float fadeSeconds)
    {
        EnsureActiveInstance();

        activeInstance?.SetRoomLook(creepyVignettePercent, dreamBloomPercent, desaturatePercent, fadeSeconds);
    }

    public void SetRoomLook(float creepyVignettePercent, float dreamBloomPercent, float desaturatePercent, float fadeSeconds)
    {
        EnsureVolume();

        float creepy = PercentTo01(creepyVignettePercent);
        float dream = PercentTo01(dreamBloomPercent);
        float desaturated = PercentTo01(desaturatePercent);

        float chromaticTarget = dream * 0.06f;
        float vignetteTarget = Mathf.Max(
            creepy * creepyVignetteIntensity,
            dream * 0.16f,
            desaturated * 0.28f);
        float bloomTarget = dream * dreamBloomIntensity;
        float grainTarget = Mathf.Max(
            creepy * 0.12f,
            dream * 0.08f,
            desaturated * 0.18f);
        float saturationTarget = Mathf.Clamp(
            dream * dreamSaturation + desaturated * desaturateSaturation,
            -100f,
            100f);
        float contrastTarget = desaturated * desaturateContrast;

        StopBaseRoutine();
        baseRoutine = StartCoroutine(FadeTo(
            chromaticTarget,
            vignetteTarget,
            bloomTarget,
            grainTarget,
            saturationTarget,
            contrastTarget,
            Mathf.Max(0f, fadeSeconds)));
    }

    public void ResetAll()
    {
        StopBaseRoutine();
        StopBurstRoutine();
        baseRoutine = StartCoroutine(FadeTo(0f, 0f, 0f, 0f, 0f, 0f, 0.25f));
    }

    public void ResetAllImmediate()
    {
        SetBaseValues(0f, 0f, 0f, 0f, 0f, 0f);
        burstAmount = 0f;
        ApplyCombinedValues();
    }

    public void SetVignetteHigh()
    {
        SetCreepyVignette();
    }

    public void ClearVignette()
    {
        StopBaseRoutine();
        baseRoutine = StartCoroutine(FadeTo(
            baseChromatic,
            0f,
            baseBloom,
            baseGrain,
            baseSaturation,
            baseContrast,
            creepyFadeSeconds));
    }

    private IEnumerator GlitchBurstRoutine()
    {
        yield return LerpBurst(0f, 1f, burstRampUp);
        yield return LerpBurst(1f, 0f, burstRampDown);
        burstRoutine = null;
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
        burstAmount = Mathf.Clamp01(t);
        ApplyCombinedValues();
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
        float chromaticStart = baseChromatic;
        float vignetteStart = baseVignette;
        float bloomStart = baseBloom;
        float grainStart = baseGrain;
        float saturationStart = baseSaturation;
        float contrastStart = baseContrast;

        if (duration <= 0f)
        {
            SetBaseValues(chromaticTarget, vignetteTarget, bloomTarget, grainTarget, saturationTarget, contrastTarget);
            ApplyCombinedValues();
            baseRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);

            SetBaseValues(
                Mathf.Lerp(chromaticStart, chromaticTarget, t),
                Mathf.Lerp(vignetteStart, vignetteTarget, t),
                Mathf.Lerp(bloomStart, bloomTarget, t),
                Mathf.Lerp(grainStart, grainTarget, t),
                Mathf.Lerp(saturationStart, saturationTarget, t),
                Mathf.Lerp(contrastStart, contrastTarget, t));
            ApplyCombinedValues();

            yield return null;
        }

        SetBaseValues(chromaticTarget, vignetteTarget, bloomTarget, grainTarget, saturationTarget, contrastTarget);
        ApplyCombinedValues();
        baseRoutine = null;
    }

    private void EnsureVolume()
    {
        if (volume == null)
            volume = GetComponent<Volume>();

        if (volume == null)
            volume = gameObject.AddComponent<Volume>();

        volume.isGlobal = true;
        volume.priority = Mathf.Max(volume.priority, 110f);

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

    private static void EnsureActiveInstance()
    {
        if (activeInstance != null)
            return;

        activeInstance = FindAnyObjectByType<PostProcessEffectToolbox>();
        if (activeInstance != null)
            return;

        GameObject toolboxObject = new GameObject("Runtime Post Process Effect Toolbox");
        activeInstance = toolboxObject.AddComponent<PostProcessEffectToolbox>();
    }

    private static float PercentTo01(float value)
    {
        return Mathf.Clamp01(value / 100f);
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

    private void SetBaseValues(float chromaticValue, float vignetteValue, float bloomValue, float grainValue, float saturationValue, float contrastValue)
    {
        baseChromatic = chromaticValue;
        baseVignette = vignetteValue;
        baseBloom = bloomValue;
        baseGrain = grainValue;
        baseSaturation = saturationValue;
        baseContrast = contrastValue;
    }

    private void ApplyCombinedValues()
    {
        ApplyValues(
            Mathf.Clamp01(baseChromatic + burstAmount * burstChromatic),
            Mathf.Clamp01(baseVignette + burstAmount * burstVignette),
            baseBloom + burstAmount * burstBloom,
            Mathf.Clamp01(baseGrain + burstAmount * burstGrain),
            baseSaturation,
            baseContrast);
    }

    private void StopBaseRoutine()
    {
        if (baseRoutine == null)
            return;

        StopCoroutine(baseRoutine);
        baseRoutine = null;
    }

    private void StopBurstRoutine()
    {
        if (burstRoutine == null)
            return;

        StopCoroutine(burstRoutine);
        burstRoutine = null;
        burstAmount = 0f;
        ApplyCombinedValues();
    }
}
