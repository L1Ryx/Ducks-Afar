using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneSettingsBatchTool
{
    private const string PrefabDirectory = "Assets/Prefabs/Systems";
    private const string PrefabPath = PrefabDirectory + "/Scene Settings.prefab";
    private const string RootName = "Scene Settings";

    [MenuItem("Ducks Afar/Scene Settings/Create Scene Settings Prefab")]
    public static void CreateSceneSettingsPrefab()
    {
        EnsurePrefabExists(selectPrefab: true);
    }

    [MenuItem("Ducks Afar/Scene Settings/Add Scene Settings To Enabled Build Scenes")]
    public static void AddPausableMarkerToEnabledBuildScenes()
    {
        AddMarkerToBuildScenes(isPausable: true, onlyEnabledScenes: true);
    }

    [MenuItem("Ducks Afar/Scene Settings/Add Scene Settings To All Build Scenes")]
    public static void AddPausableMarkerToAllBuildScenes()
    {
        AddMarkerToBuildScenes(isPausable: true, onlyEnabledScenes: false);
    }

    [MenuItem("Ducks Afar/Scene Settings/Add Scene Settings To All Scenes In Assets/Scenes")]
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

        Debug.Log($"SceneSettingsBatchTool: added/updated scene settings in {changedCount} scene(s).");
    }

    private static bool EnsureMarkerInScene(Scene scene, GameObject prefab, bool isPausable)
    {
        SceneSettings existing = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            existing = root.GetComponentInChildren<SceneSettings>(includeInactive: true);
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

        var settings = instance.GetComponent<SceneSettings>();
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
        marker.AddComponent<SceneSettings>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(marker, PrefabPath);
        Object.DestroyImmediate(marker);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (selectPrefab)
            Selection.activeObject = prefab;

        Debug.Log($"SceneSettingsBatchTool: created {PrefabPath}");
        return prefab;
    }
}
