using UnityEngine;

namespace ConvoySurvival.Run
{
    [DisallowMultipleComponent]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 7f, -12f);
        [SerializeField, Min(1f)] private float followSpeed = 8f;
        [SerializeField, Min(1f)] private float lookAheadDistance = 14f;

        public void SetTarget(Transform followTarget)
        {
            target = followTarget;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desired = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);

            Vector3 lookTarget = target.position + Vector3.forward * lookAheadDistance;
            transform.rotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
        }
    }
}
