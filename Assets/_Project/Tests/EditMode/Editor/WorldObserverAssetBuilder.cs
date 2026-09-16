#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public static class WorldObserverAssetBuilder
{
    public const string PrefabPath = "Assets/_Project/Prefabs/WorldObserver.prefab";
    public const string ScenePath = "Assets/_Project/Scenes/WorldObserver.unity";

    public static void Build()
    {
        EnsureFolder("Assets/_Project", "Scenes");

        GameObject root = new GameObject("WorldObserver");
        WorldObserverDemoBootstrap bootstrap = root.AddComponent<WorldObserverDemoBootstrap>();
        bootstrap.EnsureReady();
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        PrefabUtility.InstantiatePrefab(prefab, scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(0);
        }
    }

    public static void ImportTmpEssentials()
    {
        string packageCache = Path.Combine(Application.dataPath, "..", "Library", "PackageCache");
        string[] uiPackages = Directory.GetDirectories(packageCache, "com.unity.ugui@*");
        if (uiPackages.Length == 0)
        {
            throw new DirectoryNotFoundException("Unity UI package cache was not found.");
        }

        string packagePath = Path.Combine(uiPackages[0], "Package Resources", "TMP Essential Resources.unitypackage");
        AssetDatabase.importPackageCompleted += OnTmpPackageImported;
        AssetDatabase.importPackageFailed += OnTmpPackageImportFailed;
        AssetDatabase.ImportPackage(Path.GetFullPath(packagePath), false);
    }

    public static void AddGmConsoleToAssets()
    {
        GameObject prefabContents = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            WorldObserverCanvasView view = prefabContents.GetComponentInChildren<WorldObserverCanvasView>(true);
            if (view == null)
            {
                throw new InvalidOperationException("WorldObserver prefab has no CanvasView.");
            }

            view.EnsureReady();
            PrefabUtility.SaveAsPrefabAsset(prefabContents, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        WorldObserverDemoBootstrap bootstrap = UnityEngine.Object.FindFirstObjectByType<WorldObserverDemoBootstrap>();
        if (bootstrap == null)
        {
            throw new InvalidOperationException("WorldObserver scene has no demo bootstrap.");
        }

        bootstrap.EnsureReady();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(0);
        }
    }

    private static void OnTmpPackageImported(string packageName)
    {
        AssetDatabase.importPackageCompleted -= OnTmpPackageImported;
        AssetDatabase.importPackageFailed -= OnTmpPackageImportFailed;
        AssetDatabase.Refresh();
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(0);
        }
    }

    private static void OnTmpPackageImportFailed(string packageName, string errorMessage)
    {
        AssetDatabase.importPackageCompleted -= OnTmpPackageImported;
        AssetDatabase.importPackageFailed -= OnTmpPackageImportFailed;
        Debug.LogError("TMP package import failed: " + packageName + " :: " + errorMessage);
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(1);
        }
    }

    private static void EnsureFolder(string parent, string child)
    {
        string fullPath = parent + "/" + child;
        if (AssetDatabase.IsValidFolder(fullPath) == false)
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
#endif
