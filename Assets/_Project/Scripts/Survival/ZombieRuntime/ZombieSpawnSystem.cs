using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class ZombieSpawnSystem : MonoBehaviour
{
    [Header("Asset Discovery")]
    [SerializeField] private string[] zombieAssetFolders =
    {
        "Assets/Zombies",
        "Assets/Skeleton",
        "Assets/Polytope Studio/Lowpoly_Characters"
    };

    [Header("Spawn Control")]
    [SerializeField] private bool useExplicitSpawnPoints = false;
    [SerializeField] private Transform[] explicitSpawnPoints;
    [SerializeField] private bool preferNavMeshSpawn = true;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField, Min(3f)] private float spawnRadius = 40f;
    [SerializeField, Min(0f)] private float minSpawnDistanceFromPlayer = 14f;
    [SerializeField, Min(1f)] private float maxSpawnDistanceFromPlayer = 45f;
    [SerializeField, Min(0.1f)] private float minSpawnDelay = 0.7f;
    [SerializeField, Min(0.2f)] private float maxSpawnDelay = 2.6f;
    [SerializeField, Min(1)] private int maxAliveZombies = 20;
    [SerializeField, Min(1)] private int initialSpawnCount = 6;
    [SerializeField, Min(5f)] private float despawnDistance = 120f;

    [Header("Zombie Variation")]
    [SerializeField, Min(1f)] private float baseHealth = 65f;
    [SerializeField] private Vector2 healthVariation = new Vector2(0.85f, 1.3f);
    [SerializeField] private Vector2 speedVariation = new Vector2(0.9f, 1.25f);
    [SerializeField] private Vector2 attackVariation = new Vector2(0.85f, 1.2f);

    [Header("AI Defaults")]
    [SerializeField, Min(1f)] private float detectionRange = 20f;
    [SerializeField, Min(0.4f)] private float attackRange = 1.6f;
    [SerializeField, Min(0.1f)] private float attackCooldown = 1.05f;
    [SerializeField, Min(1f)] private float attackDamage = 11f;
    [SerializeField, Min(0.5f)] private float wanderRadius = 10f;
    [SerializeField, Min(0.1f)] private float wanderInterval = 3.2f;
    [SerializeField, Min(0.1f)] private float moveSpeed = 3.4f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly List<ZombieAgent> aliveZombies = new List<ZombieAgent>();
    private readonly List<ZombieVisualTemplate> loadedTemplates = new List<ZombieVisualTemplate>();

    private Transform playerTarget;
    private Coroutine loopRoutine;
    private bool navMeshAvailable;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindAnyObjectByType<ZombieSpawnSystem>() != null)
        {
            return;
        }

        GameObject host = new GameObject("ZombieSpawnSystem");
        host.AddComponent<ZombieSpawnSystem>();
    }

    private void Awake()
    {
        DisableLegacySystems();

        playerTarget = ResolvePlayerTarget();
        if (playerTarget == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning("ZombieSpawnSystem: player target not found. Zombie spawning disabled.");
            }

            enabled = false;
            return;
        }

        LoadZombieTemplates();
        if (loadedTemplates.Count == 0)
        {
            if (debugLogs)
            {
                Debug.LogWarning("ZombieSpawnSystem: no zombie assets discovered. Add zombie models/prefabs under configured folders.");
            }

            enabled = false;
            return;
        }

        navMeshAvailable = NavMesh.SamplePosition(playerTarget.position, out _, 15f, NavMesh.AllAreas);
        if (debugLogs)
        {
            Debug.Log($"ZombieSpawnSystem: navmesh available = {navMeshAvailable}");
        }

        for (int i = 0; i < initialSpawnCount; i++)
        {
            TrySpawnOne();
        }

        loopRoutine = StartCoroutine(SpawnLoop());
        if (debugLogs)
        {
            Debug.Log($"ZombieSpawnSystem: loaded {loadedTemplates.Count} zombie templates and started spawning.");
        }
    }

    private void DisableLegacySystems()
    {
        CombatAIManager combatManager = FindAnyObjectByType<CombatAIManager>();
        if (combatManager != null)
        {
            combatManager.enabled = false;
        }

        ZombieWaveSystem waveSystem = FindAnyObjectByType<ZombieWaveSystem>();
        if (waveSystem != null)
        {
            waveSystem.enabled = false;
        }

        CombatSceneAutoSetup legacySetup = FindAnyObjectByType<CombatSceneAutoSetup>();
        if (legacySetup != null)
        {
            Destroy(legacySetup.gameObject);
        }
    }

    private void Update()
    {
        if (playerTarget == null)
        {
            playerTarget = ResolvePlayerTarget();
        }

        CleanupDeadEntries();
        DespawnFarZombies();
    }

    private void OnDisable()
    {
        if (loopRoutine != null)
        {
            StopCoroutine(loopRoutine);
            loopRoutine = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (enabled)
        {
            float delay = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSeconds(delay);
            TrySpawnOne();
        }
    }

    private void TrySpawnOne()
    {
        if (playerTarget == null || loadedTemplates.Count == 0)
        {
            return;
        }

        CleanupDeadEntries();
        if (aliveZombies.Count >= maxAliveZombies)
        {
            return;
        }

        if (!TryFindSpawnPosition(out Vector3 spawnPos))
        {
            return;
        }

        ZombieVisualTemplate template = loadedTemplates[Random.Range(0, loadedTemplates.Count)];
        ZombieAgent zombie = BuildZombieFromTemplate(template, spawnPos);
        if (zombie != null)
        {
            aliveZombies.Add(zombie);
            zombie.Died += HandleZombieDied;
            if (debugLogs)
            {
                Debug.Log($"ZombieSpawnSystem: spawned '{template.displayName}' at {spawnPos}");
            }
        }
    }

    private ZombieAgent BuildZombieFromTemplate(ZombieVisualTemplate template, Vector3 spawnPos)
    {
        GameObject root = new GameObject($"Zombie_{template.displayName}");
        root.transform.position = spawnPos;
        root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        root.layer = LayerMask.NameToLayer("Default");

        CapsuleCollider body = root.AddComponent<CapsuleCollider>();
        body.center = new Vector3(0f, 0.95f, 0f);
        body.height = 1.9f;
        body.radius = 0.35f;

        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.mass = 65f;
        rb.linearDamping = 0.15f;
        rb.angularDamping = 2f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
        agent.speed = moveSpeed * Random.Range(speedVariation.x, speedVariation.y);
        agent.angularSpeed = 260f;
        agent.acceleration = 24f;
        agent.stoppingDistance = attackRange * 0.8f;
        agent.radius = 0.42f;
        agent.autoBraking = true;
        if (navMeshAvailable)
        {
            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit navHit, 4f, NavMesh.AllAreas))
            {
                agent.Warp(navHit.position);
            }
        }
        else
        {
            agent.enabled = false;
        }

        GameObject visual = Instantiate(template.prefab, root.transform);
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        FixUnsupportedMaterials(visual);
        AlignVisualToGround(visual.transform, root.transform.position.y);
        RemoveVisualPhysics(visual);

        Animator visualAnimator = visual.GetComponentInChildren<Animator>(true);
        if (visualAnimator == null)
        {
            visualAnimator = visual.AddComponent<Animator>();
        }

        ZombieAnimationMachine animationMachine = root.AddComponent<ZombieAnimationMachine>();
        animationMachine.ConfigureRuntime(visualAnimator, template.idleClip, template.walkClip, template.runClip, template.attackClip, template.deathClip);

        ZombieAgent zombie = root.AddComponent<ZombieAgent>();
        zombie.ConfigureRuntime(
            playerTarget,
            animationMachine,
            baseHealth * Random.Range(healthVariation.x, healthVariation.y),
            detectionRange,
            attackRange,
            attackCooldown * Random.Range(attackVariation.x, attackVariation.y),
            attackDamage * Random.Range(attackVariation.x, attackVariation.y),
            wanderRadius,
            wanderInterval,
            despawnDistance,
            debugLogs);

        return zombie;
    }

    private bool TryFindSpawnPosition(out Vector3 spawnPos)
    {
        spawnPos = Vector3.zero;
        const int maxAttempts = 26;

        float minDist = Mathf.Max(0f, minSpawnDistanceFromPlayer);
        float maxDist = Mathf.Max(minDist + 1f, Mathf.Max(maxSpawnDistanceFromPlayer, spawnRadius));

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector3 candidate;
            if (useExplicitSpawnPoints && explicitSpawnPoints != null && explicitSpawnPoints.Length > 0)
            {
                candidate = explicitSpawnPoints[Random.Range(0, explicitSpawnPoints.Length)].position;
            }
            else
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float spawnDistance = Random.Range(minDist, maxDist);
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spawnDistance;
                candidate = playerTarget.position + offset;
            }

            candidate.y = playerTarget.position.y + 8f;

            Vector3 finalPos = candidate;

            if (preferNavMeshSpawn && navMeshAvailable && NavMesh.SamplePosition(candidate, out NavMeshHit hit, 14f, NavMesh.AllAreas))
            {
                finalPos = hit.position;
            }
            else if (Physics.Raycast(candidate, Vector3.down, out RaycastHit groundHit, 120f, groundMask, QueryTriggerInteraction.Ignore))
            {
                finalPos = groundHit.point;
            }
            else
            {
                continue;
            }

            float distanceToPlayer = Vector3.Distance(finalPos, playerTarget.position);
            if (distanceToPlayer < minSpawnDistanceFromPlayer || distanceToPlayer > maxSpawnDistanceFromPlayer)
            {
                continue;
            }

            spawnPos = finalPos;
            return true;
        }

        // Fallback: force a nearby point around player (navmesh if available, else ground raycast).
        Vector3 fallback = playerTarget.position + playerTarget.right * Mathf.Max(10f, minDist + 2f) + Vector3.up * 8f;
        if (navMeshAvailable && NavMesh.SamplePosition(fallback, out NavMeshHit fallbackHit, 20f, NavMesh.AllAreas))
        {
            spawnPos = fallbackHit.position;
            return true;
        }
        if (Physics.Raycast(fallback, Vector3.down, out RaycastHit fallbackGroundHit, 120f, groundMask, QueryTriggerInteraction.Ignore))
        {
            spawnPos = fallbackGroundHit.point;
            return true;
        }

        if (debugLogs)
        {
            Debug.LogWarning("ZombieSpawnSystem: failed to find spawn position this cycle.");
        }

        return false;
    }

    private void CleanupDeadEntries()
    {
        for (int i = aliveZombies.Count - 1; i >= 0; i--)
        {
            if (aliveZombies[i] == null)
            {
                aliveZombies.RemoveAt(i);
            }
        }
    }

    private void DespawnFarZombies()
    {
        if (playerTarget == null)
        {
            return;
        }

        for (int i = aliveZombies.Count - 1; i >= 0; i--)
        {
            ZombieAgent zombie = aliveZombies[i];
            if (zombie == null)
            {
                aliveZombies.RemoveAt(i);
                continue;
            }

            float dist = Vector3.Distance(zombie.transform.position, playerTarget.position);
            if (dist <= despawnDistance)
            {
                continue;
            }

            Destroy(zombie.gameObject);
            aliveZombies.RemoveAt(i);
        }
    }

    private void HandleZombieDied(ZombieAgent agent)
    {
        if (agent == null)
        {
            return;
        }

        aliveZombies.Remove(agent);
    }

    private Transform ResolvePlayerTarget()
    {
        try
        {
            GameObject tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null)
            {
                return tagged.transform;
            }
        }
        catch (UnityException)
        {
            // Tag might be missing in early scenes.
        }

        PlayerStats stats = FindAnyObjectByType<PlayerStats>();
        if (stats != null)
        {
            return stats.transform;
        }

        GameObject named = GameObject.Find("Player");
        return named != null ? named.transform : null;
    }

    private void LoadZombieTemplates()
    {
        loadedTemplates.Clear();

#if UNITY_EDITOR
        HashSet<string> seenPaths = new HashSet<string>();
        for (int f = 0; f < zombieAssetFolders.Length; f++)
        {
            string folder = zombieAssetFolders[f];
            if (string.IsNullOrWhiteSpace(folder) || !AssetDatabase.IsValidFolder(folder))
            {
                continue;
            }

            string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { folder });
            for (int g = 0; g < guids.Length; g++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[g]);
                if (string.IsNullOrWhiteSpace(path) || seenPaths.Contains(path))
                {
                    continue;
                }

                GameObject prefab = AssetDatabase.LoadMainAssetAtPath(path) as GameObject;
                if (prefab == null || !IsLikelyZombieAsset(path, prefab))
                {
                    continue;
                }

                seenPaths.Add(path);
                ZombieVisualTemplate template = new ZombieVisualTemplate();
                template.prefab = prefab;
                template.displayName = prefab.name;
                AssignBestClips(path, template);
                loadedTemplates.Add(template);
            }
        }
#endif
    }

    private static bool IsLikelyZombieAsset(string path, GameObject prefab)
    {
        if (prefab == null || string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        string lower = path.ToLowerInvariant().Replace('\\', '/');
        bool hasRenderer = prefab.GetComponentInChildren<Renderer>(true) != null;
        if (!hasRenderer)
        {
            return false;
        }

        if (lower.Contains("/weapons/") || lower.Contains("/weapon/") || lower.Contains("/props/") || lower.Contains("/environment/"))
        {
            return false;
        }

        return lower.Contains("/zombies/") || lower.Contains("/skeleton/") || lower.Contains("zombie") || lower.Contains("undead");
    }

    private static void AssignBestClips(string assetPath, ZombieVisualTemplate template)
    {
#if UNITY_EDITOR
        Object[] all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        List<AnimationClip> clips = new List<AnimationClip>();
        for (int i = 0; i < all.Length; i++)
        {
            AnimationClip clip = all[i] as AnimationClip;
            if (clip == null)
            {
                continue;
            }

            string name = clip.name.ToLowerInvariant();
            if (name.StartsWith("__preview__"))
            {
                continue;
            }

            clips.Add(clip);
        }

        if (clips.Count == 0 && template.prefab != null)
        {
            Animator prefabAnimator = template.prefab.GetComponentInChildren<Animator>(true);
            if (prefabAnimator != null && prefabAnimator.runtimeAnimatorController != null)
            {
                AnimationClip[] ctrlClips = prefabAnimator.runtimeAnimatorController.animationClips;
                for (int i = 0; i < ctrlClips.Length; i++)
                {
                    if (ctrlClips[i] != null)
                    {
                        clips.Add(ctrlClips[i]);
                    }
                }
            }
        }

        template.idleClip = FindByToken(clips, "idle");
        template.walkClip = FindByToken(clips, "walk");
        template.runClip = FindByToken(clips, "run");
        template.attackClip = FindByToken(clips, "attack");
        template.deathClip = FindByToken(clips, "death", "die");

        if (template.walkClip == null && clips.Count > 0)
        {
            template.walkClip = clips[0];
        }
        if (template.idleClip == null)
        {
            template.idleClip = template.walkClip;
        }
        if (template.runClip == null)
        {
            template.runClip = template.walkClip;
        }
        if (template.attackClip == null)
        {
            template.attackClip = template.walkClip;
        }
        if (template.deathClip == null)
        {
            template.deathClip = template.idleClip;
        }
#endif
    }

    private static AnimationClip FindByToken(List<AnimationClip> clips, params string[] tokens)
    {
        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];
            for (int c = 0; c < clips.Count; c++)
            {
                AnimationClip clip = clips[c];
                if (clip != null && clip.name.ToLowerInvariant().Contains(token))
                {
                    return clip;
                }
            }
        }

        return null;
    }

    private static void RemoveVisualPhysics(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Destroy(colliders[i]);
        }

        Rigidbody[] bodies = root.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < bodies.Length; i++)
        {
            Destroy(bodies[i]);
        }
    }

    private static void FixUnsupportedMaterials(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Shader fallback = Shader.Find("Universal Render Pipeline/Lit");
        if (fallback == null)
        {
            fallback = Shader.Find("Standard");
        }

        if (fallback == null)
        {
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int r = 0; r < renderers.Length; r++)
        {
            Renderer renderer = renderers[r];
            if (renderer == null)
            {
                continue;
            }

            Material[] source = renderer.sharedMaterials;
            if (source == null || source.Length == 0)
            {
                continue;
            }

            bool changed = false;
            Material[] fixedMats = new Material[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                Material mat = source[i];
                if (mat == null)
                {
                    fixedMats[i] = mat;
                    continue;
                }

                bool broken = mat.shader == null ||
                              !mat.shader.isSupported ||
                              mat.shader.name == "Hidden/InternalErrorShader";

                if (!broken)
                {
                    fixedMats[i] = mat;
                    continue;
                }

                Material replacement = new Material(fallback);
                replacement.name = mat.name + "_RuntimeFixed";

                if (mat.HasProperty("_BaseColor") && replacement.HasProperty("_BaseColor"))
                {
                    replacement.SetColor("_BaseColor", mat.GetColor("_BaseColor"));
                }
                else if (mat.HasProperty("_Color") && replacement.HasProperty("_Color"))
                {
                    replacement.SetColor("_Color", mat.GetColor("_Color"));
                }

                Texture baseTex = null;
                if (mat.HasProperty("_BaseMap"))
                {
                    baseTex = mat.GetTexture("_BaseMap");
                }
                if (baseTex == null && mat.HasProperty("_MainTex"))
                {
                    baseTex = mat.GetTexture("_MainTex");
                }

                if (baseTex != null)
                {
                    if (replacement.HasProperty("_BaseMap"))
                    {
                        replacement.SetTexture("_BaseMap", baseTex);
                    }
                    if (replacement.HasProperty("_MainTex"))
                    {
                        replacement.SetTexture("_MainTex", baseTex);
                    }
                }

                if (mat.HasProperty("_BumpMap") && replacement.HasProperty("_BumpMap"))
                {
                    replacement.SetTexture("_BumpMap", mat.GetTexture("_BumpMap"));
                }

                fixedMats[i] = replacement;
                changed = true;
            }

            if (changed)
            {
                renderer.sharedMaterials = fixedMats;
            }
        }
    }

    private static void AlignVisualToGround(Transform visualRoot, float targetGroundY)
    {
        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float yOffset = bounds.min.y - targetGroundY;
        visualRoot.position -= new Vector3(0f, yOffset, 0f);
    }

    private class ZombieVisualTemplate
    {
        public string displayName;
        public GameObject prefab;
        public AnimationClip idleClip;
        public AnimationClip walkClip;
        public AnimationClip runClip;
        public AnimationClip attackClip;
        public AnimationClip deathClip;
    }
}
