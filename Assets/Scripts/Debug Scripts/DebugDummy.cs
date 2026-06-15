using IngameDebugConsole;
using UnityEngine;
using UnityEngine.Events;

public class DebugDummy : MonoBehaviour
{
    private int callCount = 1;
    [Header("Events")] [SerializeField] private UnityEvent OnToggleNoclip;
    [SerializeField] private UnityEvent OnToggleDebugView;
    [SerializeField] private UnityEvent OnToggleHyperspeed;

    [SerializeField] private DialogueEncounter encounter;
    [SerializeField] private AudioCue singleTingAc;
    
    [Header("Settings")]
    [SerializeField] private bool allowRToRestart = false;
    [SerializeField] private bool allowEscapeToQuit = false;
    

    [Header("Settings")] [SerializeField] private string nextScene = "Demo Reset";

    private void Awake()
    {
        RegisterConsoleCommands();
        
    }

    void RegisterConsoleCommands()
    {
        DebugLogConsole.AddCommand("/noclip", "Toggles noclip for the player", ToggleNoclip);
        DebugLogConsole.AddCommand("/debugView", "Toggles debug view", ToggleDebugView);
        DebugLogConsole.AddCommand("/restart", "Reloads the current scene", ReloadSameScene);
        DebugLogConsole.AddCommand("/hyperspeed", "Toggles hyperspeed for the player", ToggleHyperspeed);
        DebugLogConsole.AddCommand("/killAllAudio", "Kills all Wwise audio", KillAllWwiseAudio);
        DebugLogConsole.AddCommand<int>("/addHW", "Adds a hardworm pack by pack size. Example: /addHW 3", DebugAddHardwormPack);
        DebugLogConsole.AddCommand("/addKeycard", "Try adds Keycard", DebugAddKeycard);
        DebugLogConsole.AddCommand("/clearInven", "Clears Inventory", ClearInventory);
        DebugLogConsole.AddCommand("/loadNextScene", "Loads the next scene", GoToNextScene);
        DebugLogConsole.AddCommand("/printPersistencePath", "Prints ES3 Persistence Folder path", PrintPersistencePath);
    }

    void Update()
    {
        
        if (allowEscapeToQuit && Input.GetKeyDown(KeyCode.Escape))
        {
            ExitApplication();
        }

        if (PauseUtility.IsPaused)
            return;

        if (Input.GetKeyDown(KeyCode.R) && allowRToRestart)
        {
            ReloadSameScene();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            SetFullscreen(true);
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            SetFullscreen(false);
        }
    }

    public static void ExitApplication()
    {
        ApplicationExitUtility.ExitApplication();
    }

    public void PrintPersistencePath()
    {
        Debug.Log(Application.persistentDataPath);
    }

    public void DebugAddHardwormPack(int packSize)
    {
        if (!Game.IsReady || Game.Ctx?.Inventory == null || Game.Ctx.ItemDb == null)
        {
            Debug.LogWarning("Add hardworm failed: GameContext, Inventory, or ItemDatabase is not ready.");
            return;
        }

        HardwormPackDefinition packDef = Game.Ctx.ItemDb.GetHardwormByPackSize(packSize);
        if (packDef == null || string.IsNullOrWhiteSpace(packDef.itemId))
        {
            Debug.LogWarning($"Add hardworm failed: no hardworm pack definition found for pack size {packSize}.");
            return;
        }

        if (Game.Ctx.Inventory.TryAdd(packDef.itemId, 1))
        {
            Game.Ctx.HardwormPickupSfx?.PlayPickup(packDef);
            Debug.Log($"Added hardworm pack: size={packSize}, itemId={packDef.itemId}");
        }
    }

    public void ClearInventory()
    {
        Game.Ctx.Inventory.Clear();
    }

    public void PrintDebugMessage()
    {
        Debug.Log("Debug Dummy: Call #" + callCount.ToString());
        callCount++;
    }

    public void DebugAddKeycard()
    {
        Game.Ctx.Inventory.TryAdd("012", 1);
        Game.Ctx.Audio.PlayCueGlobal(singleTingAc);
    }
    
    public void SetFullscreen(bool fullscreen)
    {
        DesktopFullscreen.Apply(fullscreen);
    }

    public void DoSceneResets()
    {
        Game.Ctx?.SceneLoader?.PrepareForSceneLoad();
    }

    public void ReloadSameScene()
    {
        if (!Game.IsReady || Game.Ctx?.SceneLoader == null)
        {
            Debug.LogWarning("ReloadSameScene failed: SceneLoader is not ready.");
            return;
        }

        Game.Ctx.SceneLoader.ReloadActiveScene();
    }

    public void GoToNextScene()
    {
        if (!Game.IsReady || Game.Ctx?.SceneLoader == null)
        {
            Debug.LogWarning("GoToNextScene failed: SceneLoader is not ready.");
            return;
        }

        if (Game.Ctx.SceneFlow != null)
        {
            Game.Ctx.SceneFlow.LoadNextScene(nextScene);
            return;
        }

        Game.Ctx.SceneLoader.LoadScene(nextScene);
    }

    public void ToggleNoclip()
    {
        OnToggleNoclip?.Invoke();
    }

    public void ToggleDebugView()
    {
        OnToggleDebugView?.Invoke();
    }

    public void ToggleHyperspeed()
    {
        OnToggleHyperspeed?.Invoke();
    }

    public void KillAllWwiseAudio()
    {
        AkSoundEngine.StopAll();
    }
}
