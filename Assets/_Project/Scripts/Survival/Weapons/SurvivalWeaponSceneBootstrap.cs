using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class SurvivalWeaponSceneBootstrap : MonoBehaviour
{
    [Header("Scene Scope")]
    [SerializeField] private string targetSceneName = "";
    [SerializeField] private string weaponModelRoot = "Assets/MR POLY";

    [Header("Drop Settings")]
    [SerializeField, Min(0)] private int initialDrops = 8;
    [SerializeField, Min(2f)] private float dropRadius = 30f;
    [SerializeField, Min(0.5f)] private float dropCheckInterval = 2f;
    [SerializeField, Range(0f, 1f)] private float dropChancePerCheck = 0.75f;
    [SerializeField, Min(1)] private int maxActiveDrops = 14;

    private float zombieBarRefreshTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInstall()
    {
        if (FindAnyObjectByType<SurvivalWeaponSceneBootstrap>() != null)
        {
            return;
        }

        GameObject bootstrapObject = new GameObject("SurvivalWeaponSceneBootstrap");
        bootstrapObject.AddComponent<SurvivalWeaponSceneBootstrap>();
    }

    private void Awake()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.IsNullOrWhiteSpace(targetSceneName) && scene.name != targetSceneName)
        {
            Destroy(gameObject);
            return;
        }

        Camera mainCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        if (mainCamera == null)
        {
            Debug.LogWarning("SurvivalWeaponSceneBootstrap: No camera found. Weapon setup skipped.", this);
            Destroy(gameObject);
            return;
        }

        Transform playerRoot = mainCamera.transform.root != null ? mainCamera.transform.root : mainCamera.transform;
        Transform holdPoint = EnsureWeaponHoldPoint(mainCamera.transform);

        CleanupLegacyWeaponSetup(playerRoot);

        List<SurvivalWeaponDefinition> runtimeWeapons = BuildRuntimeWeaponDefinitions();
        if (runtimeWeapons.Count == 0)
        {
            Debug.LogWarning($"SurvivalWeaponSceneBootstrap: No weapon prefabs found under '{weaponModelRoot}'.", this);
            Destroy(gameObject);
            return;
        }

        runtimeWeapons.Sort((a, b) =>
        {
            bool aPistol = a.DisplayName.ToLowerInvariant().Contains("pistol");
            bool bPistol = b.DisplayName.ToLowerInvariant().Contains("pistol");
            if (aPistol == bPistol)
            {
                return 0;
            }

            return aPistol ? -1 : 1;
        });

        SurvivalWeaponManager manager = playerRoot.GetComponent<SurvivalWeaponManager>();
        if (manager == null)
        {
            manager = playerRoot.gameObject.AddComponent<SurvivalWeaponManager>();
        }

        manager.ConfigureRuntime(mainCamera, holdPoint, runtimeWeapons);

        SurvivalWeaponDropManager dropManager = FindAnyObjectByType<SurvivalWeaponDropManager>();
        if (dropManager == null)
        {
            GameObject dropObject = new GameObject("SurvivalWeaponDropManager");
            dropManager = dropObject.AddComponent<SurvivalWeaponDropManager>();
        }

        dropManager.ConfigureRuntime(
            playerRoot,
            runtimeWeapons.ToArray(),
            initialDrops,
            dropRadius,
            dropCheckInterval,
            dropChancePerCheck,
            maxActiveDrops);

        // Guaranteed visible drops near player for immediate validation.
        Vector3 basePoint = playerRoot.position + playerRoot.forward * 3f + Vector3.up * 1.1f;
        dropManager.ForceSpawnAt(basePoint);
        dropManager.ForceSpawnAt(basePoint + playerRoot.right * 1.5f);
        dropManager.ForceSpawnAt(basePoint - playerRoot.right * 1.5f);

        EnsureHud(manager);
        AttachZombieHealthBars();

        Debug.Log($"SurvivalWeaponSceneBootstrap: Integrated {runtimeWeapons.Count} weapons from '{weaponModelRoot}', initial drops: {initialDrops}, plus 3 guaranteed front spawns.", this);
    }

    private void Update()
    {
        if (Time.time < zombieBarRefreshTime)
        {
            return;
        }

        zombieBarRefreshTime = Time.time + 2f;
        AttachZombieHealthBars();
    }

    private static Transform EnsureWeaponHoldPoint(Transform cameraTransform)
    {
        Transform existing = cameraTransform.Find("WeaponHoldPoint");
        if (existing != null)
        {
            return existing;
        }

        GameObject hold = new GameObject("WeaponHoldPoint");
        hold.transform.SetParent(cameraTransform, false);
        hold.transform.localPosition = new Vector3(0.22f, -0.3f, 0.5f);
        hold.transform.localRotation = Quaternion.identity;
        return hold.transform;
    }

    private static void CleanupLegacyWeaponSetup(Transform playerRoot)
    {
        WeaponController legacyController = playerRoot.GetComponent<WeaponController>();
        if (legacyController != null)
        {
            Destroy(legacyController);
        }
    }

    private List<SurvivalWeaponDefinition> BuildRuntimeWeaponDefinitions()
    {
        List<SurvivalWeaponDefinition> definitions = new List<SurvivalWeaponDefinition>();

#if UNITY_EDITOR
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { weaponModelRoot });
        string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { weaponModelRoot });
        List<string> allGuids = new List<string>(prefabGuids.Length + modelGuids.Length);
        allGuids.AddRange(prefabGuids);
        allGuids.AddRange(modelGuids);

        Transform templateRoot = new GameObject("__RuntimeWeaponTemplates").transform;
        templateRoot.gameObject.SetActive(false);

        HashSet<string> seenPaths = new HashSet<string>();
        for (int i = 0; i < allGuids.Count; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(allGuids[i]);
            if (string.IsNullOrWhiteSpace(path) || seenPaths.Contains(path))
            {
                continue;
            }

            seenPaths.Add(path);
            string lowerPath = path.ToLowerInvariant();
            if (!lowerPath.Contains("weapon") && !lowerPath.Contains("rifle") && !lowerPath.Contains("pistol") && !lowerPath.Contains("shotgun"))
            {
                continue;
            }

            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (modelPrefab == null)
            {
                continue;
            }

            SurvivalWeaponView viewTemplate = CreateViewTemplate(templateRoot, modelPrefab);
            if (viewTemplate == null)
            {
                continue;
            }

            SurvivalWeaponDefinition def = ScriptableObject.CreateInstance<SurvivalWeaponDefinition>();
            BuildStatsForName(modelPrefab.name, out SurvivalWeaponDefinition.WeaponType type, out float damage, out float aps, out float range, out bool auto, out bool ammo, out int mag, out int reserve, out float reload);
            string id = modelPrefab.name.Replace(" ", "_").ToLowerInvariant();
            def.ConfigureRuntime(id, modelPrefab.name, type, damage, aps, range, auto, ammo, mag, reserve, reload, viewTemplate);
            definitions.Add(def);
        }
#endif

        return definitions;
    }

    private static SurvivalWeaponView CreateViewTemplate(Transform templateRoot, GameObject modelPrefab)
    {
        GameObject viewRoot = new GameObject(modelPrefab.name + "_View");
        viewRoot.transform.SetParent(templateRoot, false);

        AudioSource audioSource = viewRoot.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        SurvivalWeaponView view = viewRoot.AddComponent<SurvivalWeaponView>();

        GameObject modelInstance = Instantiate(modelPrefab, viewRoot.transform);
        modelInstance.name = "Model";
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localScale = Vector3.one;
        RemovePhysicsComponents(modelInstance);

        NormalizeScale(modelInstance, 0.45f);

        Vector3 muzzleLocal = EstimateMuzzleLocalPosition(modelInstance);
        Transform muzzle = new GameObject("MuzzlePoint").transform;
        muzzle.SetParent(viewRoot.transform, false);
        muzzle.localPosition = muzzleLocal;
        muzzle.localRotation = Quaternion.identity;

        view.ConfigureRuntime(muzzle, audioSource);
        return view;
    }

    private static void RemovePhysicsComponents(GameObject root)
    {
        Collider[] cols = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            Destroy(cols[i]);
        }

        Rigidbody[] rbs = root.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rbs.Length; i++)
        {
            Destroy(rbs[i]);
        }
    }

    private static void NormalizeScale(GameObject root, float targetLongestSide)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float longest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        if (longest <= 0.001f)
        {
            return;
        }

        float scale = Mathf.Max(0.01f, targetLongestSide / longest);
        root.transform.localScale *= scale;
    }

    private static Vector3 EstimateMuzzleLocalPosition(GameObject modelRoot)
    {
        Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Vector3(0f, 0f, 0.35f);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 worldPoint = new Vector3(bounds.center.x, bounds.center.y, bounds.max.z);
        return modelRoot.transform.parent.InverseTransformPoint(worldPoint);
    }

    private static void BuildStatsForName(
        string name,
        out SurvivalWeaponDefinition.WeaponType type,
        out float damage,
        out float attacksPerSecond,
        out float range,
        out bool automatic,
        out bool usesAmmo,
        out int magSize,
        out int reserveAmmo,
        out float reload)
    {
        string lower = name.ToLowerInvariant();

        if (lower.Contains("shotgun"))
        {
            type = SurvivalWeaponDefinition.WeaponType.Ranged;
            // Pump shotgun: very high close-range burst, slow fire cycle.
            damage = 95f;
            attacksPerSecond = 0.95f;
            range = 38f;
            automatic = false;
            usesAmmo = true;
            magSize = 6;
            reserveAmmo = 30;
            reload = 2.2f;
            return;
        }

        if (lower.Contains("pistol"))
        {
            type = SurvivalWeaponDefinition.WeaponType.Ranged;
            damage = 20f;
            attacksPerSecond = 4f;
            range = 95f;
            automatic = false;
            usesAmmo = true;
            magSize = 15;
            reserveAmmo = 75;
            reload = 1.3f;
            return;
        }

        if (lower.Contains("rifle") || lower.Contains("assault"))
        {
            type = SurvivalWeaponDefinition.WeaponType.Ranged;
            damage = 34f;
            attacksPerSecond = 7.2f;
            range = 150f;
            automatic = true;
            usesAmmo = true;
            magSize = 30;
            reserveAmmo = 120;
            reload = 1.8f;
            return;
        }

        type = SurvivalWeaponDefinition.WeaponType.Melee;
        damage = 20f;
        attacksPerSecond = 2.2f;
        range = 2f;
        automatic = false;
        usesAmmo = false;
        magSize = 1;
        reserveAmmo = 0;
        reload = 0.2f;
    }

    private static void EnsureHud(SurvivalWeaponManager manager)
    {
        ApocalypseHudController apocalypseHud = FindAnyObjectByType<ApocalypseHudController>();
        if (apocalypseHud == null)
        {
            GameObject hudObject = new GameObject("ApocalypseHudController");
            apocalypseHud = hudObject.AddComponent<ApocalypseHudController>();
        }

        apocalypseHud.SetWeaponManager(manager);
    }

    private static Text CreateHudText(Transform parent, string name, Font font, Vector2 anchoredPos, Vector2 size)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(parent, false);
        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Text text = textObj.GetComponent<Text>();
        text.font = font;
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.white;
        text.text = "--";
        return text;
    }

    private static Image CreateCrosshair(Transform parent)
    {
        GameObject crossObj = new GameObject("Crosshair", typeof(RectTransform), typeof(Image));
        crossObj.transform.SetParent(parent, false);
        RectTransform rect = crossObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(16f, 16f);
        rect.anchoredPosition = Vector2.zero;

        Image image = crossObj.GetComponent<Image>();
        image.color = Color.white;
        return image;
    }

    private static void AttachZombieHealthBars()
    {
        ZombieAI[] zombies = FindObjectsByType<ZombieAI>(FindObjectsSortMode.None);
        for (int i = 0; i < zombies.Length; i++)
        {
            if (zombies[i] == null)
            {
                continue;
            }

            if (zombies[i].GetComponent<ZombieHealthBarUI>() == null)
            {
                zombies[i].gameObject.AddComponent<ZombieHealthBarUI>();
            }
        }
    }
}
