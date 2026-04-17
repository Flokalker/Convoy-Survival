using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ZombieWaveSystem : MonoBehaviour
{
    [SerializeField] private ZombieAI zombiePrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField, Min(0f)] private float initialDelay = 5f;
    [SerializeField, Min(1f)] private float timeBetweenWaves = 20f;
    [SerializeField, Min(1)] private int baseWaveSize = 4;
    [SerializeField, Min(0)] private int waveSizeGrowth = 2;
    [SerializeField, Min(0.05f)] private float spawnInterval = 0.4f;

    private readonly List<ZombieAI> aliveZombies = new List<ZombieAI>();
    private int currentWave = 0;

    private void Start()
    {
        StartCoroutine(WaveLoop());
    }

    private IEnumerator WaveLoop()
    {
        if (initialDelay > 0f)
        {
            yield return new WaitForSeconds(initialDelay);
        }

        while (enabled)
        {
            SpawnWave();
            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    private void SpawnWave()
    {
        if (zombiePrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            return;
        }

        currentWave++;
        int zombieCount = baseWaveSize + (currentWave - 1) * waveSizeGrowth;
        StartCoroutine(SpawnWaveRoutine(zombieCount));
    }

    private IEnumerator SpawnWaveRoutine(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
            ZombieAI zombie = Instantiate(zombiePrefab, spawn.position, spawn.rotation);
            aliveZombies.Add(zombie);

            Health zombieHealth = zombie.GetComponent<Health>();
            if (zombieHealth != null)
            {
                zombieHealth.Died += () => aliveZombies.Remove(zombie);
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }
}
