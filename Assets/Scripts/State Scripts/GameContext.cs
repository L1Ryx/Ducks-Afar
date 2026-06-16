using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameContext : MonoBehaviour 
{
    [Header("State References")]
    public PlayerData PlayerData { get; private set; }

    public AudioStateModel Audio { get; private set; } 
    
    public InventoryModel Inventory { get; private set; }
    public InventorySelectionModel InventorySelection { get; private set; }
    public LevelStateModel LevelState { get; private set; }
    public LevelCheckpointModel LevelCheckpoints { get; private set; }
    public InteractionLockModel InteractionLock { get; private set; }
    public ItemDatabase ItemDb => itemDatabase;
    public SaveStateModel SaveState { get; private set; }
    public SaveSystem Saves { get; private set; }
    public SceneLoadSystem SceneLoader { get; private set; }
    public SceneFlowSystem SceneFlow { get; private set; }
    public SettingsSystem Settings { get; private set; }
    public PauseStateModel Pause { get; private set; }
    public PlayerDeathController PlayerDeath { get; private set; }
    public HardwormPickupSfx HardwormPickupSfx { get; private set; }
    public DialogueRunner Dialogue { get; private set; }
    
    [Header("View Refs")]
    [SerializeField] private DialogueRunner dialogueRunner;

    [Header("Databases")] [SerializeField] private ItemDatabase itemDatabase;

    [Header("Scene Flow")]
    [SerializeField] private SceneFlowPresetSO sceneFlowPreset;

    [SerializeField] private string bootstrapSceneName = "Bootstrap";

    private void Awake()
    {
        if (Game.IsReady && Game.Ctx != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        
        InitializeRuntimeState();
        EnsurePauseComponents();
        EnsureHoldToResetController();
        EnsurePlayerDeathController();
        EnsureMenuButtonAudioSystem();
        EnsureDevComponents();

        Game.SetContext(this);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }
    
    private void Update()
    {
        if (Pause?.IsPaused == true)
            return;

        SaveState.AddPlayTime(Time.deltaTime);
    }

    private void InitializeRuntimeState()
    {
        PlayerData = new PlayerData();
        Inventory = new InventoryModel(new InventoryData());
        InventorySelection = new InventorySelectionModel(Inventory);
        LevelState = new LevelStateModel();
        LevelCheckpoints = new LevelCheckpointModel();
        InteractionLock = new InteractionLockModel();
        Audio = new AudioStateModel();
        SaveState = new SaveStateModel();
        SceneLoader = new SceneLoadSystem(this);
        SceneFlow = new SceneFlowSystem(this, sceneFlowPreset);
        Saves = new SaveSystem(this);
        Settings = new SettingsSystem(this);
        Pause = new PauseStateModel();
        
        Audio.Initialize(gameObject); // Global emitter is on game context!
        Settings.LoadOrCreate();
        Settings.ApplyAll();
        SceneLoader.PrewarmLoadingScreen();

        HardwormPickupSfx = GetComponentInChildren<HardwormPickupSfx>();
        if (HardwormPickupSfx == null)
        {
            Debug.LogWarning("HardwormPickupSfx is null");
        }
        
        Dialogue = dialogueRunner;
        if (Dialogue == null)
            Debug.LogError("GameContext: DialogueRunner reference is missing. Assign it in the inspector.");
    }

    private void EnsurePauseComponents()
    {
        if (GetComponent<PauseSceneWatcher>() == null)
            gameObject.AddComponent<PauseSceneWatcher>();

        if (GetComponent<PauseTimeScaleDriver>() == null)
            gameObject.AddComponent<PauseTimeScaleDriver>();

        if (GetComponent<PauseInputController>() == null)
            gameObject.AddComponent<PauseInputController>();
    }

    private void EnsureHoldToResetController()
    {
        if (GetComponent<HoldToResetController>() == null)
            gameObject.AddComponent<HoldToResetController>();
    }

    private void EnsurePlayerDeathController()
    {
        PlayerDeath = GetComponent<PlayerDeathController>();
        if (PlayerDeath == null)
            PlayerDeath = gameObject.AddComponent<PlayerDeathController>();
    }

    private void EnsureMenuButtonAudioSystem()
    {
        if (GetComponent<MenuButtonAudioSystem>() == null)
            gameObject.AddComponent<MenuButtonAudioSystem>();
    }

    private void EnsureDevComponents()
    {
#if UNITY_EDITOR
        if (GetComponent<DevSceneCommands>() == null)
            gameObject.AddComponent<DevSceneCommands>();

        if (GetComponent<SaveDebugCommands>() == null)
            gameObject.AddComponent<SaveDebugCommands>();

        if (GetComponent<EditorDebugConsoleHotkey>() == null)
            gameObject.AddComponent<EditorDebugConsoleHotkey>();
#endif
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (SaveState == null || scene.name == bootstrapSceneName)
            return;

        SaveState.CurrentSceneName = scene.name;

        if (SaveState.HasSavedPlayerPosition && SaveState.SavedPlayerPositionSceneName == scene.name)
            StartCoroutine(RestoreSavedPlayerPositionWhenReady(scene.name));
    }

    private IEnumerator RestoreSavedPlayerPositionWhenReady(string sceneName)
    {
        const int maxWaitFrames = 60;

        for (int i = 0; i < maxWaitFrames; i++)
        {
            if (SaveState == null
                || !SaveState.HasSavedPlayerPosition
                || SaveState.SavedPlayerPositionSceneName != sceneName)
            {
                yield break;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = SaveState.SavedPlayerPosition;

                if (player.TryGetComponent<Rigidbody2D>(out var body))
                    body.linearVelocity = Vector2.zero;

                StartCoroutine(SyncGameplayCamerasToPlayerRoomForFrames(player));
                yield break;
            }

            yield return null;
        }
    }

    public static IEnumerator SyncGameplayCamerasToPlayerRoomForFrames(GameObject player, int frameCount = 8)
    {
        frameCount = Mathf.Max(1, frameCount);
        for (int i = 0; i < frameCount; i++)
        {
            SyncGameplayCamerasToPlayerRoom(player);
            yield return null;
        }
    }

    public static void SyncGameplayCamerasToPlayerRoom(GameObject player)
    {
        if (player == null)
            return;

        GameplayCameraAnchorController[] controllers = Object.FindObjectsByType<GameplayCameraAnchorController>(
            FindObjectsInactive.Exclude);

        foreach (GameplayCameraAnchorController controller in controllers)
        {
            if (controller != null && controller.gameObject.scene == player.scene)
                controller.FocusPlayerRoom(immediate: true);
        }
    }
}
