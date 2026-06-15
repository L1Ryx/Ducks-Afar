using UnityEngine;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Attach to any important scene GameObject. When Night is enabled, instantiates a 2D light prefab as a child.
/// Optional overrides: outer radius + color. Optional flicker via DOTween.
/// </summary>
[DisallowMultipleComponent]
public class NightLightAttachment : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("Prefab that contains a Light2D component (e.g., your Spot Light 2D Night Light prefab).")]
    [SerializeField] private GameObject nightLightPrefab;

    [Header("Night Toggle")]
    [Tooltip("If true, the light prefab is instantiated. Default false.")]
    [SerializeField] private bool night = false;

    [Tooltip("Optional. If assigned, the light only exists while this object is active in the hierarchy.")]
    [SerializeField] private GameObject requiredActiveObject;

    [Header("Overrides")]
    [SerializeField] private bool overrideOuterRadius = false;
    [Min(0f)]
    [SerializeField] private float outerRadius = 3f;

    [SerializeField] private bool overrideColor = false;
    [SerializeField] private Color lightColor = Color.white;

    [SerializeField] private bool overrideIntensity = false;
    [Min(0f)]
    [SerializeField] private float intensity = 1f;


    [Header("Flicker")]
    [SerializeField] private bool shouldFlicker = false;

    [Tooltip("Flicker amplitude as a fraction of base radius. Example: 0.05 = ±5%.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float radiusFlickerAmount = 0.05f;

    [Tooltip("Flicker amplitude as a fraction of base intensity. Example: 0.05 = ±5%.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float intensityFlickerAmount = 0.05f;

    [Tooltip("Random duration range (seconds) for each flicker tween step.")]
    [Min(0.01f)]
    [SerializeField] private Vector2 flickerStepDurationRange = new Vector2(0.05f, 0.12f);

    [Tooltip("If true, flicker will also slightly shift color brightness.")]
    [SerializeField] private bool flickerAffectsColor = false;

    [Tooltip("Brightness delta applied during flicker when flickerAffectsColor is enabled (0..1).")]
    [Range(0f, 0.5f)]
    [SerializeField] private float colorValueFlickerAmount = 0.03f;

    [Header("Instantiation")]
    [Tooltip("Local position offset for the instantiated light (relative to this object).")]
    [SerializeField] private Vector3 localOffset = Vector3.zero;

    [Tooltip("If true, uses this object's rotation for the light. If false, uses prefab rotation.")]
    [SerializeField] private bool followRotation = true;

    // Runtime state
    private GameObject _instance;
    private Light2D _light2D;
    private Tween _flickerTween;

    // Cached “base” values for flicker, after overrides are applied
    private float _baseOuterRadius;
    private float _baseIntensity;
    private Color _baseColor;
    private bool _lastVisibilityAllowed;

    private void OnEnable()
    {
        _lastVisibilityAllowed = IsVisibilityAllowed();
        ApplyNightState();
    }

    private void Start()
    {
        // Ensures correct state if values are changed before Start.
        ApplyNightState();
    }

    private void OnDisable()
    {
        StopFlicker();
        DestroyInstance();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Do not spawn/destroy scene objects in edit mode.
        if (!Application.isPlaying) return;

        ApplyNightState();
    }
#endif


    private void LateUpdate()
    {
        bool visibilityAllowed = IsVisibilityAllowed();
        if (visibilityAllowed != _lastVisibilityAllowed)
        {
            _lastVisibilityAllowed = visibilityAllowed;
            ApplyNightState();
        }

        // Optional: keep rotation matched at runtime if desired.
        if (_instance != null && followRotation)
        {
            _instance.transform.rotation = transform.rotation;
        }
    }

    private void ApplyNightState()
    {
        if (!night || !IsVisibilityAllowed())
        {
            StopFlicker();
            DestroyInstance();
            return;
        }

        EnsureInstance();
        ApplyOverrides();
        ApplyFlickerState();
    }

    private void EnsureInstance()
    {
        if (_instance != null) return;

        if (nightLightPrefab == null)
        {
            Debug.LogWarning($"{nameof(NightLightAttachment)} on '{name}' has Night enabled but no prefab assigned.", this);
            return;
        }

        _instance = Instantiate(nightLightPrefab, transform);
        _instance.name = $"{nightLightPrefab.name} (NightLight)";

        _instance.transform.localPosition = localOffset;
        if (followRotation)
            _instance.transform.localRotation = Quaternion.identity;

        _light2D = _instance.GetComponentInChildren<Light2D>();
        if (_light2D == null)
        {
            Debug.LogError($"{nameof(NightLightAttachment)} instantiated prefab '{nightLightPrefab.name}' but could not find a Light2D component.", this);
        }
    }

    private void DestroyInstance()
    {
        if (_instance == null) return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(_instance);
        else
            Destroy(_instance);
#else
        Destroy(_instance);
#endif

        _instance = null;
        _light2D = null;
    }

    private void ApplyOverrides()
    {
        if (_light2D == null) return;

        // Cache current values first
        _baseOuterRadius = _light2D.pointLightOuterRadius;
        _baseIntensity = _light2D.intensity;
        _baseColor = _light2D.color;

        // Apply overrides (these become the flicker base values)
        if (overrideOuterRadius)
        {
            _light2D.pointLightOuterRadius = outerRadius;
            _baseOuterRadius = outerRadius;
        }

        if (overrideIntensity)
        {
            _light2D.intensity = intensity;
            _baseIntensity = intensity;
        }

        if (overrideColor)
        {
            _light2D.color = lightColor;
            _baseColor = lightColor;
        }
    }

    private void ApplyFlickerState()
    {
        if (_light2D == null) return;

        if (!shouldFlicker)
        {
            StopFlicker();
            // Restore to base in case flicker previously modified it.
            _light2D.pointLightOuterRadius = _baseOuterRadius;
            _light2D.intensity = _baseIntensity;
            _light2D.color = _baseColor;
            return;
        }

        StartFlicker();
    }

    private void StartFlicker()
    {
        if (_light2D == null) return;

        StopFlicker();

        // Use a recursive tween “stepper” so we can randomize each step duration/target slightly.
        _flickerTween = DOVirtual.DelayedCall(0f, FlickerStep)
            .SetTarget(this)
            .SetUpdate(true); // ignore timescale if you want consistent flicker even when paused
    }

    private void FlickerStep()
    {
        if (_light2D == null || !shouldFlicker) return;

        float radiusTarget = _baseOuterRadius * (1f + Random.Range(-radiusFlickerAmount, radiusFlickerAmount));
        float intensityTarget = _baseIntensity * (1f + Random.Range(-intensityFlickerAmount, intensityFlickerAmount));

        float dur = Random.Range(flickerStepDurationRange.x, flickerStepDurationRange.y);

        // Tween radius + intensity together.
        Tween t1 = DOTween.To(
            () => _light2D.pointLightOuterRadius,
            v => _light2D.pointLightOuterRadius = v,
            radiusTarget,
            dur
        ).SetEase(Ease.InOutSine).SetUpdate(true);

        Tween t2 = DOTween.To(
            () => _light2D.intensity,
            v => _light2D.intensity = v,
            intensityTarget,
            dur
        ).SetEase(Ease.InOutSine).SetUpdate(true);

        if (flickerAffectsColor)
        {
            Color.RGBToHSV(_baseColor, out float h, out float s, out float v);
            float vTarget = Mathf.Clamp01(v + Random.Range(-colorValueFlickerAmount, colorValueFlickerAmount));
            Color colorTarget = Color.HSVToRGB(h, s, vTarget);

            Tween t3 = DOTween.To(
                () => _light2D.color,
                c => _light2D.color = c,
                colorTarget,
                dur
            ).SetEase(Ease.InOutSine).SetUpdate(true);

            _flickerTween = DOTween.Sequence()
                .Append(t1)
                .Join(t2)
                .Join(t3)
                .OnComplete(() =>
                {
                    // Continue flicker
                    if (this != null && enabled) FlickerStep();
                })
                .SetTarget(this);
        }
        else
        {
            _flickerTween = DOTween.Sequence()
                .Append(t1)
                .Join(t2)
                .OnComplete(() =>
                {
                    // Continue flicker
                    if (this != null && enabled) FlickerStep();
                })
                .SetTarget(this);
        }
    }

    private void StopFlicker()
    {
        if (_flickerTween != null && _flickerTween.IsActive())
        {
            _flickerTween.Kill();
        }
        _flickerTween = null;
    }

    private bool IsVisibilityAllowed()
    {
        return requiredActiveObject == null || requiredActiveObject.activeInHierarchy;
    }

    // Public API (optional, useful if you have a global day/night manager)
    public void SetNight(bool isNight)
    {
        night = isNight;
        ApplyNightState();
    }
}
