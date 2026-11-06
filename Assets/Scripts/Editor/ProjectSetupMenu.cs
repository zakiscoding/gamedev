#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cmis3150.Fps.Core;

namespace Cmis3150.Fps.EditorTools
{
    /// <summary>
    /// Menu helpers for opening the bootstrapped level and reminding about Windows builds.
    /// </summary>
    public static class ProjectSetupMenu
    {
        const string ScenePath = "Assets/Scenes/MainLevel.unity";

        [MenuItem("CMIS3150/Open Main Level")]
        public static void OpenMainLevel()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("CMIS3150/Create Bootstrap Object In Active Scene")]
        public static void EnsureBootstrap()
        {
            if (Object.FindFirstObjectByType<LevelBootstrap>() != null)
            {
                Debug.Log("LevelBootstrap already present.");
                return;
            }

            var go = new GameObject("LevelBootstrap");
            go.AddComponent<LevelBootstrap>();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = go;
            Debug.Log("Added LevelBootstrap. Press Play — the level builds itself and bakes a NavMesh at runtime.");
        }

        [MenuItem("CMIS3150/Print Windows Build Checklist")]
        public static void BuildChecklist()
        {
            Debug.Log(
                "Windows standalone checklist:\n" +
                "1) Unity Hub → install Windows Build Support (Mono) for this Editor.\n" +
                "2) Edit → Project Settings → Player → Active Input Handling = Input System Package (or Both).\n" +
                "3) File → Build Profiles → Windows → Build.\n" +
                "4) Zip the whole build folder (exe + _Data), not just the .exe.\n" +
                "5) Record gameplay (win+fail) and ≤15 min narrated code walkthrough (zone→chase→shoot→patrol).");
        }
    }
}
#endif
