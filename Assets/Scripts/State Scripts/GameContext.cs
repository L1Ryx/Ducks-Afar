using UnityEngine;

public class GameContext : MonoBehaviour // // 👈 PERSISTENT/INITIALIZED ONCE
{
    [Header("State References")]
    public PlayerData PlayerData { get; private set; }

    public AudioStateModel Audio { get; private set; } // 👈 AUDIO MODEL IS HERE
    
    public InventoryModel Inventory { get; private set; }
    public InventorySelectionModel InventorySelection { get; private set; }
    public LevelStateModel LevelState { get; private set; }
    public InteractionLockModel InteractionLock { get; private set; }
    public ItemDatabase ItemDb => itemDatabase;
    
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

        Game.SetContext(this);
    }

    private void InitializeRuntimeState()
    {
        PlayerData = new PlayerData();
        Inventory = new InventoryModel(new InventoryData());
        InventorySelection = new InventorySelectionModel(Inventory);
        LevelState = new LevelStateModel();
        InteractionLock = new InteractionLockModel();
        Audio = new AudioStateModel();
        Audio.Initialize(gameObject); // Global emitter is on game context!

        HardwormPickupSfx = GetComponentInChildren<HardwormPickupSfx>();
        if (HardwormPickupSfx == null)
        {
            Debug.LogWarning("HardwormPickupSfx is null");
        }
        
        Dialogue = dialogueRunner;
        if (Dialogue == null)
            Debug.LogError("GameContext: DialogueRunner reference is missing. Assign it in the inspector.");
    }
}