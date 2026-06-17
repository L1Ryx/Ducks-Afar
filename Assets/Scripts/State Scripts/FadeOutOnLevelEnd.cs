using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FadeOutOnLevelEnd : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image overlayImage;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Timing")]
    [Tooltip("Optional delay after level end before starting the fade.")]
    [SerializeField] private float startDelaySeconds = 0f;

    [Header("Events")]
    [Tooltip("Raised when fade-to-black finishes.")]
    [SerializeField] private GameEvent levelFadeOutComplete;
    [Tooltip("Raised after the active save slot is successfully written at fade-to-black.")]
    [SerializeField] private GameEvent savedEvent;

    [Header("Behavior")]
    [Tooltip("If true, blocks clicks during the fade-out.")]
    [SerializeField] private bool blockRaycastsDuringFade = true;
    [Tooltip("If true, saves the active slot after the curtain has fully faded to black.")]
    [SerializeField] private bool saveActiveSlotWhenBlack = true;
    [Tooltip("If true, runs the scene checkpoint capture before saving. Leave off for level-end progress saves.")]
    [SerializeField] private bool captureSceneCheckpointOnSave = false;

    private Coroutine fadeRoutine;

    private void Reset()
    {
        overlayImage = GetComponentInChildren<Image>();
#if UNITY_EDITOR
        AssignEditorDefaults();
#endif
    }

    private void Awake()
    {
        if (overlayImage == null)
        {
            Debug.LogError($"{nameof(FadeOutOnLevelEnd)}: overlayImage is not assigned.", this);
            enabled = false;
            return;
        }

        // For a fade-out-on-end component, we generally start transparent.
        // (If another component sets this differently, that's fine—this is just a sane default.)
        SetAlpha(0f);
        overlayImage.raycastTarget = false;
    }

    /// <summary>
    /// Call this from a GameEventListener Response (e.g., LevelCompletedEvent).
    /// Uses the serialized startDelaySeconds.
    /// </summary>
    public void PlayFadeToBlack()
    {
        PlayFadeToBlackWithDelay(startDelaySeconds);
    }

    /// <summary>
    /// Optional alternative entry point if you want to trigger with a custom delay via code.
    /// UnityEvent cannot easily pass floats unless you wire it explicitly, so this is mostly for code.
    /// </summary>
    public void PlayFadeToBlackWithDelay(float delaySeconds)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeToBlackRoutine(delaySeconds));
    }

    private IEnumerator FadeToBlackRoutine(float delaySeconds)
    {
        if (delaySeconds > 0f)
            yield return new WaitForSeconds(delaySeconds);

        if (blockRaycastsDuringFade)
            overlayImage.raycastTarget = true;

        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / fadeDuration);
            float eased = fadeCurve.Evaluate(normalized);

            // 0 -> 1 over time
            SetAlpha(eased);
            yield return null;
        }

        SetAlpha(1f);

        SaveActiveSlotIfWanted();

        // Fade complete event
        if (levelFadeOutComplete != null)
            levelFadeOutComplete.Raise();

        fadeRoutine = null;
    }

    private void SetAlpha(float alpha)
    {
        var c = overlayImage.color;
        c.a = alpha;
        overlayImage.color = c;
    }

    private void SaveActiveSlotIfWanted()
    {
        if (!saveActiveSlotWhenBlack)
            return;

        if (!Game.IsReady || Game.Ctx?.Saves == null || Game.Ctx.SaveState == null)
        {
            Debug.LogWarning($"{nameof(FadeOutOnLevelEnd)}: skipped level-end save because GameContext is not ready.", this);
            return;
        }

        if (!Game.Ctx.SaveState.HasActiveSlot)
        {
            Debug.LogWarning($"{nameof(FadeOutOnLevelEnd)}: skipped level-end save because no active save slot is loaded.", this);
            return;
        }

        if (!Game.Ctx.Saves.SaveToActiveSlot(captureSceneCheckpointOnSave))
            return;

        if (savedEvent != null)
        {
            savedEvent.Raise();
        }
        else
        {
            Debug.LogWarning($"{nameof(FadeOutOnLevelEnd)}: saved successfully, but no savedEvent is assigned for the progress UI.", this);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AssignEditorDefaults();
    }

    private void AssignEditorDefaults()
    {
        if (savedEvent == null)
            savedEvent = AssetDatabase.LoadAssetAtPath<GameEvent>("Assets/SOs/Events/OnGameSaved.asset");
    }
#endif
}
