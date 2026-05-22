using System;
using System.Runtime.CompilerServices;
using IngameDebugConsole;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

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
        DebugLogConsole.AddCommand("/addHW1", "Try adds 1-Hardworm Pack", DebugAddHardwormOne);
        DebugLogConsole.AddCommand("/addHW2", "Try adds 2-Hardworm Pack", DebugAddHardwormTwo);
        DebugLogConsole.AddCommand("/addHW3", "Try adds 3-Hardworm Pack", DebugAddHardwormThree);
        DebugLogConsole.AddCommand("/addHW4", "Try adds 4-Hardworm Pack", DebugAddHardwormFour);
        DebugLogConsole.AddCommand("/addHW5", "Try adds 5-Hardworm Pack", DebugAddHardwormFive);
        DebugLogConsole.AddCommand("/addKeycard", "Try adds Keycard", DebugAddKeycard);
        DebugLogConsole.AddCommand("/clearInven", "Clears Inventory", ClearInventory);
        DebugLogConsole.AddCommand("/loadNextScene", "Loads the next scene", GoToNextScene);
        DebugLogConsole.AddCommand("/printPersistencePath", "Prints ES3 Persistence Folder path", PrintPersistencePath);
    }

    void Update()
    {
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

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

    public void PrintPersistencePath()
    {
        Debug.Log(Application.persistentDataPath);
    }

    public void DebugAddHardwormOne()
    {
        Game.Ctx.Inventory.TryAdd("001", 1);
    }
    
    public void DebugAddHardwormTwo()
    {
        Game.Ctx.Inventory.TryAdd("002", 1);
    }
    
    public void DebugAddHardwormThree()
    {
        Game.Ctx.Inventory.TryAdd("003", 1);
    }
    
    public void DebugAddHardwormFour()
    {
        Game.Ctx.Inventory.TryAdd("004", 1);
    }
    
    public void DebugAddHardwormFive()
    {
        Game.Ctx.Inventory.TryAdd("005", 1);
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
        if (fullscreen)
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
        }
    }

    public void DoSceneResets()
    {
        if (Game.IsReady && Game.Ctx != null)
        {
            Game.Ctx.Audio?.StopGlobalAmbience(immediate: false);
            Game.Ctx.LevelState?.Reset();
            Game.Ctx.InteractionLock?.ForceClear();
            Game.Ctx.Inventory.Clear();
            // Optional: reset other per-run state here (inventory, selections, etc.)
        }
    }
    /*
     * SHAWN REFACTOR THIS INTO ANOTHER GAMEOBJECT PLEASE IM BEGGING
     */
    public void ReloadSameScene()
    {
        DoSceneResets();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToNextScene()
    {
        DoSceneResets();
        SceneManager.LoadScene(nextScene);
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
