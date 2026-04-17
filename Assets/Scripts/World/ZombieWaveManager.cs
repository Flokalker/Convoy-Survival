using System.Collections.Generic;
using UnityEngine;

namespace PostApocRoadtrip.World
{
    public class ZombieWaveManager : MonoBehaviour
    {
        public float timeBetweenWaves = 2.4f;
        public int baseWaveSize = 22;
        public int maxWaveSize = 70;
        public int minimumPoolSize = 95;
        public float spawnRadius = 40f;
        public float forwardSpawnBias = 26f;

        private readonly List<ZombieEnemy> zombies = new();
        private readonly List<Vector3> spawnAnchors = new();
        private Transform target;
        private VehicleHealth targetHealth;
        private float nextWaveTime;

        public int CurrentWave { get; private set; }
        public int ActiveZombies { get; private set; }

        private void Start()
        {
            targetHealth = FindObjectOfType<VehicleHealth>();
            target = targetHealth != null ? targetHealth.transform : null;

            zombies.AddRange(FindObjectsOfType<ZombieEnemy>(true));
            for (var i = 0; i < zombies.Count; i++)
            {
                spawnAnchors.Add(zombies[i].transform.position);
                zombies[i].gameObject.SetActive(false);
            }

            ExpandZombiePool();

            nextWaveTime = Time.time + 1.2f;
        }

        private void Update()
        {
            if (target == null || targetHealth == null || targetHealth.IsDestroyed || zombies.Count == 0)
            {
                return;
            }

            if (ActiveZombies <= 0 && Time.time >= nextWaveTime)
            {
                StartNextWave();
            }
        }

        public void NotifyZombieKilled(ZombieEnemy zombie)
        {
            ActiveZombies = Mathf.Max(0, ActiveZombies - 1);
            if (ActiveZombies == 0)
            {
                nextWaveTime = Time.time + timeBetweenWaves;
            }
        }

        private void StartNextWave()
        {
            CurrentWave++;
            var waveSize = Mathf.Min(maxWaveSize, zombies.Count, baseWaveSize + CurrentWave * 5);
            ActiveZombies = 0;

            for (var i = 0; i < waveSize; i++)
            {
                var zombie = zombies[i];
                var anchor = spawnAnchors[(i + CurrentWave * 2) % spawnAnchors.Count];
                var targetForward = target != null ? target.forward : Vector3.forward;
                var targetRight = target != null ? target.right : Vector3.right;
                var side = Random.Range(-22f, 22f);
                var forward = Random.Range(12f, spawnRadius) + forwardSpawnBias;
                var biasedPosition = target.position + targetForward * forward + targetRight * side;
                var spawnPosition = i % 3 == 0
                    ? new Vector3(anchor.x + Random.Range(-12f, 12f), anchor.y, anchor.z + Random.Range(-12f, 12f))
                    : new Vector3(biasedPosition.x, anchor.y, biasedPosition.z);
                zombie.Activate(target, targetHealth, this, spawnPosition);
                ActiveZombies++;
            }
        }

        private void ExpandZombiePool()
        {
            if (zombies.Count == 0)
            {
                return;
            }

            var template = zombies[0];
            var parent = template.transform.parent;
            var safety = 0;
            while (zombies.Count < minimumPoolSize && safety < minimumPoolSize * 2)
            {
                safety++;
                var clone = Instantiate(template.gameObject, parent);
                clone.name = $"GeneratedWaveZombie_{zombies.Count:000}";
                var enemy = clone.GetComponent<ZombieEnemy>();
                enemy.moveSpeed = Random.Range(2.6f, 4.1f);
                enemy.chaseRange = 180f;
                enemy.shootRange = Random.Range(18f, 28f);
                enemy.fireInterval = Random.Range(0.95f, 1.45f);
                enemy.shotDamage = Random.Range(3.5f, 6.5f);
                zombies.Add(enemy);
                spawnAnchors.Add(spawnAnchors[zombies.Count % spawnAnchors.Count] + new Vector3(Random.Range(-30f, 30f), 0f, Random.Range(-40f, 40f)));
                clone.SetActive(false);
            }
        }
    }
}
