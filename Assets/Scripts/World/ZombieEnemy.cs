using UnityEngine;

namespace PostApocRoadtrip.World
{
    public class ZombieEnemy : MonoBehaviour
    {
        public float maxHealth = 25f;
        public float moveSpeed = 3.2f;
        public float chaseRange = 180f;
        public float shootRange = 22f;
        public float shotDamage = 5f;
        public float fireInterval = 1.45f;
        public float ramKillSpeed = 7f;

        private Transform target;
        private VehicleHealth targetHealth;
        private ZombieWaveManager waveManager;
        private Material tracerMaterial;
        private float currentHealth;
        private float nextShotTime;
        private bool alive;

        public bool IsAlive => alive;

        private void Awake()
        {
            currentHealth = maxHealth;
            alive = true;
        }

        public void Activate(Transform newTarget, VehicleHealth newTargetHealth, ZombieWaveManager manager, Vector3 spawnPosition)
        {
            target = newTarget;
            targetHealth = newTargetHealth;
            waveManager = manager;
            transform.position = spawnPosition;
            currentHealth = maxHealth;
            alive = true;
            nextShotTime = Time.time + Random.Range(0.2f, 1.1f);
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!alive || target == null || targetHealth == null || targetHealth.IsDestroyed)
            {
                return;
            }

            var toTarget = target.position - transform.position;
            var activeSurvivor = FindObjectOfType<FirstPersonSurvivorController>();
            if (activeSurvivor != null && activeSurvivor.gameObject.activeInHierarchy)
            {
                toTarget = activeSurvivor.transform.position - transform.position;
            }

            toTarget.y = 0f;
            var distance = toTarget.magnitude;
            if (distance > chaseRange || distance <= 0.05f)
            {
                return;
            }

            var lookRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 6f);

            if (distance > shootRange)
            {
                transform.position += toTarget.normalized * (moveSpeed * Time.deltaTime);
                return;
            }

            TryShoot();
        }

        public void TakeRamHit(float vehicleSpeed)
        {
            if (!alive)
            {
                return;
            }

            currentHealth -= vehicleSpeed >= ramKillSpeed ? maxHealth : Mathf.Max(4f, vehicleSpeed * 1.5f);
            if (currentHealth <= 0f)
            {
                Kill();
            }
        }

        public void TakeBulletDamage(float damage)
        {
            if (!alive || damage <= 0f)
            {
                return;
            }

            currentHealth -= damage;
            if (currentHealth <= 0f)
            {
                Kill();
            }
        }

        private void TryShoot()
        {
            if (Time.time < nextShotTime)
            {
                return;
            }

            nextShotTime = Time.time + fireInterval + Random.Range(-0.18f, 0.28f);
            if (HasLineOfSight())
            {
                SpawnShotTracer(transform.position + Vector3.up * 1.55f, target.position + Vector3.up * 0.8f);
                targetHealth.Damage(shotDamage);
            }
        }

        private bool HasLineOfSight()
        {
            var origin = transform.position + Vector3.up * 1.55f;
            var destination = target.position + Vector3.up * 0.8f;
            var direction = destination - origin;
            var distance = direction.magnitude;
            if (distance <= 0.1f)
            {
                return false;
            }

            var hits = Physics.RaycastAll(origin, direction / distance, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (hit.collider.transform.IsChildOf(target))
                {
                    return true;
                }

                return false;
            }

            return true;
        }

        private void SpawnShotTracer(Vector3 start, Vector3 end)
        {
            var direction = end - start;
            var distance = direction.magnitude;
            if (distance <= 0.1f)
            {
                return;
            }

            tracerMaterial ??= CreateTracerMaterial();
            var tracer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tracer.name = "ZombieShotTracer";
            tracer.transform.position = start + direction * 0.5f;
            tracer.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            tracer.transform.localScale = new Vector3(0.035f, 0.035f, distance);
            var renderer = tracer.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = tracerMaterial;
            }

            var collider = tracer.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Destroy(tracer, 0.08f);
        }

        private static Material CreateTracerMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader)
            {
                color = new Color(1f, 0.22f, 0.08f, 1f)
            };

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(1.6f, 0.22f, 0.05f, 1f));
            }

            return material;
        }

        private void Kill()
        {
            alive = false;
            waveManager?.NotifyZombieKilled(this);
            gameObject.SetActive(false);
        }
    }
}
