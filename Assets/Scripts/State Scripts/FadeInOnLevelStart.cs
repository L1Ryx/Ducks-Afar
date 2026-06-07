using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class FadeInOnLevelStart : MonoBehaviour
{
    public static event Action<FadeInOnLevelStart> FadeFromBlackStarted;

    [Header("References")]
    [SerializeField] private Image overlayImage;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Behavior")]
    [SerializeField] private bool disableAfterFade = true;
    [SerializeField] private bool blockRaycastsDuringFade = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onFadeInStarted;
    [SerializeField] private UnityEvent onFadeIn;

    private Coroutine fadeRoutine;

    private void Reset()
    {
        overlayImage = GetComponentInChildren<Image>();
    }

    private void Awake()
    {
        if (overlayImage == null)
        {
            Debug.LogError($"{nameof(FadeInOnLevelStart)}: overlayImage is not assigned.", this);
            enabled = false;
            return;
        }

        SetAlpha(1f);
        overlayImage.raycastTarget = blockRaycastsDuringFade;
    }

    // Hook this up in GameEventListener.Response
    public void PlayFadeFromBlack()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        overlayImage.raycastTarget = blockRaycastsDuringFade;
        FadeFromBlackStarted?.Invoke(this);
        onFadeInStarted?.Invoke();
        fadeRoutine = StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeOutRoutine()
    {
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / fadeDuration);
            float eased = fadeCurve.Evaluate(normalized);

            SetAlpha(1f - eased);
            yield return null;
        }

        SetAlpha(0f);
        overlayImage.raycastTarget = false;
        onFadeIn?.Invoke();

        if (disableAfterFade)
            gameObject.SetActive(false);

        fadeRoutine = null;
    }

    private void SetAlpha(float alpha)
    {
        var c = overlayImage.color;
        c.a = alpha;
        overlayImage.color = c;
    }
}
