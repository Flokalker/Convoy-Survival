using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;

namespace PostApocRoadtrip.Editor
{
    public static class RoadtripBatchPipeline
    {
        public static void SetupUrpRoadtripLookBatch()
        {
            RoadtripUrpSetup.SetupUrpRoadtripLook();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Batch URP setup completed.");
        }

        public static void RebuildWorldBatch()
        {
            SetupUrpRoadtripLookBatch();
            RebuildWorldScene();
            Debug.Log("Batch world rebuild completed.");
        }

        public static void ScaffoldWorldBatch()
        {
            RebuildWorldBatch();
            WorldStreamingScaffolder.ScaffoldChunkScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Batch world scaffold completed.");
        }

        public static void BuildWebGLBatch()
        {
            RebuildWorldForWebGlBatch();

            var buildPath = "Builds/WebGL";
            CleanWebGlArtifacts(buildPath);
            System.IO.Directory.CreateDirectory(buildPath);
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.Minimal);
            PlayerSettings.runInBackground = true;

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { RoadtripWorldEditorTools.ScenePath },
                locationPathName = buildPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new System.Exception($"WebGL batch build failed with result: {report.summary.result}");
            }

            StampWebGlIndex(buildPath);
            Debug.Log($"Batch WebGL build completed at {buildPath}");
        }

        private static void RebuildWorldForWebGlBatch()
        {
            RoadtripUrpSetup.SetupBuiltInWebGlLook();
            RebuildWorldScene();
            GraphicsSettings.defaultRenderPipeline = null;
            QualitySettings.renderPipeline = null;
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Batch WebGL world rebuild completed with built-in rendering.");
        }

        private static void RebuildWorldScene()
        {
            EditorSceneManager.OpenScene(RoadtripWorldEditorTools.ScenePath);
            var bootstrap = Object.FindObjectOfType<World.RoadtripWorldBootstrap>();
            if (bootstrap == null)
            {
                var bootstrapObject = new GameObject("RoadtripWorldBootstrap");
                bootstrap = bootstrapObject.AddComponent<World.RoadtripWorldBootstrap>();
            }

            bootstrap.RebuildWorld();
            EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
            EditorSceneManager.SaveOpenScenes();
        }

        private static void CleanWebGlArtifacts(string buildPath)
        {
            var fullBuildPath = Path.GetFullPath(buildPath);
            if (Directory.Exists(fullBuildPath))
            {
                Directory.Delete(fullBuildPath, true);
            }

            var webGlBeeArtifacts = Path.GetFullPath("Library/Bee/artifacts/WebGL");
            if (Directory.Exists(webGlBeeArtifacts))
            {
                Directory.Delete(webGlBeeArtifacts, true);
            }

            var buildPlayerData = Path.GetFullPath("Library/BuildPlayerData/WebGL");
            if (Directory.Exists(buildPlayerData))
            {
                Directory.Delete(buildPlayerData, true);
            }

            var stagingArea = Path.GetFullPath("Temp/StagingArea");
            if (Directory.Exists(stagingArea))
            {
                Directory.Delete(stagingArea, true);
            }
        }

        private static void StampWebGlIndex(string buildPath)
        {
            var indexPath = Path.Combine(buildPath, "index.html");
            if (!File.Exists(indexPath))
            {
                return;
            }

            var version = System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var html = File.ReadAllText(indexPath);
            html = html.Replace("var loaderUrl = buildUrl + \"/WebGL.loader.js\";", $"var buildVersion = \"{version}\";\n      var loaderUrl = buildUrl + \"/WebGL.loader.js?v=\" + buildVersion;");
            html = html.Replace("dataUrl: buildUrl + \"/WebGL.data\",", "dataUrl: buildUrl + \"/WebGL.data?v=\" + buildVersion,");
            html = html.Replace("frameworkUrl: buildUrl + \"/WebGL.framework.js\",", "frameworkUrl: buildUrl + \"/WebGL.framework.js?v=\" + buildVersion,");
            html = html.Replace("codeUrl: buildUrl + \"/WebGL.wasm\",", "codeUrl: buildUrl + \"/WebGL.wasm?v=\" + buildVersion,");
            html = html.Replace("productVersion: \"1.0\",", $"productVersion: \"{version}\",");
            File.WriteAllText(indexPath, html);
        }
    }
}
