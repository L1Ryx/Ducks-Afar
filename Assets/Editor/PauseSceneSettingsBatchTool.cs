using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PauseSceneSettingsBatchTool
{
    private const string PrefabDirectory = "Assets/Prefabs/Systems";
    private const string PrefabPath = PrefabDirectory + "/Scene Pause Settings.prefab";
    private const string RootName = "Scene Pause Settings";

    [MenuItem("Ducks Afar/Pause/Create Scene Pause Settings Prefab")]
    public static void CreateScenePauseSettingsPrefab()
    {
        EnsurePrefabExists(selectPrefab: true);
    }

    [MenuItem("Ducks Afar/Pause/Add Pausable Marker To Enabled Build Scenes")]
    public static void AddPausableMarkerToEnabledBuildScenes()
    {
        AddMarkerToBuildScenes(isPausable: true, onlyEnabledScenes: true);
    }

    [MenuItem("Ducks Afar/Pause/Add Pausable Marker To All Build Scenes")]
    public static void AddPausableMarkerToAllBuildScenes()
    {
        AddMarkerToBuildScenes(isPausable: true, onlyEnabledScenes: false);
    }

    [MenuItem("Ducks Afar/Pause/Add Pausable Marker To All Scenes In Assets/Scenes")]
    public static void AddPausableMarkerToAllScenesInAssetsScenes()
    {
        AddMarkerToScenePaths(AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }), isPausable: true);
    }

    private static void AddMarkerToBuildScenes(bool isPausable, bool onlyEnabledScenes)
    {
        var scenes = EditorBuildSettings.scenes;
        int count = 0;

        string[] scenePaths = new string[scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
        {
            if (onlyEnabledScenes && !scenes[i].enabled)
                continue;

            scenePaths[count] = scenes[i].path;
            count++;
        }

        System.Array.Resize(ref scenePaths, count);
        AddMarkerToScenePaths(scenePaths, isPausable);
    }

    private static void AddMarkerToScenePaths(string[] sceneGuidsOrPaths, bool isPausable)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var prefab = EnsurePrefabExists(selectPrefab: false);
        if (prefab == null)
            return;

        string previouslyOpenScenePath = SceneManager.GetActiveScene().path;
        int changedCount = 0;

        try
        {
            foreach (string sceneGuidOrPath in sceneGuidsOrPaths)
            {
                string scenePath = sceneGuidOrPath.EndsWith(".unity")
                    ? sceneGuidOrPath
                    : AssetDatabase.GUIDToAssetPath(sceneGuidOrPath);

                if (string.IsNullOrEmpty(scenePath) || !scenePath.EndsWith(".unity"))
                    continue;

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                if (EnsureMarkerInScene(scene, prefab, isPausable))
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    changedCount++;
                }
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(previouslyOpenScenePath))
                EditorSceneManager.OpenScene(previouslyOpenScenePath, OpenSceneMode.Single);
        }

        Debug.Log($"PauseSceneSettingsBatchTool: added/updated pause markers in {changedCount} scene(s).");
    }

    private static bool EnsureMarkerInScene(Scene scene, GameObject prefab, bool isPausable)
    {
        ScenePauseSettings existing = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            existing = root.GetComponentInChildren<ScenePauseSettings>(includeInactive: true);
            if (existing != null)
                break;
        }

        if (existing != null)
        {
            var serialized = new SerializedObject(existing);
            serialized.FindProperty("isPausable").boolValue = isPausable;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = RootName;

        var settings = instance.GetComponent<ScenePauseSettings>();
        if (settings != null)
        {
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("isPausable").boolValue = isPausable;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        return true;
    }

    private static GameObject EnsurePrefabExists(bool selectPrefab)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null)
        {
            if (selectPrefab)
                Selection.activeObject = existing;

            return existing;
        }

        Directory.CreateDirectory(PrefabDirectory);

        var marker = new GameObject(RootName);
        marker.AddComponent<ScenePauseSettings>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(marker, PrefabPath);
        Object.DestroyImmediate(marker);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (selectPrefab)
            Selection.activeObject = prefab;

        Debug.Log($"PauseSceneSettingsBatchTool: created {PrefabPath}");
        return prefab;
    }
}
