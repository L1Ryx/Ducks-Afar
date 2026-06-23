using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class FreakyTofuController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask playerLayerMask;
    [SerializeField] private bool autoFindPlayer = true;
    [SerializeField] private Transform player;

    [Header("Detection")]
    [SerializeField, Min(0f)] private float detectionRange = 7f;
    [SerializeField] private bool drawDetectionRange = true;
    [SerializeField] private Color detectionRangeColor = new Color(1f, 0.1f, 0.1f, 0.55f);

    [Header("Jumpscare Movement")]
    [SerializeField, Min(0f)] private float chaseSpeed = 18f;
    [SerializeField, Min(0f)] private float acceleration = 90f;
    [SerializeField, Min(0f)] private float jitterAmplitude = 0.85f;
    [SerializeField, Min(0.01f)] private float jitterFrequency = 18f;
    [SerializeField, Min(0f)] private float speedPulseAmount = 0.35f;
    [SerializeField, Min(0.01f)] private float speedPulseFrequency = 11f;

    [Header("Number UI")]
    [SerializeField] private CanvasGroup numberCanvasGroup;
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private int displayNumber = 3;

    [Header("Scene Advance")]
    [SerializeField] private FadeOutOnLevelEnd curtainFade;
    [SerializeField] private string fallbackNextSceneName;
    [SerializeField] private bool lockInteractionsOnTouch = true;

    private Rigidbody2D body;
    private Vector2 velocity;
    private bool hasNoticedPlayer;
    private bool hasTriggeredSceneAdvance;
    private bool ownsInteractionLock;
    private float jitterSeed;

    private void Awake()
    {
        ResolveRefs();
        ConfigureBody();
        RefreshNumber();
        SetNumberVisible(false);
        jitterSeed = Random.value * 1000f;
    }

    private void OnEnable()
    {
        ResolvePlayer();
    }

    private void OnDisable()
    {
        ReleaseInteractionLock();
    }

    private void FixedUpdate()
    {
        if (hasTriggeredSceneAdvance)
        {
            StopMoving();
            return;
        }

        ResolvePlayer();

        if (player == null)
        {
            StopMoving();
            return;
        }

        if (!hasNoticedPlayer && IsPlayerInDetectionRange())
            NoticePlayer();

        if (!hasNoticedPlayer)
        {
            StopMoving();
            return;
        }

        ChasePlayer();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTriggerSceneAdvance(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryTriggerSceneAdvance(other);
    }

    private void OnValidate()
    {
        if (detectionRange < 0f)
            detectionRange = 0f;

        if (jitterFrequency < 0.01f)
            jitterFrequency = 0.01f;

        if (speedPulseFrequency < 0.01f)
            speedPulseFrequency = 0.01f;

        ResolveRefs();
        RefreshNumber();
    }

    private void NoticePlayer()
    {
        hasNoticedPlayer = true;
        SetNumberVisible(true);
    }

    private void ChasePlayer()
    {
        Vector2 current = body != null ? body.position : (Vector2)transform.position;
        Vector2 toPlayer = (Vector2)player.position - current;
        Vector2 direction = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector2.up;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);

        float time = Time.time + jitterSeed;
        float jitter = Mathf.Sin(time * jitterFrequency) + 0.45f * Mathf.Sin(time * jitterFrequency * 2.37f);
        float pulse = 1f + speedPulseAmount * Mathf.Max(0f, Mathf.Sin(time * speedPulseFrequency));
        Vector2 desiredVelocity = (direction * chaseSpeed * pulse) + (perpendicular * jitter * jitterAmplitude);

        velocity = Vector2.MoveTowards(velocity, desiredVelocity, acceleration * Time.fixedDeltaTime);

        if (body != null)
            body.MovePosition(current + velocity * Time.fixedDeltaTime);
        else
            transform.position = current + velocity * Time.fixedDeltaTime;
    }

    private void StopMoving()
    {
        velocity = Vector2.zero;

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }
    }

    private bool IsPlayerInDetectionRange()
    {
        if (detectionRange <= 0f)
            return true;

        return ((Vector2)player.position - (Vector2)transform.position).sqrMagnitude <= detectionRange * detectionRange;
    }

    private void TryTriggerSceneAdvance(Collider2D other)
    {
        if (hasTriggeredSceneAdvance || !IsPlayerCollider(other))
            return;

        hasTriggeredSceneAdvance = true;
        hasNoticedPlayer = true;
        SetNumberVisible(true);
        StopMoving();
        StopPlayer(other);

        if (lockInteractionsOnTouch)
            AcquireInteractionLock();

        ResolveCurtain()?.CutToBlackImmediate();
        Game.Ctx?.Saves?.SaveCurrentGameToActiveSlot();

        if (Game.IsReady && Game.Ctx?.SceneFlow != null && Game.Ctx.SceneFlow.LoadNextScene(fallbackNextSceneName))
            return;

        Debug.LogWarning($"{nameof(FreakyTofuController)} on '{name}' could not advance scene flow.", this);
        ReleaseInteractionLock();
    }

    private void StopPlayer(Collider2D other)
    {
        if (other != null && other.attachedRigidbody != null)
            other.attachedRigidbody.linearVelocity = Vector2.zero;
    }

    private void AcquireInteractionLock()
    {
        if (ownsInteractionLock || !Game.IsReady || Game.Ctx?.InteractionLock == null)
            return;

        Game.Ctx.InteractionLock.Acquire();
        ownsInteractionLock = true;
    }

    private void ReleaseInteractionLock()
    {
        if (!ownsInteractionLock || !Game.IsReady || Game.Ctx?.InteractionLock == null)
        {
            ownsInteractionLock = false;
            return;
        }

        Game.Ctx.InteractionLock.Release();
        ownsInteractionLock = false;
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        GameObject candidate = GetPlayerObject(other);
        return candidate != null;
    }

    private GameObject GetPlayerObject(Collider2D other)
    {
        if (other == null)
            return null;

        if (other.attachedRigidbody != null && IsPlayerObject(other.attachedRigidbody.gameObject))
            return other.attachedRigidbody.gameObject;

        Transform current = other.transform;
        while (current != null)
        {
            if (IsPlayerObject(current.gameObject))
                return current.gameObject;

            current = current.parent;
        }

        return null;
    }

    private bool IsPlayerObject(GameObject candidate)
    {
        if (candidate == null)
            return false;

        if (playerLayerMask.value != 0 && ((1 << candidate.layer) & playerLayerMask.value) == 0)
            return false;

        return string.IsNullOrWhiteSpace(playerTag) || candidate.CompareTag(playerTag);
    }

    private void ResolveRefs()
    {
        if (body == null)
            body = GetComponent<Rigidbody2D>();

        if (numberCanvasGroup == null)
            numberCanvasGroup = GetComponentInChildren<CanvasGroup>(true);

        if (numberText == null)
            numberText = GetComponentInChildren<TMP_Text>(true);
    }

    private void ConfigureBody()
    {
        if (body == null)
            return;

        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void ResolvePlayer()
    {
        if (player != null || !autoFindPlayer)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
            player = playerObject.transform;
    }

    private FadeOutOnLevelEnd ResolveCurtain()
    {
        if (curtainFade == null)
            curtainFade = FindAnyObjectByType<FadeOutOnLevelEnd>(FindObjectsInactive.Include);

        return curtainFade;
    }

    private void RefreshNumber()
    {
        if (numberText != null)
            numberText.text = displayNumber.ToString();
    }

    private void SetNumberVisible(bool visible)
    {
        if (numberCanvasGroup == null)
            return;

        numberCanvasGroup.alpha = visible ? 1f : 0f;
        numberCanvasGroup.interactable = false;
        numberCanvasGroup.blocksRaycasts = false;
    }

    private void OnDrawGizmos()
    {
        if (!drawDetectionRange || detectionRange <= 0f)
            return;

        Gizmos.color = detectionRangeColor;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
