using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class WeaponSceneAutoSetup : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "BrowserPrototype";
    [SerializeField] private bool onlyWhenNoSpawnerExists = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInstall()
    {
        if (FindObjectOfType<WeaponSceneAutoSetup>() != null)
        {
            return;
        }

        GameObject setupObject = new GameObject("WeaponSceneAutoSetup");
        setupObject.AddComponent<WeaponSceneAutoSetup>();
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
        if (onlyWhenNoSpawnerExists && spawner != null)
        {
            Destroy(gameObject);
            return;
        }

        if (spawner == null)
        {
            GameObject spawnerObject = new GameObject("WeaponDropSpawner");
            spawner = spawnerObject.AddComponent<WeaponDropSpawner>();
        }

        WeaponDropSpawner.WeaponDropDefinition[] definitions = LoadWeaponDefinitions();
        if (definitions.Length > 0)
        {
            spawner.Configure(definitions, 10, 48f, ~0);
            spawner.SpawnDrops(playerCamera.transform.position);
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

    private static WeaponDropSpawner.WeaponDropDefinition[] LoadWeaponDefinitions()
    {
        List<WeaponDropSpawner.WeaponDropDefinition> definitions = new List<WeaponDropSpawner.WeaponDropDefinition>();

#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/MR POLY/Low Poly Weapons Set/Prefabs" });
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
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
}
