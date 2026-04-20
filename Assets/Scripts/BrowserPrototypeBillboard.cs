using UnityEngine;

[DisallowMultipleComponent]
public class BrowserPrototypeBillboard : MonoBehaviour
{
    [SerializeField] private bool keepVerticalAxis = true;

    private Camera cachedCamera;

    private void LateUpdate()
    {
        if (cachedCamera == null)
        {
            cachedCamera = Camera.main;
        }

        if (cachedCamera == null)
        {
            return;
        }

        Vector3 lookTarget = cachedCamera.transform.position;
        if (keepVerticalAxis)
        {
            lookTarget.y = transform.position.y;
        }

        transform.LookAt(lookTarget, Vector3.up);
    }
}
