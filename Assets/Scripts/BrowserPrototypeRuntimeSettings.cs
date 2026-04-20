using UnityEngine;

[DisallowMultipleComponent]
public class BrowserPrototypeRuntimeSettings : MonoBehaviour
{
    [Header("Scope")]
    [SerializeField] private bool onlyApplyOnWebGL = false;

    [Header("Performance Defaults")]
    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private int vSyncCount = 0;
    [SerializeField, Min(0f)] private float shadowDistance = 60f;
    [SerializeField] private bool disableRealtimeReflectionProbes = true;

    private void Awake()
    {
#if !UNITY_WEBGL
        if (onlyApplyOnWebGL)
        {
            return;
        }
#endif

        if (targetFrameRate > 0)
        {
            Application.targetFrameRate = targetFrameRate;
        }

        QualitySettings.vSyncCount = Mathf.Max(0, vSyncCount);
        QualitySettings.shadowDistance = shadowDistance;

        if (disableRealtimeReflectionProbes)
        {
            QualitySettings.realtimeReflectionProbes = false;
        }
    }
}
