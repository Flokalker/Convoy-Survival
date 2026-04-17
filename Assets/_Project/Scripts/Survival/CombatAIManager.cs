using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CombatAIManager : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private ZombieAI[] zombieVariantPrefabs;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField, Min(0f)] private float initialDelay = 8f;
    [SerializeField, Min(2f)] private float timeBetweenWaves = 18f;
    [SerializeField, Min(1)] private int baseWaveSize = 5;
    [SerializeField, Min(0)] private int waveGrowth = 2;
    [SerializeField, Min(0.1f)] private float spawnInterval = 0.35f;

    [Header("Runtime")]
    [SerializeField] private int currentWave = 0;
    [SerializeField] private int aliveCount = 0;

    private readonly List<ZombieAI> aliveZombies = new List<ZombieAI>();
    private readonly Dictionary<Health, System.Action> deathHandlers = new Dictionary<Health, System.Action>();
    private Coroutine waveLoopRoutine;

    public int CurrentWave => currentWave;
    public int AliveCount => aliveCount;

    public void Configure(ZombieAI[] variants, Transform[] points)
    {
        zombieVariantPrefabs = variants;
        spawnPoints = points;
    }

    public void RestartWaveLoop()
    {
        if (waveLoopRoutine != null)
        {
            StopCoroutine(waveLoopRoutine);
        }

        waveLoopRoutine = StartCoroutine(WaveLoop());
    }

    public void SpawnMixedWaveNow(int count)
    {
        if (count <= 0)
        {
            return;
        }

        StartCoroutine(SpawnWave(count));
    }

    public void SpawnVariantWaveNow(ZombieAI.ZombieVariant variant, int count)
    {
        if (count <= 0)
        {
            return;
        }

        ZombieAI variantPrefab = GetPrefabForVariant(variant);
        if (variantPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            return;
        }

        StartCoroutine(SpawnVariantRoutine(variantPrefab, count));
    }

    private void OnEnable()
    {
        RestartWaveLoop();
    }

    private void OnDisable()
    {
        if (waveLoopRoutine != null)
        {
            StopCoroutine(waveLoopRoutine);
            waveLoopRoutine = null;
        }
    }

    private IEnumerator WaveLoop()
    {
        if (initialDelay > 0f)
        {
            yield return new WaitForSeconds(initialDelay);
        }

        while (enabled)
        {
            currentWave++;
            int count = baseWaveSize + (currentWave - 1) * waveGrowth;
            yield return StartCoroutine(SpawnWave(count));
            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    private IEnumerator SpawnWave(int count)
    {
        if (zombieVariantPrefabs == null || zombieVariantPrefabs.Length == 0 || spawnPoints == null || spawnPoints.Length == 0)
        {
            yield break;
        }

        for (int i = 0; i < count; i++)
        {
            ZombieAI prefab = zombieVariantPrefabs[Random.Range(0, zombieVariantPrefabs.Length)];
            Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            if (prefab == null || point == null)
            {
                continue;
            }

            ZombieAI zombie = Instantiate(prefab, point.position, point.rotation);
            if (zombie != null && !zombie.gameObject.activeSelf)
            {
                zombie.gameObject.SetActive(true);
            }
            RegisterZombie(zombie);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private IEnumerator SpawnVariantRoutine(ZombieAI prefab, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            if (point == null)
            {
                continue;
            }

            ZombieAI zombie = Instantiate(prefab, point.position, point.rotation);
            if (zombie != null && !zombie.gameObject.activeSelf)
            {
                zombie.gameObject.SetActive(true);
            }
            RegisterZombie(zombie);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private ZombieAI GetPrefabForVariant(ZombieAI.ZombieVariant variant)
    {
        if (zombieVariantPrefabs == null)
        {
            return null;
        }

        for (int i = 0; i < zombieVariantPrefabs.Length; i++)
        {
            ZombieAI prefab = zombieVariantPrefabs[i];
            if (prefab != null && prefab.Variant == variant)
            {
                return prefab;
            }
        }

        return zombieVariantPrefabs.Length > 0 ? zombieVariantPrefabs[0] : null;
    }

    private void RegisterZombie(ZombieAI zombie)
    {
        if (zombie == null)
        {
            return;
        }

        aliveZombies.Add(zombie);
        aliveCount = aliveZombies.Count;

        Health health = zombie.GetComponent<Health>();
        if (health != null)
        {
            System.Action handler = () => UnregisterZombie(zombie, health);
            deathHandlers[health] = handler;
            health.Died += handler;
        }
    }

    private void UnregisterZombie(ZombieAI zombie, Health health)
    {
        if (health != null && deathHandlers.TryGetValue(health, out System.Action handler))
        {
            health.Died -= handler;
            deathHandlers.Remove(health);
        }

        aliveZombies.Remove(zombie);
        aliveCount = aliveZombies.Count;
    }
}
