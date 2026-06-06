using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class StudioSplashController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image splashImage;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float initialDelay = 0.2f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.75f;
    [SerializeField, Min(0f)] private float holdDuration = 1.4f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.65f;
    [SerializeField, Min(0f)] private float postFadeDelay = 0.25f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Fallback")]
    [SerializeField] private string fallbackNextSceneName = "Title Screen";

    private Tween activeTween;

    private void Awake()
    {
        if (splashImage == null)
            splashImage = GetComponentInChildren<Image>(true);

        SetSplashAlpha(0f);
    }

    private void Start()
    {
        StartCoroutine(PlayRoutine());
    }

    private void OnDestroy()
    {
        activeTween?.Kill();
    }

    private IEnumerator PlayRoutine()
    {
        if (splashImage == null)
        {
            Debug.LogWarning($"{nameof(StudioSplashController)}: splashImage is not assigned.", this);
            AdvanceToNextScene();
            yield break;
        }

        if (initialDelay > 0f)
            yield return WaitForSeconds(initialDelay);

        yield return FadeSplashTo(1f, fadeInDuration);

        if (holdDuration > 0f)
            yield return WaitForSeconds(holdDuration);

        yield return FadeSplashTo(0f, fadeOutDuration);

        if (postFadeDelay > 0f)
            yield return WaitForSeconds(postFadeDelay);

        AdvanceToNextScene();
    }

    private void AdvanceToNextScene()
    {
        if (Game.IsReady)
        {
            if (Game.Ctx?.SceneFlow != null && Game.Ctx.SceneFlow.LoadNextScene())
                return;

            if (Game.Ctx?.SceneLoader != null)
            {
                Game.Ctx.SceneLoader.LoadScene(
                    fallbackNextSceneName,
                    null,
                    SceneLoadPresentation.SilentBlack);
                return;
            }
        }

        SceneManager.LoadScene(fallbackNextSceneName);
    }

    private IEnumerator FadeSplashTo(float targetAlpha, float duration)
    {
        activeTween?.Kill();

        if (duration <= 0f)
        {
            SetSplashAlpha(targetAlpha);
            yield break;
        }

        activeTween = splashImage
            .DOFade(targetAlpha, duration)
            .SetEase(Ease.InOutSine)
            .SetUpdate(useUnscaledTime);

        yield return activeTween.WaitForCompletion();
        activeTween = null;
    }

    private IEnumerator WaitForSeconds(float seconds)
    {
        if (useUnscaledTime)
        {
            float endTime = Time.unscaledTime + seconds;
            while (Time.unscaledTime < endTime)
                yield return null;

            yield break;
        }

        yield return new UnityEngine.WaitForSeconds(seconds);
    }

    private void SetSplashAlpha(float alpha)
    {
        if (splashImage == null)
            return;

        Color color = splashImage.color;
        color.a = alpha;
        splashImage.color = color;
    }
}
