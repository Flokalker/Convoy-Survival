using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using System.IO;
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
        // Legacy system disabled. Replaced by ZombieSpawnSystem.
    }

    private void Awake()
    {
        Destroy(gameObject);
        return;

        Scene activeScene = SceneManager.GetActiveScene();
        if (!string.IsNullOrWhiteSpace(targetSceneName) && activeScene.name != targetSceneName)
        {
            Destroy(gameObject);
            return;
        }

        CombatAIManager existingManager = FindObjectOfType<CombatAIManager>();

        Transform player = FindPlayerTransform();
        if (player == null)
        {
            Debug.LogWarning("CombatSceneAutoSetup: Could not find a player transform. Skipping wave setup.", this);
            Destroy(gameObject);
            return;
        }

        EnsurePlayerStats();

        CombatAIManager manager = existingManager;
        if (manager == null)
        {
            manager = new GameObject("CombatAIManager").AddComponent<CombatAIManager>();
        }
        else if (!onlyWhenNoManagerExists)
        {
            // Explicitly allow replacing manager setup each load when requested.
            manager.StopAllCoroutines();
        }

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
        root.gameObject.hideFlags = HideFlags.HideInHierarchy;
        root.position = new Vector3(0f, -500f, 0f);

        GameObject spitProjectile = CreateSpitProjectileTemplate(root);
        List<GameObject> zombieVisualPrefabs = LoadZombieVisualPrefabs();
        Debug.Log($"CombatSceneAutoSetup: Found {zombieVisualPrefabs.Count} zombie visual asset(s).");
        int templateCount = Mathf.Max(3, zombieVisualPrefabs.Count);
        List<ZombieAI> templates = new List<ZombieAI>(templateCount);
        ZombieSpecialMechanics.ZombieMechanic[] mechanics =
        {
            ZombieSpecialMechanics.ZombieMechanic.None,
            ZombieSpecialMechanics.ZombieMechanic.Berserker,
            ZombieSpecialMechanics.ZombieMechanic.Exploder,
            ZombieSpecialMechanics.ZombieMechanic.Leaper
        };

        for (int i = 0; i < templateCount; i++)
        {
            ZombieAI.ZombieVariant variant = (ZombieAI.ZombieVariant)(i % 3);
            float speed = GetSpeedForVariant(variant);
            GameObject visualPrefab = zombieVisualPrefabs.Count > 0 ? zombieVisualPrefabs[i % zombieVisualPrefabs.Count] : null;
            string visualName = visualPrefab != null ? visualPrefab.name : "Fallback";
            string templateName = $"Zombie_{variant}_{visualName}_{i}";
            ZombieSpecialMechanics.ZombieMechanic mechanic = mechanics[i % mechanics.Length];

            ZombieAI template = CreateZombieTemplate(root, templateName, variant, speed, visualPrefab, mechanic);
            if (variant == ZombieAI.ZombieVariant.Spitter)
            {
                Transform spitterMouth = new GameObject("SpitOrigin").transform;
                spitterMouth.SetParent(template.transform, false);
                spitterMouth.localPosition = new Vector3(0f, 1.4f, 0.5f);
                template.ConfigureRuntimeVariant(ZombieAI.ZombieVariant.Spitter, spitProjectile, spitterMouth);
            }

            templates.Add(template);
        }

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
            zombieVariants = templates.ToArray(),
            spawnPoints = points
        };
    }

    private static float GetSpeedForVariant(ZombieAI.ZombieVariant variant)
    {
        switch (variant)
        {
            case ZombieAI.ZombieVariant.Runner:
                return 5.2f;
            case ZombieAI.ZombieVariant.Tank:
                return 2f;
            case ZombieAI.ZombieVariant.Spitter:
                return 2.8f;
            default:
                return 3.5f;
        }
    }

    private static ZombieAI CreateZombieTemplate(
        Transform parent,
        string name,
        ZombieAI.ZombieVariant variant,
        float speed,
        GameObject visualPrefab,
        ZombieSpecialMechanics.ZombieMechanic mechanic)
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

        AttachZombieVisual(zombie.transform, visualPrefab);

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

        ZombieSpecialMechanics specialMechanics = zombie.AddComponent<ZombieSpecialMechanics>();
        specialMechanics.ConfigureRuntime(mechanic);

        return ai;
    }

    private static void AttachZombieVisual(Transform zombieRoot, GameObject visualPrefab)
    {
        if (visualPrefab != null)
        {
            GameObject visual = Instantiate(visualPrefab, zombieRoot);
            visual.name = "ZombieVisual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            AlignVisualToGround(visual.transform);
            FixBrokenMaterials(visual.transform);
            SetupZombieAnimation(visual, visualPrefab);

            // Remove colliders on imported model so root capsule handles gameplay collisions.
            Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                UnityEngine.Object.Destroy(colliders[i]);
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
            UnityEngine.Object.Destroy(fallbackCollider);
        }

        FixBrokenMaterials(fallback.transform);
    }

    private static List<GameObject> LoadZombieVisualPrefabs()
    {
        List<GameObject> prefabs = new List<GameObject>();

        // Runtime fallback if a model was put in a Resources/Zombies folder.
        GameObject resourceModel = Resources.Load<GameObject>("Zombies/Zombie Attack-4");
        if (resourceModel != null)
        {
            prefabs.Add(resourceModel);
        }

#if UNITY_EDITOR
        // Load only main assets from zombie model files to avoid pulling sub-objects (like single hands/bones).
        string zombiesFolder = "Assets/Zombies";
        if (AssetDatabase.IsValidFolder(zombiesFolder))
        {
            string absoluteFolder = Path.Combine(Application.dataPath, "Zombies");
            if (Directory.Exists(absoluteFolder))
            {
                string[] files = Directory.GetFiles(absoluteFolder, "*.*", SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    string extension = Path.GetExtension(files[i]).ToLowerInvariant();
                    if (extension != ".fbx" && extension != ".prefab")
                    {
                        continue;
                    }

                    string normalized = files[i].Replace('\\', '/');
                    int assetsIndex = normalized.IndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
                    if (assetsIndex < 0)
                    {
                        continue;
                    }

                    string assetPath = normalized.Substring(assetsIndex + 1);
                    GameObject prefab = AssetDatabase.LoadMainAssetAtPath(assetPath) as GameObject;
                    if (prefab != null && !prefabs.Contains(prefab) && IsLikelyZombieVisual(assetPath, prefab))
                    {
                        prefabs.Add(prefab);
                    }
                }
            }
        }

        // Fallback: named zombie assets anywhere in project, still using main assets only.
        string[] namedZombieGuids = AssetDatabase.FindAssets("zombie t:GameObject", new[] { "Assets" });
        for (int i = 0; i < namedZombieGuids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(namedZombieGuids[i]);
            GameObject prefab = AssetDatabase.LoadMainAssetAtPath(assetPath) as GameObject;
            if (prefab != null && !prefabs.Contains(prefab) && IsLikelyZombieVisual(assetPath, prefab))
            {
                prefabs.Add(prefab);
            }
        }
#endif

        return prefabs;
    }

    private static bool IsLikelyZombieVisual(string assetPath, GameObject prefab)
    {
        if (prefab == null || string.IsNullOrEmpty(assetPath))
        {
            return false;
        }

        string path = assetPath.ToLowerInvariant().Replace('\\', '/');

        // Exclusions for obvious non-character assets.
        if (path.Contains("/weapons") ||
            path.Contains("/weapon") ||
            path.Contains("/props") ||
            path.Contains("/environment") ||
            path.Contains("/vehicle") ||
            path.Contains("/village"))
        {
            return false;
        }

        // Direct includes.
        if (path.Contains("/assets/zombies/") || path.Contains("/skeleton/"))
        {
            return prefab.GetComponentInChildren<Renderer>(true) != null;
        }

        // Name-based includes.
        if (path.Contains("zombie") || path.Contains("undead") || path.Contains("walker") || path.Contains("infected"))
        {
            return prefab.GetComponentInChildren<Renderer>(true) != null;
        }

        return false;
    }

    private static void AlignVisualToGround(Transform visualRoot)
    {
        if (visualRoot == null)
        {
            return;
        }

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

        float yOffset = bounds.min.y - visualRoot.parent.position.y;
        visualRoot.position -= new Vector3(0f, yOffset, 0f);
    }

    private static void FixBrokenMaterials(Transform visualRoot)
    {
        if (visualRoot == null)
        {
            return;
        }

        Shader fallbackShader = Shader.Find("Universal Render Pipeline/Lit");
        if (fallbackShader == null)
        {
            fallbackShader = Shader.Find("Standard");
        }

        if (fallbackShader == null)
        {
            return;
        }

        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            bool changed = false;
            for (int m = 0; m < materials.Length; m++)
            {
                Material source = materials[m];
                if (source == null)
                {
                    continue;
                }

                bool shaderBroken = source.shader == null ||
                                    !source.shader.isSupported ||
                                    source.shader.name == "Hidden/InternalErrorShader";
                if (!shaderBroken)
                {
                    continue;
                }

                Material replacement = new Material(fallbackShader);
                replacement.name = $"{source.name}_AutoFixed";

                if (source.HasProperty("_BaseColor") && replacement.HasProperty("_BaseColor"))
                {
                    replacement.SetColor("_BaseColor", source.GetColor("_BaseColor"));
                }
                else if (source.HasProperty("_Color") && replacement.HasProperty("_Color"))
                {
                    replacement.SetColor("_Color", source.GetColor("_Color"));
                }

                Texture baseMap = null;
                if (source.HasProperty("_BaseMap"))
                {
                    baseMap = source.GetTexture("_BaseMap");
                }
                if (baseMap == null && source.HasProperty("_MainTex"))
                {
                    baseMap = source.GetTexture("_MainTex");
                }

                if (baseMap != null)
                {
                    if (replacement.HasProperty("_BaseMap"))
                    {
                        replacement.SetTexture("_BaseMap", baseMap);
                    }
                    if (replacement.HasProperty("_MainTex"))
                    {
                        replacement.SetTexture("_MainTex", baseMap);
                    }
                }

                materials[m] = replacement;
                changed = true;
            }

            if (changed)
            {
                renderer.sharedMaterials = materials;
            }
        }
    }

    private static void SetupZombieAnimation(GameObject visual, GameObject visualPrefab)
    {
        if (visual == null || visualPrefab == null)
        {
            return;
        }

        Animator existingAnimator = visual.GetComponentInChildren<Animator>(true);
        if (existingAnimator != null && existingAnimator.runtimeAnimatorController != null)
        {
            existingAnimator.enabled = true;
            existingAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            return;
        }

        Animation legacyAnimation = visual.GetComponentInChildren<Animation>(true);
        if (legacyAnimation != null && legacyAnimation.clip != null)
        {
            legacyAnimation.playAutomatically = true;
            legacyAnimation.wrapMode = WrapMode.Loop;
            legacyAnimation.Play();
            return;
        }

        AnimationClip clip = FindBestZombieClip(visualPrefab);
        if (clip == null)
        {
            return;
        }

        ZombieClipPlayer clipPlayer = visual.GetComponent<ZombieClipPlayer>();
        if (clipPlayer == null)
        {
            clipPlayer = visual.AddComponent<ZombieClipPlayer>();
        }

        clipPlayer.SetClip(clip);
    }

    private static AnimationClip FindBestZombieClip(GameObject visualPrefab)
    {
        if (visualPrefab == null)
        {
            return null;
        }

        // Runtime path: if an AnimatorController exists, just use its first clip.
        Animator sourceAnimator = visualPrefab.GetComponentInChildren<Animator>(true);
        if (sourceAnimator != null && sourceAnimator.runtimeAnimatorController != null)
        {
            AnimationClip[] controllerClips = sourceAnimator.runtimeAnimatorController.animationClips;
            if (controllerClips != null && controllerClips.Length > 0)
            {
                return SelectPreferredClip(controllerClips);
            }
        }

#if UNITY_EDITOR
        string path = AssetDatabase.GetAssetPath(visualPrefab);
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        UnityEngine.Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
        List<AnimationClip> clips = new List<AnimationClip>();
        for (int i = 0; i < allAssets.Length; i++)
        {
            AnimationClip clip = allAssets[i] as AnimationClip;
            if (clip == null)
            {
                continue;
            }

            string clipName = clip.name.ToLowerInvariant();
            if (clipName.StartsWith("__preview__") || clip.legacy)
            {
                continue;
            }

            clips.Add(clip);
        }

        return SelectPreferredClip(clips.ToArray());
#else
        return null;
#endif
    }

    private static AnimationClip SelectPreferredClip(AnimationClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        string[] preferredNames = { "walk", "run", "idle", "attack" };
        for (int i = 0; i < preferredNames.Length; i++)
        {
            string token = preferredNames[i];
            for (int c = 0; c < clips.Length; c++)
            {
                if (clips[c] == null)
                {
                    continue;
                }

                if (clips[c].name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return clips[c];
                }
            }
        }

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null)
            {
                return clips[i];
            }
        }

        return null;
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
