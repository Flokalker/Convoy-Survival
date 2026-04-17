using UnityEngine;

[DisallowMultipleComponent]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.4f, -11.3f);
    [SerializeField] private Vector3 targetPositionOffset = new Vector3(0f, 0.6f, 0f);
    [SerializeField] private bool lockExactlyBehind = true;
    [SerializeField] private float followSpeed = 8f;

    private CarController carController;

    public void SetTarget(Transform followTarget)
    {
        target = followTarget;
        carController = target != null ? target.GetComponent<CarController>() : null;
    }

    public void Configure(Vector3 followOffset, Vector3 lookTargetOffset, float movementFollowSpeed)
    {
        offset = followOffset;
        targetPositionOffset = lookTargetOffset;
        followSpeed = Mathf.Max(0.01f, movementFollowSpeed);
    }

    public void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        UpdateFollow(true);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        UpdateFollow(lockExactlyBehind);
    }

    private void UpdateFollow(bool instant)
    {
        Vector3 targetBasePosition = GetTargetBasePosition();
        Vector3 headingForward = GetHeadingForward();
        Vector3 headingRight = Vector3.Cross(Vector3.up, headingForward).normalized;
        Vector3 desiredPosition = targetBasePosition
            + headingRight * offset.x
            + Vector3.up * offset.y
            + headingForward * offset.z;
        Vector3 lookDirection = targetBasePosition - desiredPosition;
        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            lookDirection = headingForward;
        }

        Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);

        if (instant)
        {
            transform.position = desiredPosition;
            transform.rotation = desiredRotation;
            return;
        }

        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        transform.rotation = desiredRotation;
    }

    private Vector3 GetTargetBasePosition()
    {
        return target.position + target.TransformDirection(targetPositionOffset);
    }

    private Vector3 GetHeadingForward()
    {
        if (carController != null)
        {
            return carController.DriveForward;
        }

        Vector3 forward = target.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
    }
}
