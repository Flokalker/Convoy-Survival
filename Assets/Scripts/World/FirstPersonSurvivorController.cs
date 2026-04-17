using UnityEngine;

namespace PostApocRoadtrip.World
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonSurvivorController : MonoBehaviour
    {
        public float walkSpeed = 5.2f;
        public float sprintSpeed = 7.4f;
        public float mouseSensitivity = 2.2f;
        public float gravity = -18f;
        public float weaponDamage = 18f;
        public float weaponRange = 70f;
        public float fireInterval = 0.22f;

        private CharacterController characterController;
        private Transform viewCamera;
        private float pitch;
        private float verticalVelocity;
        private float nextFireTime;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            EnsureCamera();
            CreateWeaponVisual();
        }

        private void OnEnable()
        {
            EnsureCamera();
            var euler = transform.eulerAngles;
            pitch = 0f;
            if (viewCamera != null)
            {
                viewCamera.gameObject.SetActive(true);
                viewCamera.position = transform.position + Vector3.up * 1.65f;
                viewCamera.rotation = Quaternion.Euler(pitch, euler.y, 0f);
            }
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            Look();
            Move();

            if (Input.GetMouseButton(0))
            {
                TryShoot();
            }
        }

        private void LateUpdate()
        {
            if (viewCamera == null)
            {
                EnsureCamera();
            }

            if (viewCamera != null)
            {
                viewCamera.position = transform.position + Vector3.up * 1.65f;
                viewCamera.rotation = Quaternion.Euler(pitch, transform.eulerAngles.y, 0f);
            }
        }

        private void Look()
        {
            var mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            var mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            transform.Rotate(Vector3.up * mouseX);
            pitch = Mathf.Clamp(pitch - mouseY, -75f, 76f);
        }

        private void Move()
        {
            var speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
            var input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
            input = Vector3.ClampMagnitude(input, 1f);

            var move = transform.right * input.x + transform.forward * input.z;
            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            verticalVelocity += gravity * Time.deltaTime;
            move = move * speed + Vector3.up * verticalVelocity;
            characterController.Move(move * Time.deltaTime);
        }

        private void TryShoot()
        {
            if (Time.time < nextFireTime || viewCamera == null)
            {
                return;
            }

            nextFireTime = Time.time + fireInterval;
            var ray = new Ray(viewCamera.position, viewCamera.forward);
            var hits = Physics.RaycastAll(ray, weaponRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                var zombie = hit.collider.GetComponentInParent<ZombieEnemy>();
                if (zombie != null)
                {
                    zombie.TakeBulletDamage(weaponDamage);
                    SpawnPlayerTracer(ray.origin, hit.point);
                }

                return;
            }
        }

        private void SpawnPlayerTracer(Vector3 start, Vector3 end)
        {
            var direction = end - start;
            var distance = direction.magnitude;
            if (distance <= 0.1f)
            {
                return;
            }

            var tracer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tracer.name = "PlayerShotTracer";
            tracer.transform.position = start + direction * 0.5f;
            tracer.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            tracer.transform.localScale = new Vector3(0.025f, 0.025f, distance);
            var renderer = tracer.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
                {
                    color = new Color(0.75f, 0.95f, 1f, 1f)
                };
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", new Color(0.7f, 1.4f, 1.8f, 1f));
                }

                renderer.sharedMaterial = material;
            }

            var collider = tracer.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Destroy(tracer, 0.06f);
        }

        private void EnsureCamera()
        {
            if (Camera.main != null)
            {
                viewCamera = Camera.main.transform;
                return;
            }

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var cameraComponent = cameraObject.AddComponent<Camera>();
            cameraComponent.fieldOfView = 64f;
            cameraComponent.nearClipPlane = 0.1f;
            cameraComponent.farClipPlane = 420f;
            viewCamera = cameraObject.transform;
        }

        private void CreateWeaponVisual()
        {
            if (transform.Find("WeaponVisual") != null)
            {
                return;
            }

            var weaponRoot = new GameObject("WeaponVisual").transform;
            weaponRoot.SetParent(transform, false);
            weaponRoot.localPosition = new Vector3(0.38f, 1.28f, 0.58f);
            weaponRoot.localRotation = Quaternion.Euler(0f, 0f, 0f);

            var metal = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            metal.color = new Color(0.11f, 0.12f, 0.12f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "RifleBody";
            body.transform.SetParent(weaponRoot, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.16f, 0.16f, 0.78f);
            body.GetComponent<Renderer>().sharedMaterial = metal;
            Destroy(body.GetComponent<Collider>());

            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrel.name = "RifleBarrel";
            barrel.transform.SetParent(weaponRoot, false);
            barrel.transform.localPosition = new Vector3(0f, 0.03f, 0.52f);
            barrel.transform.localScale = new Vector3(0.07f, 0.07f, 0.55f);
            barrel.GetComponent<Renderer>().sharedMaterial = metal;
            Destroy(barrel.GetComponent<Collider>());
        }

        private void OnGUI()
        {
            var cx = Screen.width * 0.5f;
            var cy = Screen.height * 0.5f;
            GUI.color = new Color(0.85f, 0.95f, 1f, 0.85f);
            GUI.DrawTexture(new Rect(cx - 8f, cy - 1f, 16f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 1f, cy - 8f, 2f, 16f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
