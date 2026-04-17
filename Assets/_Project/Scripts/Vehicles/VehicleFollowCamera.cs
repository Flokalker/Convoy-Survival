using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class VehicleFollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 2.6f, -5.5f);
    [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.1f, 0f);
    [SerializeField, Min(0f)] private float followSmoothTime = 0.12f;
    [SerializeField, Min(0f)] private float rotationLerpSpeed = 8f;

    private Vector3 followVelocity;

    public void SetTarget(Transform followTarget)
    {
        target = followTarget;
        SnapToTarget();
    }

    public void SetCameraActive(bool activeState)
    {
        gameObject.SetActive(activeState);

        if (activeState)
        {
            SnapToTarget();
        }
    }

    private void Start()
    {
        if (target != null && gameObject.activeInHierarchy)
        {
            SnapToTarget();
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        // The camera follows a fixed offset to stay simple and stable for the first vehicle version.
        Vector3 desiredPosition = target.TransformPoint(followOffset);
        transform.position = followSmoothTime <= 0f
            ? desiredPosition
            : Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, followSmoothTime);

        Vector3 lookTarget = target.TransformPoint(lookOffset);
        Vector3 lookDirection = lookTarget - transform.position;
        if (lookDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        float t = rotationLerpSpeed <= 0f ? 1f : 1f - Mathf.Exp(-rotationLerpSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, t);
    }

    private void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        transform.position = target.TransformPoint(followOffset);

        Vector3 lookTarget = target.TransformPoint(lookOffset);
        Vector3 lookDirection = lookTarget - transform.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }
    }
}
