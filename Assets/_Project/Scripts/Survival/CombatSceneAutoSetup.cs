using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class CombatSceneAutoSetup : MonoBehaviour
{
    [Header("Scene Scope")]
    [SerializeField] private string targetSceneName = "BrowserPrototype";
    [SerializeField] private bool onlyWhenNoManagerExists = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInstallInLoadedScene()
    {
        if (FindObjectOfType<CombatSceneAutoSetup>() != null)
        {
            return;
        }

        GameObject installer = new GameObject("CombatSceneAutoSetup");
        installer.AddComponent<CombatSceneAutoSetup>();
    }

    private void Awake()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!string.IsNullOrWhiteSpace(targetSceneName) && activeScene.name != targetSceneName)
        {
            Destroy(gameObject);
            return;
        }

        CombatAIManager existingManager = FindObjectOfType<CombatAIManager>();
        if (onlyWhenNoManagerExists && existingManager != null)
        {
            EnsurePlayerStats();
            Destroy(gameObject);
            return;
        }

        Transform player = FindPlayerTransform();
        if (player == null)
        {
            Debug.LogWarning("CombatSceneAutoSetup: Could not find a player transform. Skipping wave setup.", this);
            Destroy(gameObject);
            return;
        }

        EnsurePlayerStats();

        CombatAIManager manager = existingManager != null
            ? existingManager
            : new GameObject("CombatAIManager").AddComponent<CombatAIManager>();

        GeneratedCombatAssets assets = BuildRuntimeAssets(player.position);
        manager.Configure(assets.zombieVariants, assets.spawnPoints);
        manager.RestartWaveLoop();

        Destroy(gameObject);
    }

    private static Transform FindPlayerTransform()
    {
        GameObject named = GameObject.Find("Player");
        if (named != null)
        {
            return named.transform;
        }

        try
        {
            GameObject tagged = GameObject.FindGameObjectWithTag("Player");
            return tagged != null ? tagged.transform : null;
        }
        catch (UnityException)
        {
            return null;
        }
    }

    private static void EnsurePlayerStats()
    {
        Transform player = FindPlayerTransform();
        if (player == null)
        {
            return;
        }

        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (stats == null)
        {
            stats = player.gameObject.AddComponent<PlayerStats>();
        }

        // Keep prototype combat loop alive after death (show game over, no full scene reset).
        stats.SetResetSceneOnDeath(false);
    }

    private struct GeneratedCombatAssets
    {
        public ZombieAI[] zombieVariants;
        public Transform[] spawnPoints;
    }

    private static GeneratedCombatAssets BuildRuntimeAssets(Vector3 center)
    {
        Transform root = new GameObject("__GeneratedCombatTemplates").transform;
        root.gameObject.SetActive(false);

        GameObject spitProjectile = CreateSpitProjectileTemplate(root);

        ZombieAI runner = CreateZombieTemplate(root, "Zombie_Runner_Template", ZombieAI.ZombieVariant.Runner, 5.2f);
        ZombieAI tank = CreateZombieTemplate(root, "Zombie_Tank_Template", ZombieAI.ZombieVariant.Tank, 2f);
        ZombieAI spitter = CreateZombieTemplate(root, "Zombie_Spitter_Template", ZombieAI.ZombieVariant.Spitter, 2.8f);

        Transform spitterMouth = new GameObject("SpitOrigin").transform;
        spitterMouth.SetParent(spitter.transform, false);
        spitterMouth.localPosition = new Vector3(0f, 1.4f, 0.5f);
        spitter.ConfigureRuntimeVariant(ZombieAI.ZombieVariant.Spitter, spitProjectile, spitterMouth);

        Transform spawnsRoot = new GameObject("ZombieSpawnPoints").transform;
        CreateSpawnPoint(spawnsRoot, center + new Vector3(22f, 0f, 22f), "Spawn_A");
        CreateSpawnPoint(spawnsRoot, center + new Vector3(-22f, 0f, 18f), "Spawn_B");
        CreateSpawnPoint(spawnsRoot, center + new Vector3(16f, 0f, -24f), "Spawn_C");
        CreateSpawnPoint(spawnsRoot, center + new Vector3(-18f, 0f, -20f), "Spawn_D");

        Transform[] points = new Transform[spawnsRoot.childCount];
        for (int i = 0; i < spawnsRoot.childCount; i++)
        {
            points[i] = spawnsRoot.GetChild(i);
        }

        return new GeneratedCombatAssets
        {
            zombieVariants = new[] { runner, tank, spitter },
            spawnPoints = points
        };
    }

    private static ZombieAI CreateZombieTemplate(Transform parent, string name, ZombieAI.ZombieVariant variant, float speed)
    {
        GameObject zombie = new GameObject(name);
        zombie.name = name;
        zombie.transform.SetParent(parent, false);
        zombie.transform.localScale = new Vector3(1f, 1f, 1f);
        try
        {
            zombie.tag = "Zombie";
        }
        catch (UnityException)
        {
            // Ignore if tag was not created yet.
        }

        CapsuleCollider collider = zombie.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 0.95f, 0f);
        collider.height = 1.9f;
        collider.radius = 0.35f;

        AttachZombieVisual(zombie.transform);

        NavMeshAgent agent = zombie.AddComponent<NavMeshAgent>();
        agent.speed = speed;
        agent.angularSpeed = 240f;
        agent.acceleration = 18f;
        agent.stoppingDistance = 1.2f;
        agent.radius = 0.45f;

        Health health = zombie.AddComponent<Health>();
        health.SetMaxHealth(100f, true);

        ZombieAI ai = zombie.AddComponent<ZombieAI>();
        ai.ConfigureRuntimeVariant(variant);

        return ai;
    }

    private static void AttachZombieVisual(Transform zombieRoot)
    {
        GameObject visualPrefab = LoadZombieVisualPrefab();
        if (visualPrefab != null)
        {
            GameObject visual = Instantiate(visualPrefab, zombieRoot);
            visual.name = "ZombieVisual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            // Remove colliders on imported model so root capsule handles gameplay collisions.
            Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Object.Destroy(colliders[i]);
            }

            return;
        }

        GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        fallback.name = "ZombieVisual_Fallback";
        fallback.transform.SetParent(zombieRoot, false);
        fallback.transform.localPosition = new Vector3(0f, 1f, 0f);
        fallback.transform.localRotation = Quaternion.identity;
        fallback.transform.localScale = new Vector3(0.7f, 1f, 0.7f);

        Collider fallbackCollider = fallback.GetComponent<Collider>();
        if (fallbackCollider != null)
        {
            Object.Destroy(fallbackCollider);
        }
    }

    private static GameObject LoadZombieVisualPrefab()
    {
        GameObject resourceModel = Resources.Load<GameObject>("Zombies/Zombie Attack-4");
        if (resourceModel != null)
        {
            return resourceModel;
        }

#if UNITY_EDITOR
        const string modelPath = "Assets/Zombies/Zombie Attack-4.fbx";
        return AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
#else
        return null;
#endif
    }

    private static GameObject CreateSpitProjectileTemplate(Transform parent)
    {
        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = "SpitProjectile_Template";
        projectile.transform.SetParent(parent, false);
        projectile.transform.localScale = Vector3.one * 0.25f;
        projectile.AddComponent<ZombieProjectile>();
        return projectile;
    }

    private static void CreateSpawnPoint(Transform parent, Vector3 position, string pointName)
    {
        GameObject point = new GameObject(pointName);
        point.transform.SetParent(parent);
        point.transform.position = position;
    }
}
