using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class WeaponSceneAutoSetup : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "";
    [SerializeField] private bool onlyWhenNoSpawnerExists = true;
    [SerializeField] private string editorWeaponRootFolder = "Assets/MR POLY";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInstall()
    {
        // Legacy setup disabled. Replaced by SurvivalWeaponSceneBootstrap.
    }

    private void Awake()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.IsNullOrWhiteSpace(targetSceneName) && scene.name != targetSceneName)
        {
            Destroy(gameObject);
            return;
        }

        Camera playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindFirstObjectByType<Camera>();
        }

        if (playerCamera == null)
        {
            Debug.LogWarning("WeaponSceneAutoSetup: No Main Camera found. Skipping weapon setup.", this);
            Destroy(gameObject);
            return;
        }

        GameObject controllerOwner = playerCamera.transform.root != null ? playerCamera.transform.root.gameObject : playerCamera.gameObject;
        WeaponController controller = controllerOwner.GetComponent<WeaponController>();
        if (controller == null)
        {
            controller = controllerOwner.AddComponent<WeaponController>();
        }

        Transform holdPoint = EnsureWeaponHoldPoint(playerCamera.transform);
        controller.Configure(playerCamera, holdPoint);

        WeaponDropSpawner spawner = FindObjectOfType<WeaponDropSpawner>();

        if (spawner == null)
        {
            GameObject spawnerObject = new GameObject("WeaponDropSpawner");
            spawner = spawnerObject.AddComponent<WeaponDropSpawner>();
        }
        else if (onlyWhenNoSpawnerExists)
        {
            // Keep using existing spawner, but still ensure it is configured.
        }

        WeaponDropSpawner.WeaponDropDefinition[] definitions = LoadWeaponDefinitions(editorWeaponRootFolder);
        if (definitions.Length > 0)
        {
            spawner.Configure(definitions, 8, 32f, ~0);
            spawner.SetSpawnCenter(playerCamera.transform);
            spawner.SpawnDrops(playerCamera.transform.position);
            EquipStarterPistol(controller, definitions);
            Debug.Log($"WeaponSceneAutoSetup: Loaded {definitions.Length} weapon prefabs from '{editorWeaponRootFolder}'.");
        }
        else
        {
            Debug.LogWarning($"WeaponSceneAutoSetup: No weapon prefabs found under '{editorWeaponRootFolder}'.");
        }

        Destroy(gameObject);
    }

    private static Transform EnsureWeaponHoldPoint(Transform cameraTransform)
    {
        Transform holdPoint = cameraTransform.Find("WeaponHoldPoint");
        if (holdPoint != null)
        {
            return holdPoint;
        }

        GameObject holdPointObject = new GameObject("WeaponHoldPoint");
        holdPointObject.transform.SetParent(cameraTransform, false);
        holdPointObject.transform.localPosition = new Vector3(0.2f, -0.2f, 0.45f);
        holdPointObject.transform.localRotation = Quaternion.identity;
        return holdPointObject.transform;
    }

    private static WeaponDropSpawner.WeaponDropDefinition[] LoadWeaponDefinitions(string weaponRootFolder)
    {
        List<WeaponDropSpawner.WeaponDropDefinition> definitions = new List<WeaponDropSpawner.WeaponDropDefinition>();

#if UNITY_EDITOR
        string[] searchFolders = string.IsNullOrWhiteSpace(weaponRootFolder)
            ? new[] { "Assets/MR POLY" }
            : new[] { weaponRootFolder };
        string[] guids = AssetDatabase.FindAssets("t:Prefab", searchFolders);
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (assetPath.IndexOf("weapon", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                assetPath.IndexOf("rifle", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                assetPath.IndexOf("pistol", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                assetPath.IndexOf("shotgun", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                continue;
            }

            WeaponDropSpawner.WeaponDropDefinition definition = new WeaponDropSpawner.WeaponDropDefinition
            {
                visualPrefab = prefab,
                weaponName = prefab.name
            };

            ApplyWeaponStatsFromName(definition);
            definitions.Add(definition);
        }
#endif

        if (definitions.Count == 0)
        {
            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fallback.name = "FallbackWeapon";
            fallback.transform.localScale = new Vector3(0.3f, 0.2f, 1f);
            fallback.SetActive(false);

            WeaponDropSpawner.WeaponDropDefinition definition = new WeaponDropSpawner.WeaponDropDefinition
            {
                visualPrefab = fallback,
                weaponName = "Fallback Rifle",
                damage = 20f,
                fireRate = 6f,
                range = 120f
            };
            definitions.Add(definition);
        }

        return definitions.ToArray();
    }

    private static void ApplyWeaponStatsFromName(WeaponDropSpawner.WeaponDropDefinition definition)
    {
        string lower = definition.weaponName.ToLowerInvariant();
        if (lower.Contains("shotgun"))
        {
            definition.damage = 55f;
            definition.fireRate = 1.25f;
            definition.range = 45f;
            return;
        }

        if (lower.Contains("pistol"))
        {
            definition.damage = 22f;
            definition.fireRate = 4.2f;
            definition.range = 95f;
            return;
        }

        if (lower.Contains("rifle") || lower.Contains("assault"))
        {
            definition.damage = 30f;
            definition.fireRate = 7.5f;
            definition.range = 150f;
            return;
        }

        definition.damage = 24f;
        definition.fireRate = 6f;
        definition.range = 120f;
    }

    private static void EquipStarterPistol(WeaponController controller, WeaponDropSpawner.WeaponDropDefinition[] definitions)
    {
        if (controller == null || definitions == null || definitions.Length == 0)
        {
            return;
        }

        WeaponDropSpawner.WeaponDropDefinition starter = null;
        for (int i = 0; i < definitions.Length; i++)
        {
            if (definitions[i] == null || string.IsNullOrWhiteSpace(definitions[i].weaponName))
            {
                continue;
            }

            if (definitions[i].weaponName.ToLowerInvariant().Contains("pistol"))
            {
                starter = definitions[i];
                break;
            }
        }

        if (starter == null)
        {
            starter = definitions[0];
        }

        controller.EquipWeapon(starter.visualPrefab, starter.weaponName, starter.damage, starter.fireRate, starter.range);
    }
}
