using UnityEngine;

namespace PostApocRoadtrip.World
{
    [DisallowMultipleComponent]
    public class HorizonBandFollower : MonoBehaviour
    {
        [Range(0f, 1f)] public float followXMultiplier = 0.06f;
        [Range(0f, 1f)] public float followZMultiplier = 0.5f;
        public Vector3 positionOffset = new Vector3(0f, 0f, 380f);
        public bool resolveTargetAutomatically = true;

        private Transform target;
        private float anchoredY;

        private void Awake()
        {
            anchoredY = transform.position.y;
            ResolveTarget();
            UpdatePosition(true);
        }

        private void OnEnable()
        {
            if (resolveTargetAutomatically)
            {
                ResolveTarget();
            }

            UpdatePosition(true);
        }

        private void LateUpdate()
        {
            if (target == null && resolveTargetAutomatically)
            {
                ResolveTarget();
            }

            UpdatePosition(false);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            UpdatePosition(true);
        }

        private void ResolveTarget()
        {
            if (Camera.main != null)
            {
                target = Camera.main.transform;
                return;
            }

            var previewCar = FindObjectOfType<PreviewCarController>();
            if (previewCar != null)
            {
                target = previewCar.transform;
            }
        }

        private void UpdatePosition(bool immediate)
        {
            if (target == null)
            {
                return;
            }

            var destination = new Vector3(
                target.position.x * followXMultiplier + positionOffset.x,
                anchoredY + positionOffset.y,
                target.position.z * followZMultiplier + positionOffset.z);

            transform.position = immediate
                ? destination
                : Vector3.Lerp(transform.position, destination, Time.deltaTime * 1.5f);
        }
    }
}
