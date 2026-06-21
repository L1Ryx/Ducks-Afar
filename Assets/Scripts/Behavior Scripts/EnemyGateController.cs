using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public sealed class EnemyGateController : MonoBehaviour
{
    [System.Serializable]
    private sealed class GateHalf
    {
        public Transform root;
        public SpriteRenderer spriteRenderer;
        public BoxCollider2D collider;
        public Sprite closedSprite;
        public Sprite transitionSprite;
        public Sprite openSprite;
        public Vector2 closedColliderOffset;
        public Vector2 closedColliderSize = Vector2.one;
        public Vector2 openColliderOffset;
        public Vector2 openColliderSize = Vector2.one;

        public bool IsConfigured => spriteRenderer != null && collider != null;
    }

    [Header("Persistence")]
    [SerializeField] private PersistentId persistentId;
    [SerializeField] private string flagId;
    [SerializeField] private bool saveActiveSlotOnOpen = true;
    [SerializeField] private GameEvent savedEvent;

    [Header("Enemies")]
    [SerializeField] private HardwormDefeatableEnemy[] enemies;
    [SerializeField] private bool destroyEnemiesWhenLoadedOpen = true;

    [Header("Gate")]
    [SerializeField] private GateHalf leftHalf = new GateHalf();
    [SerializeField] private GateHalf rightHalf = new GateHalf();
    [SerializeField, Min(0f)] private float transitionDuration = 0.25f;

    [Header("Legacy Gate")]
    [SerializeField] private Collider2D[] gateColliders;
    [SerializeField] private SpriteRenderer gateSpriteRenderer;
    [SerializeField] private Sprite openedSprite;
    [SerializeField] private GameObject openedVersionPrefab;
    [SerializeField] private bool moveToInteractedLayerOnOpen = true;
    [SerializeField] private string interactedLayerName = "Interacted";

    private bool isOpen;
    private bool isOpening;
    private Coroutine openRoutine;
    private GameObject openedVersionInstance;

    private void Awake()
    {
        EnsureRefs();
    }

    private void OnEnable()
    {
        RegisterEnemyListeners();
    }

    private void Start()
    {
        if (IsFlagSet())
        {
            ApplyOpenStateImmediate(destroyCachedEnemies: destroyEnemiesWhenLoadedOpen);
            return;
        }

        ApplyClosedState();
        CheckEnemies();
    }

    private void OnDisable()
    {
        UnregisterEnemyListeners();
    }

    public void CheckEnemies()
    {
        if (isOpen)
            return;

        if (enemies == null || enemies.Length == 0)
            return;

        for (int i = 0; i < enemies.Length; i++)
        {
            HardwormDefeatableEnemy enemy = enemies[i];
            if (enemy != null && !enemy.IsDefeated)
                return;
        }

        OpenGate(persist: true, destroyCachedEnemies: false);
    }

    public void OpenGate()
    {
        OpenGate(persist: true, destroyCachedEnemies: false);
    }

    private void HandleEnemyDefeated(HardwormDefeatableEnemy enemy)
    {
        CheckEnemies();
    }

    private void OpenGate(bool persist, bool destroyCachedEnemies)
    {
        if (isOpen || isOpening)
            return;

        isOpening = true;
        UnregisterEnemyListeners();

        if (openRoutine != null)
            StopCoroutine(openRoutine);

        openRoutine = StartCoroutine(OpenGateRoutine(persist, destroyCachedEnemies));
    }

    private IEnumerator OpenGateRoutine(bool persist, bool destroyCachedEnemies)
    {
        ApplyTransitionState();

        if (transitionDuration > 0f && HasConfiguredHalves())
            yield return new WaitForSeconds(transitionDuration);

        ApplyOpenVisualState();

        isOpen = true;
        isOpening = false;
        openRoutine = null;

        if (destroyCachedEnemies)
            DestroyCachedEnemiesForLoadedOpen();

        if (persist)
            PersistOpenState();
    }

    private void ApplyOpenStateImmediate(bool destroyCachedEnemies)
    {
        if (isOpen)
            return;

        isOpen = true;
        isOpening = false;
        UnregisterEnemyListeners();

        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
            openRoutine = null;
        }

        ApplyOpenVisualState();

        if (destroyCachedEnemies)
            DestroyCachedEnemiesForLoadedOpen();
    }

    private void ApplyClosedState()
    {
        if (HasConfiguredHalves())
        {
            ApplyHalfState(leftHalf, leftHalf.closedSprite, leftHalf.closedColliderOffset, leftHalf.closedColliderSize);
            ApplyHalfState(rightHalf, rightHalf.closedSprite, rightHalf.closedColliderOffset, rightHalf.closedColliderSize);
            SetLegacyGateVisible(false);
            SetLegacyCollidersEnabled(false);
        }
    }

    private void ApplyTransitionState()
    {
        if (!HasConfiguredHalves())
            return;

        ApplyHalfSprite(leftHalf, leftHalf.transitionSprite);
        ApplyHalfSprite(rightHalf, rightHalf.transitionSprite);
    }

    private void ApplyOpenVisualState()
    {
        if (HasConfiguredHalves())
        {
            ApplyHalfState(leftHalf, leftHalf.openSprite, leftHalf.openColliderOffset, leftHalf.openColliderSize);
            ApplyHalfState(rightHalf, rightHalf.openSprite, rightHalf.openColliderOffset, rightHalf.openColliderSize);
            SetLegacyGateVisible(false);
            SetLegacyCollidersEnabled(false);
            return;
        }

        if (gateColliders == null || gateColliders.Length == 0)
            gateColliders = GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D col in gateColliders)
        {
            if (col != null)
                col.enabled = false;
        }

        if (gateSpriteRenderer != null && openedSprite != null)
            gateSpriteRenderer.sprite = openedSprite;

        if (openedVersionPrefab != null && openedVersionInstance == null)
            openedVersionInstance = Instantiate(openedVersionPrefab, transform.position, Quaternion.identity, transform.parent);

        if (moveToInteractedLayerOnOpen)
            MoveHierarchyToLayer(interactedLayerName);
    }

    private void PersistOpenState()
    {
        if (!TryResolveFlagId(out string resolvedFlagId))
            return;

        if (!Game.IsReady || Game.Ctx?.SaveState == null)
        {
            Debug.LogWarning($"{name}: cannot set enemy gate save flag '{resolvedFlagId}' because GameContext is not ready.", this);
            return;
        }

        bool changed = Game.Ctx.SaveState.SetWorldState(resolvedFlagId);
        if (!changed)
            return;

        if (saveActiveSlotOnOpen && Game.Ctx.Saves != null && Game.Ctx.Saves.SaveCurrentGameToActiveSlot())
            savedEvent?.Raise();
    }

    private bool HasConfiguredHalves()
    {
        return leftHalf != null && rightHalf != null && leftHalf.IsConfigured && rightHalf.IsConfigured;
    }

    private void ApplyHalfState(GateHalf half, Sprite sprite, Vector2 colliderOffset, Vector2 colliderSize)
    {
        ApplyHalfSprite(half, sprite);

        if (half.collider == null)
            return;

        half.collider.enabled = true;
        half.collider.offset = colliderOffset;
        half.collider.size = colliderSize;
    }

    private void ApplyHalfSprite(GateHalf half, Sprite sprite)
    {
        if (half == null || half.spriteRenderer == null || sprite == null)
            return;

        half.spriteRenderer.sprite = sprite;
    }

    private void SetLegacyGateVisible(bool visible)
    {
        if (gateSpriteRenderer != null)
            gateSpriteRenderer.enabled = visible;
    }

    private void SetLegacyCollidersEnabled(bool enabled)
    {
        if (gateColliders == null)
            return;

        for (int i = 0; i < gateColliders.Length; i++)
        {
            Collider2D col = gateColliders[i];
            if (col == null)
                continue;

            if ((leftHalf != null && col == leftHalf.collider) || (rightHalf != null && col == rightHalf.collider))
                continue;

            col.enabled = enabled;
        }
    }

    private bool IsFlagSet()
    {
        if (!TryResolveFlagId(out string resolvedFlagId))
            return false;

        return Game.IsReady &&
               Game.Ctx?.SaveState != null &&
               Game.Ctx.SaveState.HasWorldState(resolvedFlagId);
    }

    private void DestroyCachedEnemiesForLoadedOpen()
    {
        if (enemies == null)
            return;

        for (int i = 0; i < enemies.Length; i++)
        {
            HardwormDefeatableEnemy enemy = enemies[i];
            if (enemy != null)
                enemy.DestroyImmediatelyForPersistence();
        }
    }

    private void RegisterEnemyListeners()
    {
        if (enemies == null)
            return;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
                enemies[i].Defeated += HandleEnemyDefeated;
        }
    }

    private void UnregisterEnemyListeners()
    {
        if (enemies == null)
            return;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
                enemies[i].Defeated -= HandleEnemyDefeated;
        }
    }

    private void EnsureRefs()
    {
        if (persistentId == null)
            persistentId = GetComponent<PersistentId>();

        if (gateSpriteRenderer == null)
            gateSpriteRenderer = GetComponent<SpriteRenderer>();

        if (gateColliders == null || gateColliders.Length == 0)
            gateColliders = GetComponentsInChildren<Collider2D>(true);
    }

    private void MoveHierarchyToLayer(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
            return;

        SetLayerRecursive(gameObject, layer);
    }

    private void SetLayerRecursive(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    private bool TryResolveFlagId(out string resolvedFlagId)
    {
        resolvedFlagId = string.IsNullOrWhiteSpace(flagId) ? string.Empty : flagId.Trim();

        if (resolvedFlagId.Length == 0 && persistentId != null)
            persistentId.TryGetId(out resolvedFlagId);

        if (resolvedFlagId.Length > 0)
            return true;

        Debug.LogWarning($"{name}: enemy gate save flag id is empty.", this);
        return false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureRefs();

        if (savedEvent == null)
            savedEvent = AssetDatabase.LoadAssetAtPath<GameEvent>("Assets/SOs/Events/OnGameSaved.asset");
    }
#endif
}
