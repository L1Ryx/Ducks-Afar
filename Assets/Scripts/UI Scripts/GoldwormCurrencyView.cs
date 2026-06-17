using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GoldwormCurrencyView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup sectionGroup;
    [SerializeField] private TMP_Text amountText;

    [Header("Visibility")]
    [SerializeField, Min(0f)] private float fadeInSeconds = 0.18f;
    [SerializeField, Min(0f)] private float fadeOutSeconds = 0.35f;
    [SerializeField, Range(0f, 1f)] private float visibleAlpha = 1f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Optional Game Event API")]
    [SerializeField] private GameEvent showIndefinitelyEvent;
    [SerializeField] private GameEvent hideIndefinitelyEvent;

    private SaveStateModel boundSaveState;
    private Coroutine bindRoutine;
    private Coroutine fadeRoutine;
    private bool pinnedVisible;
    private bool lastSceneAllowsUI;

    private void Awake()
    {
        if (sectionGroup == null)
            sectionGroup = GetComponent<CanvasGroup>();

        if (amountText == null)
            amountText = GetComponentInChildren<TMP_Text>(true);

        if (sectionGroup != null)
        {
            sectionGroup.interactable = false;
            sectionGroup.blocksRaycasts = false;
        }
    }

    private void OnEnable()
    {
        showIndefinitelyEvent?.RegisterRuntimeListener(ShowIndefinitely);
        hideIndefinitelyEvent?.RegisterRuntimeListener(HideIndefinitely);
        SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        bindRoutine = StartCoroutine(BindWhenReady());
    }

    private void OnDisable()
    {
        showIndefinitelyEvent?.UnregisterRuntimeListener(ShowIndefinitely);
        hideIndefinitelyEvent?.UnregisterRuntimeListener(HideIndefinitely);
        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (bindRoutine != null)
        {
            StopCoroutine(bindRoutine);
            bindRoutine = null;
        }

        StopFadeRoutine();
        UnbindSaveState();
    }

    public void ShowIndefinitely()
    {
        pinnedVisible = true;
        RefreshVisibility(fade: true);
    }

    public void HideIndefinitely()
    {
        pinnedVisible = false;
        RefreshVisibility(fade: true);
    }

    public void SetIndefiniteVisible(bool visible)
    {
        if (visible)
            ShowIndefinitely();
        else
            HideIndefinitely();
    }

    public void PrepareForSceneTransition()
    {
        if (sectionGroup == null || sectionGroup.alpha <= 0f)
            return;

        FadeTo(0f, fadeOutSeconds);
    }

    public void ShowTemporarily()
    {
        ShowIndefinitely();
    }

    public void RefreshAmount()
    {
        SetAmount(boundSaveState != null ? boundSaveState.Goldworms : 0);
    }

    private IEnumerator BindWhenReady()
    {
        while (!Game.IsReady || Game.Ctx?.SaveState == null)
            yield return null;

        BindSaveState(Game.Ctx.SaveState);
        RefreshAmount();
        lastSceneAllowsUI = IsGoldwormCurrencyAllowedInActiveScene();
        SetAlpha(ShouldBeVisible() ? visibleAlpha : 0f);

        bindRoutine = null;
    }

    private void BindSaveState(SaveStateModel saveState)
    {
        if (boundSaveState == saveState)
            return;

        UnbindSaveState();
        boundSaveState = saveState;
        boundSaveState.GoldwormsChanged += HandleGoldwormsChanged;
        boundSaveState.GoldwormCollectionStateChanged += HandleGoldwormCollectionStateChanged;
    }

    private void UnbindSaveState()
    {
        if (boundSaveState == null)
            return;

        boundSaveState.GoldwormsChanged -= HandleGoldwormsChanged;
        boundSaveState.GoldwormCollectionStateChanged -= HandleGoldwormCollectionStateChanged;
        boundSaveState = null;
    }

    private void HandleGoldwormsChanged(int amount)
    {
        SetAmount(amount);
        RefreshVisibility(fade: true);
    }

    private void HandleGoldwormCollectionStateChanged(bool hasCollected)
    {
        RefreshVisibility(fade: true);
    }

    private void SetAmount(int amount)
    {
        if (amountText != null)
            amountText.text = Mathf.Max(0, amount).ToString();
    }

    private void HandleActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        RefreshVisibilityForSceneChange();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshVisibilityForSceneChange();
    }

    private void RefreshVisibilityForSceneChange()
    {
        bool sceneAllowsUI = IsGoldwormCurrencyAllowedInActiveScene();
        bool shouldFade = lastSceneAllowsUI != sceneAllowsUI;
        lastSceneAllowsUI = sceneAllowsUI;
        RefreshVisibility(shouldFade);
    }

    private void RefreshVisibility(bool fade)
    {
        float targetAlpha = ShouldBeVisible() ? visibleAlpha : 0f;
        float duration = targetAlpha > 0f ? fadeInSeconds : fadeOutSeconds;

        if (fade)
            FadeTo(targetAlpha, duration);
        else
            SetAlpha(targetAlpha);
    }

    private bool ShouldBeVisible()
    {
        if (!IsGoldwormCurrencyAllowedInActiveScene())
            return false;

        return pinnedVisible || boundSaveState?.HasCollectedGoldworms == true;
    }

    private static bool IsGoldwormCurrencyAllowedInActiveScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        SceneSettings[] settings = FindObjectsByType<SceneSettings>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (SceneSettings candidate in settings)
        {
            if (candidate != null && candidate.gameObject.scene == activeScene)
                return candidate.AllowGoldwormCurrencyUI;
        }

        return false;
    }

    private void FadeTo(float targetAlpha, float duration)
    {
        StopFadeRoutine();
        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, duration));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        if (sectionGroup == null)
            yield break;

        float startAlpha = sectionGroup.alpha;
        if (duration <= 0f)
        {
            SetAlpha(targetAlpha);
            fadeRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += GetDeltaTime();
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetAlpha(targetAlpha);
        fadeRoutine = null;
    }

    private void SetAlpha(float alpha)
    {
        if (sectionGroup != null)
            sectionGroup.alpha = Mathf.Clamp01(alpha);
    }

    private void StopFadeRoutine()
    {
        if (fadeRoutine == null)
            return;

        StopCoroutine(fadeRoutine);
        fadeRoutine = null;
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }
}
