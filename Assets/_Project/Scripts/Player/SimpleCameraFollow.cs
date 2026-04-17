using UnityEngine;

[DisallowMultipleComponent]
public class SimpleCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 4f, -8f);
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);
    [SerializeField, Min(0.01f)] private float positionLerpSpeed = 6f;
    [SerializeField, Min(0.01f)] private float rotationLerpSpeed = 8f;

    private void Start()
    {
        SnapToTarget();
    }

    private void OnEnable()
    {
        SnapToTarget();
    }

    private void OnValidate()
    {
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.TransformPoint(offset);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, positionLerpSpeed * Time.deltaTime);

        Vector3 lookPoint = target.position + lookAtOffset;
        Quaternion desiredRotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationLerpSpeed * Time.deltaTime);
    }

    private void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.TransformPoint(offset);
        transform.position = desiredPosition;

        Vector3 lookPoint = target.position + lookAtOffset;
        transform.rotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
    }
}
