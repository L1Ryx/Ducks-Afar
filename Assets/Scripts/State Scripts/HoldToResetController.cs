using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class HoldToResetController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference resetAction;
    [SerializeField] private bool useKeyboardFallback = true;
    [SerializeField] private Key fallbackKey = Key.R;
    [SerializeField] private bool sceneRestartAllowed = false;

    [Header("Timing")]
    [Min(0.1f)] [SerializeField] private float holdDuration = 1.5f;
    [Min(0f)] [SerializeField] private float fadeToBlackDuration = 0.5f;
    [Min(0.01f)] [SerializeField] private float releaseRecoveryDuration = 0.35f;

    [Header("Post Processing")]
    [SerializeField] private int volumePriority = 999;
    [Range(0f, 1f)] [SerializeField] private float maxFilmGrainIntensity = 1f;
    [Range(0f, 1f)] [SerializeField] private float maxChromaticAberrationIntensity = 1f;
    [SerializeField] private float targetSaturation = -100f;
    [SerializeField] private float targetPostExposure = -10f;
    [Min(0.1f)] [SerializeField] private float holdEffectCurvePower = 3f;

    private Volume resetVolume;
    private VolumeProfile resetProfile;
    private FilmGrain filmGrain;
    private ChromaticAberration chromaticAberration;
    private ColorAdjustments colorAdjustments;

    private float progress;
    private float fadeToBlackProgress;
    private float holdTimer;
    private float fadeToBlackTimer;
    private bool hasTriggeredReset;
    private bool isFadingToBlack;
    private bool wasHoldingReset;

    public void SetSceneRestartAllowed(bool allowed)
    {
        sceneRestartAllowed = allowed;

        if (!sceneRestartAllowed && !isFadingToBlack)
            ResetHoldProgress();
    }

    public bool ForceReset()
    {
        if (hasTriggeredReset || isFadingToBlack)
            return false;

        if (!Game.IsReady || Game.Ctx?.SceneLoader == null || Game.Ctx.SceneLoader.IsLoading)
            return false;

        TriggerReset();
        return true;
    }

    private void Awake()
    {
        EnsureVolume();
        ApplyProgress(0f);
    }

    private void Start()
    {
        ApplyRestartProgressRtpc(progress);
    }

    private void OnEnable()
    {
        if (resetAction != null && resetAction.action != null)
            resetAction.action.Enable();

        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        if (resetAction != null && resetAction.action != null)
            resetAction.action.Disable();

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        wasHoldingReset = false;
        ApplyFadeToBlackProgress(0f);
        ApplyProgress(0f);
        ApplyRestartProgressRtpc(0f);
    }

    private void Update()
    {
        bool pressed = IsResetPressed();

        if (isFadingToBlack)
        {
            UpdateFadeToBlack();
            return;
        }

        if (hasTriggeredReset)
            return;

        if (pressed && CanResetNow())
        {
            if (!wasHoldingReset)
            {
                wasHoldingReset = true;
                ApplyRestartProgressRtpc(0f);
            }

            holdTimer += Time.unscaledDeltaTime;
            float normalizedHoldProgress = Mathf.Clamp01(holdTimer / holdDuration);
            ApplyProgress(normalizedHoldProgress);
            ApplyRestartProgressRtpc(normalizedHoldProgress);

            if (holdTimer >= holdDuration)
                TriggerReset();

            return;
        }

        if (wasHoldingReset)
        {
            wasHoldingReset = false;
            ApplyRestartProgressRtpc(0f);
        }

        RecoverTowardIdle();
    }

    private bool IsResetPressed()
    {
        if (resetAction != null && resetAction.action != null)
            return resetAction.action.IsPressed();

        return useKeyboardFallback &&
               Keyboard.current != null &&
               Keyboard.current[fallbackKey].isPressed;
    }

    private bool CanResetNow()
    {
        if (!sceneRestartAllowed)
            return false;

        if (!Game.IsReady || Game.Ctx?.SceneLoader == null)
            return false;

        if (PauseUtility.IsPaused)
            return false;

        if (Game.Ctx.SceneLoader.IsLoading)
            return false;

        if (Game.Ctx.InteractionLock?.IsLocked == true)
            return false;

        return true;
    }

    private void ResetHoldProgress()
    {
        holdTimer = 0f;
        wasHoldingReset = false;
        ApplyFadeToBlackProgress(0f);
        ApplyProgress(0f);
        ApplyRestartProgressRtpc(0f);
    }

    private void RecoverTowardIdle()
    {
        holdTimer = 0f;
        ApplyFadeToBlackProgress(0f);

        if (progress <= 0f)
            return;

        float rate = releaseRecoveryDuration > 0f ? 1f / releaseRecoveryDuration : float.PositiveInfinity;
        ApplyProgress(Mathf.MoveTowards(progress, 0f, rate * Time.unscaledDeltaTime));
    }

    private void TriggerReset()
    {
        hasTriggeredReset = true;
        wasHoldingReset = false;
        holdTimer = 0f;
        ApplyProgress(1f);
        ApplyRestartProgressRtpc(1f);
        ProjectAudio.PlayGlobal(ProjectAudio.Config != null ? ProjectAudio.Config.RestartSuccessCue : null);
        ProjectAudio.StopMusicAndAmbienceImmediate();

        isFadingToBlack = true;
        fadeToBlackTimer = 0f;

        if (fadeToBlackDuration <= 0f)
            CompleteReset();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        holdTimer = 0f;
        fadeToBlackTimer = 0f;
        isFadingToBlack = false;
        hasTriggeredReset = false;
        wasHoldingReset = false;
        ApplyFadeToBlackProgress(0f);
        ApplyProgress(0f);
        ApplyRestartProgressRtpc(0f);
    }

    private void EnsureVolume()
    {
        if (resetVolume != null)
            return;

        var volumeObject = new GameObject("Hold To Reset PP Volume");
        volumeObject.transform.SetParent(transform, false);
        volumeObject.hideFlags = HideFlags.DontSave;

        resetVolume = volumeObject.AddComponent<Volume>();
        resetVolume.isGlobal = true;
        resetVolume.priority = volumePriority;
        resetVolume.weight = 0f;

        resetProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        resetProfile.hideFlags = HideFlags.HideAndDontSave;

        filmGrain = resetProfile.Add<FilmGrain>(true);
        filmGrain.intensity.overrideState = true;
        filmGrain.response.overrideState = true;
        filmGrain.response.value = 1f;

        chromaticAberration = resetProfile.Add<ChromaticAberration>(true);
        chromaticAberration.intensity.overrideState = true;

        colorAdjustments = resetProfile.Add<ColorAdjustments>(true);
        colorAdjustments.saturation.overrideState = true;
        colorAdjustments.postExposure.overrideState = true;

        resetVolume.sharedProfile = resetProfile;
    }

    private void ApplyProgress(float value)
    {
        EnsureVolume();

        progress = Mathf.Clamp01(value);
        resetVolume.weight = progress;
        float curvedProgress = EaseOut(progress);

        if (filmGrain != null)
            filmGrain.intensity.value = curvedProgress * maxFilmGrainIntensity;

        if (chromaticAberration != null)
            chromaticAberration.intensity.value = curvedProgress * maxChromaticAberrationIntensity;

        if (colorAdjustments != null)
        {
            colorAdjustments.saturation.value = Mathf.Lerp(0f, targetSaturation, curvedProgress);
            colorAdjustments.postExposure.value = Mathf.Lerp(0f, targetPostExposure, fadeToBlackProgress);
        }
    }

    private float EaseOut(float t)
    {
        return 1f - Mathf.Pow(1f - Mathf.Clamp01(t), holdEffectCurvePower);
    }

    private void ApplyRestartProgressRtpc(float normalizedProgress)
    {
        ProjectAudio.SetGlobalRtpc(
            ProjectAudio.Config != null ? ProjectAudio.Config.RestartProgressRtpc : null,
            Mathf.Clamp01(normalizedProgress) * 100f);
    }

    private void UpdateFadeToBlack()
    {
        fadeToBlackTimer += Time.unscaledDeltaTime;
        float t = fadeToBlackDuration > 0f ? Mathf.Clamp01(fadeToBlackTimer / fadeToBlackDuration) : 1f;
        ApplyFadeToBlackProgress(t);

        if (t >= 1f)
            CompleteReset();
    }

    private void ApplyFadeToBlackProgress(float value)
    {
        fadeToBlackProgress = Mathf.Clamp01(value);

        if (colorAdjustments != null)
            colorAdjustments.postExposure.value = Mathf.Lerp(0f, targetPostExposure, fadeToBlackProgress);
    }

    private void CompleteReset()
    {
        isFadingToBlack = false;

        if (Game.Ctx?.Saves != null
            && Game.Ctx.SaveState != null
            && Game.Ctx.SaveState.HasActiveSlot
            && Game.Ctx.Saves.LoadFromSlotAndEnterScene(Game.Ctx.SaveState.ActiveSlotIndex))
        {
            return;
        }

        Game.Ctx.SceneLoader.ReloadActiveScene();
    }
}
