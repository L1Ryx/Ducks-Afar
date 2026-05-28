using IngameDebugConsole;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class EditorDebugConsoleHotkey : MonoBehaviour
{
    private const string ConsolePrefabPath = "Assets/Plugins/IngameDebugConsole/IngameDebugConsole.prefab";

    [SerializeField] private KeyCode toggleKey = KeyCode.F1;
    [SerializeField] private bool instantiateConsoleIfMissing = true;

    private bool desiredVisible;

    private void Update()
    {
#if UNITY_EDITOR
        if (!Input.GetKeyDown(toggleKey))
            return;

        DebugLogManager manager = GetOrCreateDebugLogManager();
        if (manager == null)
        {
            Debug.LogWarning(
                "Debug console hotkey pressed, but DebugLogManager.Instance is null. " +
                $"Add {ConsolePrefabPath} to Bootstrap/Persistent UI, or keep Instantiate Console If Missing enabled.");
            return;
        }

        desiredVisible = !desiredVisible;

        if (desiredVisible)
        {
            manager.PopupEnabled = true;
            manager.ShowLogWindow();
        }
        else
        {
            manager.HideLogWindow();
            manager.PopupEnabled = false;
        }
#endif
    }

#if UNITY_EDITOR
    private DebugLogManager GetOrCreateDebugLogManager()
    {
        if (DebugLogManager.Instance != null)
            return DebugLogManager.Instance;

        DebugLogManager manager = FindFirstObjectByType<DebugLogManager>(FindObjectsInactive.Exclude);
        if (manager != null)
            return manager;

        if (!instantiateConsoleIfMissing)
            return null;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConsolePrefabPath);
        if (prefab == null)
            return null;

        GameObject instance = Instantiate(prefab);
        instance.name = prefab.name;
        return instance.GetComponent<DebugLogManager>();
    }
#endif
}
