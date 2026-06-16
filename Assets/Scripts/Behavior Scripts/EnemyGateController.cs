using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public sealed class EnemyGateController : MonoBehaviour
{
    [Header("Persistence")]
    [SerializeField] private PersistentId persistentId;
    [SerializeField] private string flagId;
    [SerializeField] private bool saveActiveSlotOnOpen = true;
    [SerializeField] private GameEvent savedEvent;

    [Header("Enemies")]
    [SerializeField] private HardwormDefeatableEnemy[] enemies;
    [SerializeField] private bool destroyEnemiesWhenLoadedOpen = true;

    [Header("Gate")]
    [SerializeField] private Collider2D[] gateColliders;
    [SerializeField] private SpriteRenderer gateSpriteRenderer;
    [SerializeField] private Sprite openedSprite;
    [SerializeField] private GameObject openedVersionPrefab;
    [SerializeField] private bool moveToInteractedLayerOnOpen = true;
    [SerializeField] private string interactedLayerName = "Interacted";

    private bool isOpen;
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
            OpenGate(persist: false, destroyCachedEnemies: destroyEnemiesWhenLoadedOpen);
            return;
        }

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
        if (isOpen)
            return;

        isOpen = true;
        UnregisterEnemyListeners();

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

        if (destroyCachedEnemies)
            DestroyCachedEnemiesForLoadedOpen();

        if (persist)
            PersistOpenState();
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
