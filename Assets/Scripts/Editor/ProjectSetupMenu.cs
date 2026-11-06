#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Cmis3150.Fps.Core;

namespace Cmis3150.Fps.EditorTools
{
    /// <summary>
    /// Editor menu + batch-mode entry points.
    ///  CMIS3150 → Build Level In Scene : generates MainLevel.unity from LevelBootstrap, bakes the NavMeshSurface, saves.
    ///  CMIS3150 → Build Windows Player : builds Builds/Windows/Project2FPS.exe
    /// Batch: Unity.exe -batchmode -quit -projectPath . -executeMethod Cmis3150.Fps.EditorTools.ProjectSetupMenu.BatchBuildAll
    /// </summary>
    public static class ProjectSetupMenu
    {
        const string ScenePath = "Assets/Scenes/MainLevel.unity";
        const string BuildPath = "Builds/Windows/Project2FPS.exe";

        [MenuItem("CMIS3150/Open Main Level")]
        public static void OpenMainLevel()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("CMIS3150/Build Level In Scene")]
        public static void BuildLevelInScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var go = new GameObject("LevelBootstrap");
            var lb = go.AddComponent<LevelBootstrap>();
            lb.Build();
            Object.DestroyImmediate(go); // the generated objects stay; the generator is not needed at runtime

            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[CMIS3150] Level generated, NavMesh baked, scene saved: " + ScenePath);
        }

        [MenuItem("CMIS3150/Build Windows Player")]
        public static void BuildWindows()
        {
            PlayerSettings.productName = "Project2FPS";
            PlayerSettings.companyName = "CMIS3150";
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);

            Directory.CreateDirectory(Path.GetDirectoryName(BuildPath));
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = BuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(opts);
            Debug.Log($"[CMIS3150] Build result: {report.summary.result}, errors: {report.summary.totalErrors}, size: {report.summary.totalSize}");
            if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }

        public static void BatchBuildLevel() => BuildLevelInScene();

        public static void BatchBuildAll()
        {
            BuildLevelInScene();
            BuildWindows();
        }
    }
}
#endif
