using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PostApocRoadtrip.Editor
{
    public static class RoadtripUrpSetup
    {
        private const string SettingsFolder = "Assets/Settings";
        private const string RenderingFolder = "Assets/Settings/Rendering";
        private const string RendererPath = "Assets/Settings/Rendering/RoadtripForwardRenderer.asset";
        private const string PipelinePath = "Assets/Settings/Rendering/RoadtripUrp.asset";
        private const string VolumeProfilePath = "Assets/Settings/Rendering/RoadtripRoadsideVolume.asset";

        [MenuItem("Tools/Roadtrip World/Setup URP Roadtrip Look")]
        public static void SetupUrpRoadtripLook()
        {
            EnsureFolder("Assets", "Settings");
            EnsureFolder(SettingsFolder, "Rendering");

            var rendererData = LoadOrCreateRendererData();
            var pipelineAsset = LoadOrCreatePipelineAsset(rendererData);
            var volumeProfile = LoadOrCreateVolumeProfile();

            GraphicsSettings.defaultRenderPipeline = pipelineAsset;

            var previousQualityLevel = QualitySettings.GetQualityLevel();
            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipelineAsset;
            }

            QualitySettings.SetQualityLevel(previousQualityLevel, false);
            QualitySettings.renderPipeline = pipelineAsset;

            ConfigureSceneVolume(volumeProfile);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("URP roadtrip look configured.");
        }

        public static void SetupBuiltInWebGlLook()
        {
            GraphicsSettings.defaultRenderPipeline = null;

            var previousQualityLevel = QualitySettings.GetQualityLevel();
            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = null;
            }

            QualitySettings.SetQualityLevel(previousQualityLevel, false);
            QualitySettings.renderPipeline = null;

            DisableSceneVolumes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Built-in WebGL render path configured.");
        }

        private static UniversalRendererData LoadOrCreateRendererData()
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (rendererData != null)
            {
                return rendererData;
            }

            rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, RendererPath);
            EditorUtility.SetDirty(rendererData);
            return rendererData;
        }

        private static UniversalRenderPipelineAsset LoadOrCreatePipelineAsset(UniversalRendererData rendererData)
        {
            var pipelineAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipelineAsset == null)
            {
                pipelineAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
                AssetDatabase.CreateAsset(pipelineAsset, PipelinePath);
            }

            var serializedObject = new SerializedObject(pipelineAsset);
            SetObjectArrayElement(serializedObject.FindProperty("m_RendererDataList"), 0, rendererData);
            SetInt(serializedObject.FindProperty("m_DefaultRendererIndex"), 0);
            SetBool(serializedObject.FindProperty("m_RequireDepthTexture"), false);
            SetBool(serializedObject.FindProperty("m_RequireOpaqueTexture"), false);
            SetBool(serializedObject.FindProperty("m_SupportsHDR"), true);
            SetBool(serializedObject.FindProperty("m_MainLightShadowsSupported"), true);
            SetBool(serializedObject.FindProperty("m_AdditionalLightShadowsSupported"), false);
            SetBool(serializedObject.FindProperty("m_SoftShadowsSupported"), true);
            SetBool(serializedObject.FindProperty("m_UseSRPBatcher"), true);
            SetFloat(serializedObject.FindProperty("m_RenderScale"), 1f);
            SetFloat(serializedObject.FindProperty("m_ShadowDistance"), 105f);
            SetInt(serializedObject.FindProperty("m_MSAA"), 2);
            SetInt(serializedObject.FindProperty("m_MainLightRenderingMode"), 1);
            SetInt(serializedObject.FindProperty("m_AdditionalLightsRenderingMode"), 1);
            SetInt(serializedObject.FindProperty("m_AdditionalLightsPerObjectLimit"), 4);
            SetInt(serializedObject.FindProperty("m_ColorGradingMode"), 0);
            SetInt(serializedObject.FindProperty("m_ColorGradingLutSize"), 32);
            SetInt(serializedObject.FindProperty("m_ShadowCascadeCount"), 2);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(pipelineAsset);
            return pipelineAsset;
        }

        private static VolumeProfile LoadOrCreateVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }

            var colorAdjustments = GetOrAdd<ColorAdjustments>(profile);
            colorAdjustments.active = true;
            colorAdjustments.postExposure.overrideState = true;
            colorAdjustments.postExposure.value = -0.28f;
            colorAdjustments.contrast.overrideState = true;
            colorAdjustments.contrast.value = 18f;
            colorAdjustments.saturation.overrideState = true;
            colorAdjustments.saturation.value = -22f;
            colorAdjustments.colorFilter.overrideState = true;
            colorAdjustments.colorFilter.value = new Color(0.82f, 0.9f, 1f, 1f);

            var whiteBalance = GetOrAdd<WhiteBalance>(profile);
            whiteBalance.active = true;
            whiteBalance.temperature.overrideState = true;
            whiteBalance.temperature.value = -18f;
            whiteBalance.tint.overrideState = true;
            whiteBalance.tint.value = -3f;

            var bloom = GetOrAdd<Bloom>(profile);
            bloom.active = true;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 1.35f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.05f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.68f;
            bloom.clamp.overrideState = true;
            bloom.clamp.value = 65472f;
            bloom.highQualityFiltering.overrideState = true;
            bloom.highQualityFiltering.value = false;

            var vignette = GetOrAdd<Vignette>(profile);
            vignette.active = true;
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.16f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.34f;

            var tonemapping = GetOrAdd<Tonemapping>(profile);
            tonemapping.active = true;
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.ACES;

            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void ConfigureSceneVolume(VolumeProfile profile)
        {
            EditorSceneManager.OpenScene(RoadtripWorldEditorTools.ScenePath);

            var volume = Object.FindObjectOfType<Volume>();
            if (volume == null)
            {
                var volumeObject = new GameObject("Global Volume");
                volume = volumeObject.AddComponent<Volume>();
            }

            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = profile;
            EditorUtility.SetDirty(volume);
            EditorSceneManager.MarkAllScenesDirty();
        }

        private static void DisableSceneVolumes()
        {
            EditorSceneManager.OpenScene(RoadtripWorldEditorTools.ScenePath);

            foreach (var volume in Object.FindObjectsOfType<Volume>(true))
            {
                volume.enabled = false;
                EditorUtility.SetDirty(volume);
            }

            EditorSceneManager.MarkAllScenesDirty();
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = Path.Combine(parent, child).Replace("\\", "/");
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet(out T component))
            {
                return component;
            }

            return profile.Add<T>(true);
        }

        private static void SetBool(SerializedProperty property, bool value)
        {
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static void SetFloat(SerializedProperty property, float value)
        {
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void SetInt(SerializedProperty property, int value)
        {
            if (property != null)
            {
                property.intValue = value;
            }
        }

        private static void SetObjectArrayElement(SerializedProperty property, int index, Object value)
        {
            if (property == null)
            {
                return;
            }

            if (property.arraySize <= index)
            {
                property.arraySize = index + 1;
            }

            property.GetArrayElementAtIndex(index).objectReferenceValue = value;
        }
    }
}
