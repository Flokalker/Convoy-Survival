using UnityEngine;

namespace PostApocRoadtrip.World
{
    [RequireComponent(typeof(Rigidbody))]
    public class PreviewCarController : MonoBehaviour
    {
        public Transform cameraPivot;
        public float acceleration = 16f;
        public float maxSpeed = 24f;
        public float steeringSpeed = 58f;
        public float braking = 20f;
        public Vector3 cameraOffset = new Vector3(0f, 4.6f, -10.2f);
        public bool autopilot;
        public float collisionSkin = 0.18f;
        public float ramKillSpeed = 7f;
        public float groundFollowHeight = 0.55f;
        public float groundRayHeight = 5f;
        public float groundRayDistance = 10f;

        private Rigidbody body;
        private BoxCollider vehicleCollider;
        private Transform followCamera;
        private float currentSpeed;
        private float lockedHeight;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.None;
            vehicleCollider = GetComponent<BoxCollider>();
            lockedHeight = transform.position.y;
            EnsureCamera();
        }

        private void Update()
        {
            var throttle = autopilot ? 1f : Input.GetAxis("Vertical");
            var steerInput = autopilot ? Mathf.Sin(Time.time * 0.24f) * 0.2f : Input.GetAxis("Horizontal");
            var targetSpeed = throttle * maxSpeed;
            var rate = Mathf.Abs(throttle) > 0.01f ? acceleration : braking;

            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.deltaTime);

            var steerFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / 6f);
            transform.rotation *= Quaternion.Euler(0f, steerInput * steeringSpeed * steerFactor * Time.deltaTime, 0f);
            var move = transform.forward * (currentSpeed * Time.deltaTime);
            if (move.sqrMagnitude > 0.0001f && IsBlocked(move))
            {
                currentSpeed = 0f;
                move = Vector3.zero;
            }

            transform.position += move;
            FollowDriveSurface();
            TryRamZombies();
        }

        private bool IsBlocked(Vector3 move)
        {
            if (vehicleCollider == null)
            {
                return false;
            }

            var distance = move.magnitude;
            var direction = move / distance;
            var center = transform.TransformPoint(vehicleCollider.center);
            var halfExtents = Vector3.Scale(vehicleCollider.size * 0.5f, transform.lossyScale);
            halfExtents = new Vector3(
                Mathf.Max(0.1f, halfExtents.x - collisionSkin),
                Mathf.Max(0.1f, halfExtents.y - collisionSkin),
                Mathf.Max(0.1f, halfExtents.z - collisionSkin));

            var hits = Physics.BoxCastAll(center, halfExtents, direction, transform.rotation, distance + collisionSkin, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (IsDriveSurface(hit.collider))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private void FollowDriveSurface()
        {
            var rayOrigin = transform.position + Vector3.up * groundRayHeight;
            var hits = Physics.RaycastAll(rayOrigin, Vector3.down, groundRayDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform) || hit.normal.y < 0.45f)
                {
                    continue;
                }

                if (!IsDriveSurface(hit.collider) && !hit.collider.name.Contains("Ground"))
                {
                    continue;
                }

                var targetY = hit.point.y + groundFollowHeight;
                transform.position = new Vector3(transform.position.x, Mathf.Lerp(transform.position.y, targetY, Time.deltaTime * 9f), transform.position.z);
                return;
            }

            transform.position = new Vector3(transform.position.x, Mathf.Lerp(transform.position.y, lockedHeight, Time.deltaTime * 5f), transform.position.z);
        }

        private void TryRamZombies()
        {
            if (Mathf.Abs(currentSpeed) < ramKillSpeed)
            {
                return;
            }

            var center = transform.position + transform.forward * 1.25f + Vector3.up * 0.8f;
            var hits = Physics.OverlapBox(center, new Vector3(1.45f, 1.05f, 2.25f), transform.rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit == null || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                var zombie = hit.GetComponentInParent<ZombieEnemy>();
                if (zombie != null)
                {
                    zombie.TakeRamHit(Mathf.Abs(currentSpeed));
                }
            }
        }

        private static bool IsDriveSurface(Collider candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            var name = candidate.name;
            return name.Contains("DriveRamp") || name.Contains("ElevatedDrive") || name.Contains("UpperRoad") || name.Contains("Ground");
        }

        private void LateUpdate()
        {
            if (followCamera == null)
            {
                EnsureCamera();
                if (followCamera == null)
                {
                    return;
                }
            }

            var pivot = cameraPivot != null ? cameraPivot : transform;
            var targetPosition = pivot.TransformPoint(cameraOffset);
            followCamera.position = Vector3.Lerp(followCamera.position, targetPosition, Time.deltaTime * 5f);

            var lookTarget = pivot.position + Vector3.up * 1.2f;
            var targetRotation = Quaternion.LookRotation(lookTarget - followCamera.position, Vector3.up);
            followCamera.rotation = Quaternion.Lerp(followCamera.rotation, targetRotation, Time.deltaTime * 6f);
        }

        private void EnsureCamera()
        {
            if (Camera.main != null)
            {
                followCamera = Camera.main.transform;
                return;
            }

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var cameraComponent = cameraObject.AddComponent<Camera>();
            cameraComponent.fieldOfView = 61f;
            cameraComponent.nearClipPlane = 0.1f;
            cameraComponent.farClipPlane = 420f;
            followCamera = cameraObject.transform;
        }

        public void Halt()
        {
            currentSpeed = 0f;
        }
    }
}
