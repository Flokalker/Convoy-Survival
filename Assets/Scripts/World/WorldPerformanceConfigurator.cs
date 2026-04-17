using UnityEngine;

namespace PostApocRoadtrip.World
{
    [DisallowMultipleComponent]
    public class WorldPerformanceConfigurator : MonoBehaviour
    {
        [SerializeField] private float lodBias = 1f;
        [SerializeField] private float maximumLODLevel = 0;
        [SerializeField] private float shadowDistance = 110f;
        [SerializeField] private int pixelLightCount = 2;
        [SerializeField] private int anisotropicTextures = 2;
        [SerializeField] private int antiAliasing = 2;
        [SerializeField] private bool runOnAwake = true;

        private void Awake()
        {
            if (runOnAwake)
            {
                Apply();
            }
        }

        [ContextMenu("Apply Performance Budget")]
        public void Apply()
        {
            QualitySettings.lodBias = lodBias;
            QualitySettings.maximumLODLevel = Mathf.RoundToInt(maximumLODLevel);
            QualitySettings.shadowDistance = shadowDistance;
            QualitySettings.pixelLightCount = pixelLightCount;
            QualitySettings.antiAliasing = antiAliasing;
            QualitySettings.anisotropicFiltering = (AnisotropicFiltering)Mathf.Clamp(anisotropicTextures, 0, 2);
            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.billboardsFaceCameraPosition = true;
            QualitySettings.streamingMipmapsActive = true;
            QualitySettings.streamingMipmapsMemoryBudget = 256f;
        }
    }
}
