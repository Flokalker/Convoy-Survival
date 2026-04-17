using System.IO;
using PostApocRoadtrip.World;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PostApocRoadtrip.Editor
{
    public static class RoadtripWorldEditorTools
    {
        public const string ScenePath = "Assets/Scenes/PostApocRoadtrip.unity";
        private const string BuildPath = "Builds/WebGL";

        [MenuItem("Tools/Roadtrip World/Open World Scene")]
        public static void OpenWorldScene()
        {
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Tools/Roadtrip World/Rebuild World")]
        public static void RebuildWorld()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                RoadtripUrpSetup.SetupUrpRoadtripLook();
                EditorSceneManager.OpenScene(ScenePath);
                var bootstrap = Object.FindObjectOfType<RoadtripWorldBootstrap>();
                if (bootstrap == null)
                {
                    var bootstrapObject = new GameObject("RoadtripWorldBootstrap");
                    bootstrap = bootstrapObject.AddComponent<RoadtripWorldBootstrap>();
                }

                bootstrap.RebuildWorld();
                EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
                Debug.Log("Roadtrip world rebuilt.");
            }
        }

        [MenuItem("Tools/Roadtrip World/Build WebGL Localhost")]
        public static void BuildWebGLLocalhost()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                RoadtripUrpSetup.SetupBuiltInWebGlLook();
                EditorSceneManager.OpenScene(ScenePath);
                var bootstrap = Object.FindObjectOfType<RoadtripWorldBootstrap>();
                if (bootstrap == null)
                {
                    var bootstrapObject = new GameObject("RoadtripWorldBootstrap");
                    bootstrap = bootstrapObject.AddComponent<RoadtripWorldBootstrap>();
                }

                bootstrap.RebuildWorld();
                EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
                EditorSceneManager.SaveOpenScenes();
            }

            Directory.CreateDirectory(BuildPath);
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.Minimal);
            PlayerSettings.runInBackground = true;

            var scenes = EditorBuildSettings.scenes;
            if (scenes == null || scenes.Length == 0)
            {
                scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = GetEnabledScenePaths(scenes),
                locationPathName = BuildPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"WebGL build completed: {Path.GetFullPath(BuildPath)}");
            }
            else
            {
                Debug.LogError("WebGL build failed. Check the Unity console for details.");
            }
        }

        private static string[] GetEnabledScenePaths(EditorBuildSettingsScene[] scenes)
        {
            var enabledScenes = new System.Collections.Generic.List<string>();
            foreach (var scene in scenes)
            {
                if (scene != null && scene.enabled)
                {
                    enabledScenes.Add(scene.path);
                }
            }

            if (enabledScenes.Count == 0)
            {
                enabledScenes.Add(ScenePath);
            }

            return enabledScenes.ToArray();
        }
    }
}
