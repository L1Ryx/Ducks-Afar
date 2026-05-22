using UnityEngine;

public class GameContext : MonoBehaviour 
{
    [Header("State References")]
    public PlayerData PlayerData { get; private set; }

    public AudioStateModel Audio { get; private set; } 
    
    public InventoryModel Inventory { get; private set; }
    public InventorySelectionModel InventorySelection { get; private set; }
    public LevelStateModel LevelState { get; private set; }
    public InteractionLockModel InteractionLock { get; private set; }
    public ItemDatabase ItemDb => itemDatabase;
    public SaveStateModel SaveState { get; private set; }
    public SaveSystem Saves { get; private set; }
    public SettingsSystem Settings { get; private set; }
    public PauseStateModel Pause { get; private set; }
    public HardwormPickupSfx HardwormPickupSfx { get; private set; }
    public DialogueRunner Dialogue { get; private set; }
    
    [Header("View Refs")]
    [SerializeField] private DialogueRunner dialogueRunner;

    [Header("Databases")] [SerializeField] private ItemDatabase itemDatabase;

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

        Game.SetContext(this);
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
        InteractionLock = new InteractionLockModel();
        Audio = new AudioStateModel();
        SaveState = new SaveStateModel();
        Saves = new SaveSystem(this);
        Settings = new SettingsSystem(this);
        Pause = new PauseStateModel();
        
        Audio.Initialize(gameObject); // Global emitter is on game context!
        Settings.LoadOrCreate();
        Settings.ApplyAll();

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
}
