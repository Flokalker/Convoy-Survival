using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -12f);
    [SerializeField] private float followSmoothness = 8f;
    [SerializeField] private float lookSmoothness = 10f;

    public void Configure(Transform followTarget, Vector3 followOffset)
    {
        target = followTarget;
        offset = followOffset;

        transform.position = target.position + offset;
        transform.rotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSmoothness * Time.deltaTime
        );

        Quaternion desiredRotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            lookSmoothness * Time.deltaTime
        );
    }
}
