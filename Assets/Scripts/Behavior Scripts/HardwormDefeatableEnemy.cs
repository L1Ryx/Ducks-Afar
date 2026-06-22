using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class HardwormDefeatableEnemy : MonoBehaviour
{
    [Header("Requirement")]
    [SerializeField, Min(1)] private int requiredHardworms = 3;
    [SerializeField] private bool allowLargerHardwormPacks;

    [Header("Player Contact")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask playerLayerMask;
    [SerializeField] private bool killPlayerOnFailedContact = true;
    [SerializeField, Min(0f)] private float contactProbeDistance = 0.08f;

    [Header("Refs")]
    [SerializeField] private CameraRoomAStarChaser2D chaser;
    [SerializeField] private TMP_Text requirementText;
    [SerializeField] private Animator animator;

    [Header("Defeat")]
    [SerializeField] private string deathAnimationStateName = "Die";
    [SerializeField, Min(0f)] private float deathAnimationDuration = 0.7f;
    [SerializeField, Min(0f)] private float fadeDuration = 0.35f;
    [SerializeField] private bool destroyAfterFade = true;

    [Header("Drop")]
    [SerializeField] private GameObject droppedItemPrefab;
    [SerializeField] private Vector3 droppedItemOffset;

    [Header("Events")]
    [SerializeField] private UnityEvent onDefeated;
    [SerializeField] private UnityEvent onDefeatFailed;

    private SpriteRenderer[] spriteRenderers;
    private TMP_Text[] textRenderers;
    private Graphic[] uiGraphics;
    private Collider2D[] colliders;
    private Rigidbody2D body;
    private readonly Collider2D[] playerContactResults = new Collider2D[8];
    private bool isDefeated;
    private bool isDefeating;

    public event Action<HardwormDefeatableEnemy> Defeated;

    public bool IsDefeated => isDefeated;

    private void Awake()
    {
        EnsureRefs();
        CacheRenderersAndColliders();
        UpdateRequirementText();
    }

    private void OnEnable()
    {
        UpdateFleeState();
    }

    private void Update()
    {
        UpdateFleeState();
    }

    private void FixedUpdate()
    {
        ProbeNearbyPlayerContact();
    }

    private void OnValidate()
    {
        if (requiredHardworms < 1)
            requiredHardworms = 1;

        if (chaser == null)
            chaser = GetComponent<CameraRoomAStarChaser2D>();

        if (requirementText == null)
            requirementText = GetComponentInChildren<TMP_Text>(true);

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (body == null)
            body = GetComponent<Rigidbody2D>();

        UpdateRequirementText();
    }

    public bool TryDefeatFromPlayer(GameObject player, bool raiseFailureEvent = false)
    {
        if (isDefeated || isDefeating)
            return false;

        if (!IsPlayerObject(player))
            return false;

        if (!TryGetHeldMatchingHardworm(out string itemId))
        {
            if (raiseFailureEvent)
                onDefeatFailed?.Invoke();
            return false;
        }

        if (!Game.IsReady || Game.Ctx?.Inventory == null || !Game.Ctx.Inventory.TryRemove(itemId, 1))
        {
            if (raiseFailureEvent)
                onDefeatFailed?.Invoke();
            return false;
        }

        Defeat();
        return true;
    }

    public void Defeat()
    {
        if (isDefeated || isDefeating)
            return;

        StartCoroutine(DefeatRoutine());
    }

    public void DestroyImmediatelyForPersistence()
    {
        if (isDefeated)
            return;

        isDefeated = true;
        isDefeating = false;
        chaser?.SetFleeFromTarget(false);
        DisableGameplay(disableColliders: true);
        Defeated?.Invoke(this);
        Destroy(gameObject);
    }

    private IEnumerator DefeatRoutine()
    {
        isDefeating = true;
        chaser?.SetFleeFromTarget(false);
        DisableGameplay(disableColliders: false);

        yield return PlayDeathAnimation();

        float elapsed = 0f;
        while (fadeDuration > 0f && elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Clamp01(1f - elapsed / fadeDuration));
            yield return null;
        }

        SetAlpha(0f);
        isDefeated = true;
        isDefeating = false;

        onDefeated?.Invoke();
        Defeated?.Invoke(this);
        DropItem();

        if (destroyAfterFade)
            Destroy(gameObject);
    }

    private void DropItem()
    {
        if (droppedItemPrefab == null)
            return;

        Instantiate(droppedItemPrefab, transform.position + droppedItemOffset, Quaternion.identity, transform.parent);
    }

    private void DisableGameplay(bool disableColliders)
    {
        if (chaser != null)
            chaser.FreezeMovement();

        MakeBodyImmovable();

        if (!disableColliders)
            return;

        if (colliders == null)
            CacheRenderersAndColliders();

        foreach (Collider2D col in colliders)
        {
            if (col != null)
                col.enabled = false;
        }
    }

    private void MakeBodyImmovable()
    {
        if (body == null)
            body = GetComponent<Rigidbody2D>();

        if (body == null)
            return;

        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
    }

    private IEnumerator PlayDeathAnimation()
    {
        if (animator == null || string.IsNullOrWhiteSpace(deathAnimationStateName))
            yield break;

        animator.enabled = true;
        animator.Play(deathAnimationStateName, 0, 0f);
        animator.Update(0f);

        if (deathAnimationDuration > 0f)
            yield return new WaitForSeconds(deathAnimationDuration);
    }

    private void UpdateFleeState()
    {
        if (chaser == null || isDefeated || isDefeating)
            return;

        chaser.SetFleeFromTarget(TryGetHeldMatchingHardworm(out _));
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDefeatFromPlayerContact(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDefeatFromPlayerContact(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDefeatFromPlayerContact(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDefeatFromPlayerContact(other);
    }

    private bool TryDefeatFromPlayerContact(Collider2D other)
    {
        GameObject player = GetPlayerObject(other);
        if (player == null)
            return false;

        if (TryDefeatFromPlayer(player))
            return true;

        return TryKillPlayerFromContact(player);
    }

    private void ProbeNearbyPlayerContact()
    {
        if (isDefeated || isDefeating || colliders == null)
            return;

        ContactFilter2D filter = new ContactFilter2D
        {
            useLayerMask = playerLayerMask.value != 0,
            layerMask = playerLayerMask,
            useTriggers = true,
        };

        foreach (Collider2D ownCollider in colliders)
        {
            if (ownCollider == null || !ownCollider.enabled || ownCollider.isTrigger)
                continue;

            Bounds bounds = ownCollider.bounds;
            Vector2 center = bounds.center;
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.y) + contactProbeDistance;
            int count = Physics2D.OverlapCircle(center, radius, filter, playerContactResults);

            for (int i = 0; i < count; i++)
            {
                Collider2D playerCollider = playerContactResults[i];
                if (playerCollider == null)
                    continue;

                GameObject player = GetPlayerObject(playerCollider);
                if (player == null)
                    continue;

                ColliderDistance2D distance = ownCollider.Distance(playerCollider);
                if (!distance.isOverlapped && distance.distance > contactProbeDistance)
                    continue;

                TryDefeatFromPlayerContact(playerCollider);
                return;
            }
        }
    }

    private bool TryKillPlayerFromContact(GameObject player)
    {
        if (!killPlayerOnFailedContact || isDefeated || isDefeating)
            return false;

        if (!Game.IsReady || Game.Ctx?.PlayerDeath == null)
            return false;

        return Game.Ctx.PlayerDeath.KillPlayer(player, gameObject);
    }

    private bool IsPlayerObject(GameObject candidate)
    {
        if (candidate == null)
            return false;

        if (playerLayerMask.value != 0 && ((1 << candidate.layer) & playerLayerMask.value) == 0)
            return false;

        return string.IsNullOrWhiteSpace(playerTag) || candidate.CompareTag(playerTag);
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

    private bool TryGetHeldMatchingHardworm(out string itemId)
    {
        itemId = null;

        if (!Game.IsReady || Game.Ctx?.Inventory == null || Game.Ctx.InventorySelection == null || Game.Ctx.ItemDb == null)
            return false;

        InventoryEntry selected = Game.Ctx.InventorySelection.GetSelectedEntry();
        if (string.IsNullOrWhiteSpace(selected.itemId) || selected.count <= 0)
            return false;

        HardwormPackDefinition hardworm = Game.Ctx.ItemDb.Get<HardwormPackDefinition>(selected.itemId);
        if (hardworm == null)
            return false;

        bool matches = allowLargerHardwormPacks
            ? hardworm.packSize >= requiredHardworms
            : hardworm.packSize == requiredHardworms;

        if (!matches || Game.Ctx.Inventory.GetCount(selected.itemId) <= 0)
            return false;

        itemId = selected.itemId;
        return true;
    }

    private void EnsureRefs()
    {
        if (chaser == null)
            chaser = GetComponent<CameraRoomAStarChaser2D>();

        if (requirementText == null)
            requirementText = GetComponentInChildren<TMP_Text>(true);

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (body == null)
            body = GetComponent<Rigidbody2D>();
    }

    private void CacheRenderersAndColliders()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        textRenderers = GetComponentsInChildren<TMP_Text>(true);
        uiGraphics = GetComponentsInChildren<Graphic>(true);
        colliders = GetComponentsInChildren<Collider2D>(true);
    }

    private void UpdateRequirementText()
    {
        if (requirementText != null)
            requirementText.text = requiredHardworms.ToString();
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderers == null || textRenderers == null || uiGraphics == null)
            CacheRenderersAndColliders();

        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            if (renderer == null)
                continue;

            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }

        foreach (TMP_Text text in textRenderers)
        {
            if (text == null)
                continue;

            Color color = text.color;
            color.a = alpha;
            text.color = color;
        }

        foreach (Graphic graphic in uiGraphics)
        {
            if (graphic == null)
                continue;

            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }
}
