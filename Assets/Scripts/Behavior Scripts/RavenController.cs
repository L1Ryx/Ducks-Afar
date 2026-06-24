using System.Collections;
using UnityEngine;

public sealed class RavenController : MonoBehaviour
{
    [Header("Triggering")]
    [SerializeField] private RavenFlyby ravenPrefab;
    [SerializeField] private bool spawnAutomatically = true;
    [SerializeField, Min(0f)] private float minDelay = 30f;
    [SerializeField, Min(0f)] private float maxDelay = 60f;
    [SerializeField] private GameEvent triggerEvent;

    [Header("Flight")]
    [SerializeField, Min(0f)] private float speed = 16f;
    [SerializeField] private Vector2 angleRange = new Vector2(-60f, 60f);
    [SerializeField, Min(0f)] private float offscreenPadding = 2f;
    [SerializeField] private Vector2 scaleRange = new Vector2(1f, 1.15f);
    [SerializeField, Min(0f)] private float bobAmplitude = 0.08f;
    [SerializeField, Min(0f)] private float bobFrequency = 2.1f;

    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    [Header("Audio")]
    [SerializeField] private AudioCue flybyCue;

    private Coroutine automaticRoutine;

    private void OnEnable()
    {
        triggerEvent?.RegisterRuntimeListener(TriggerFlyby);

        if (spawnAutomatically)
            automaticRoutine = StartCoroutine(AutomaticFlybys());
    }

    private void OnDisable()
    {
        triggerEvent?.UnregisterRuntimeListener(TriggerFlyby);

        if (automaticRoutine != null)
        {
            StopCoroutine(automaticRoutine);
            automaticRoutine = null;
        }
    }

    [ContextMenu("Trigger Flyby")]
    public void TriggerFlyby()
    {
        Camera cameraToUse = ResolveCamera();
        if (ravenPrefab == null)
        {
            Debug.LogWarning($"{nameof(RavenController)} could not spawn a raven because no flyby prefab is assigned.", this);
            return;
        }

        if (cameraToUse == null)
        {
            Debug.LogWarning($"{nameof(RavenController)} could not spawn a raven because no camera was available.", this);
            return;
        }

        Vector2 direction = BuildDirection();
        Vector3 spawnPosition = BuildSpawnPosition(cameraToUse, direction);
        RavenFlyby flyby = Instantiate(ravenPrefab, spawnPosition, Quaternion.identity);
        flyby.name = "Raven Flyby";
        ProjectAudio.PlayGlobal(flybyCue);

        float scale = Random.Range(
            Mathf.Min(scaleRange.x, scaleRange.y),
            Mathf.Max(scaleRange.x, scaleRange.y));
        flyby.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);

        flyby.Initialize(
            direction,
            speed,
            offscreenPadding,
            bobAmplitude,
            bobFrequency,
            cameraToUse);
    }

    private IEnumerator AutomaticFlybys()
    {
        while (enabled)
        {
            float delay = Random.Range(Mathf.Min(minDelay, maxDelay), Mathf.Max(minDelay, maxDelay));
            yield return new WaitForSeconds(delay);
            TriggerFlyby();
        }
    }

    private Camera ResolveCamera()
    {
        if (targetCamera != null)
            return targetCamera;

        targetCamera = Camera.main;
        return targetCamera;
    }

    private Vector2 BuildDirection()
    {
        float angle = Random.Range(Mathf.Min(angleRange.x, angleRange.y), Mathf.Max(angleRange.x, angleRange.y));
        return Quaternion.Euler(0f, 0f, angle) * Vector2.left;
    }

    private Vector3 BuildSpawnPosition(Camera cameraToUse, Vector2 direction)
    {
        float depth = Mathf.Abs(cameraToUse.transform.position.z - transform.position.z);
        Vector3 bottomLeft = cameraToUse.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
        Vector3 topRight = cameraToUse.ViewportToWorldPoint(new Vector3(1f, 1f, depth));

        float x = topRight.x + offscreenPadding;
        float y = Random.Range(bottomLeft.y + offscreenPadding, topRight.y - offscreenPadding);

        if (direction.y > 0.2f)
            y = Random.Range(bottomLeft.y - offscreenPadding, topRight.y);
        else if (direction.y < -0.2f)
            y = Random.Range(bottomLeft.y, topRight.y + offscreenPadding);

        return new Vector3(x, y, transform.position.z);
    }
}
