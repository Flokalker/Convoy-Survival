using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BrowserPrototypeSceneBuilder
{
    private const string ScenePath = "Assets/_Project/Scenes/BrowserPrototype.unity";

    [MenuItem("Tools/HTL Spiel/Create Browser Prototype Scene")]
    public static void CreateBrowserPrototypeScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EnsureProjectFolders();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SetupEnvironment();

        GameObject worldRoot = new GameObject("World");
        CreateTestWorld(worldRoot.transform);

        Vector3 spawnPosition = new Vector3(0f, 2.2f, -30f);
        Transform respawnPoint = CreateRespawnPoint(spawnPosition, worldRoot.transform);

        GameObject player = CreatePlayer(spawnPosition, respawnPoint);
        BrowserFpsController fpsController = player.GetComponent<BrowserFpsController>();
        BrowserPrototypeInteractor interactor = player.GetComponent<BrowserPrototypeInteractor>();
        BrowserPrototypeInteractionState interactionState = player.GetComponent<BrowserPrototypeInteractionState>();

        CreateCollectibles(worldRoot.transform);
        CreateHintSign(worldRoot.transform);
        CreateRuntimeSettingsObject();
        CreatePrototypeUi(fpsController, interactor, interactionState);

        bool wasSaved = EditorSceneManager.SaveScene(scene, ScenePath);
        if (!wasSaved)
        {
            Debug.LogError("Browser prototype scene could not be saved.");
            return;
        }

        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = player;
        EditorGUIUtility.PingObject(player);
        Debug.Log("Browser prototype scene created and added to Build Settings.");
    }

    private static void SetupEnvironment()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.52f, 0.55f, 0.58f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.012f;
        RenderSettings.fogColor = new Color(0.62f, 0.7f, 0.78f);

        GameObject lightObject = new GameObject("Directional Light");
        Light directionalLight = lightObject.AddComponent<Light>();
        directionalLight.type = LightType.Directional;
        directionalLight.intensity = 1.05f;
        directionalLight.color = new Color(1f, 0.97f, 0.92f);
        directionalLight.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(44f, -34f, 0f);
    }

    private static void CreateTestWorld(Transform worldRoot)
    {
        Material groundMaterial = CreateMaterial(new Color(0.31f, 0.38f, 0.32f));
        Material pathMaterial = CreateMaterial(new Color(0.42f, 0.42f, 0.45f));
        Material platformMaterial = CreateMaterial(new Color(0.45f, 0.36f, 0.3f));
        Material wallMaterial = CreateMaterial(new Color(0.38f, 0.34f, 0.3f));

        Transform geometryRoot = new GameObject("Geometry").transform;
        geometryRoot.SetParent(worldRoot);

        GameObject ground = CreatePrimitiveCube("Ground", new Vector3(0f, -0.5f, 0f), new Vector3(80f, 1f, 80f), geometryRoot);
        ApplyMaterial(ground, groundMaterial);

        GameObject path = CreatePrimitiveCube("MainPath", new Vector3(0f, 0.1f, -16f), new Vector3(8f, 0.2f, 26f), geometryRoot);
        ApplyMaterial(path, pathMaterial);

        GameObject spawnPlateau = CreatePrimitiveCube("SpawnPlateau", new Vector3(0f, 1f, -30f), new Vector3(8f, 2f, 8f), geometryRoot);
        ApplyMaterial(spawnPlateau, platformMaterial);

        GameObject rampA = CreatePrimitiveCube("RampA", new Vector3(0f, 0.6f, -8f), new Vector3(8f, 1f, 10f), geometryRoot);
        rampA.transform.rotation = Quaternion.Euler(-12f, 0f, 0f);
        ApplyMaterial(rampA, pathMaterial);

        GameObject rampB = CreatePrimitiveCube("RampB", new Vector3(0f, 1.7f, 8f), new Vector3(8f, 1f, 10f), geometryRoot);
        rampB.transform.rotation = Quaternion.Euler(-8f, 0f, 0f);
        ApplyMaterial(rampB, pathMaterial);

        GameObject platformA = CreatePrimitiveCube("PlatformA", new Vector3(0f, 2.8f, 17f), new Vector3(7f, 0.8f, 7f), geometryRoot);
        GameObject platformB = CreatePrimitiveCube("PlatformB", new Vector3(10f, 4.2f, 25f), new Vector3(6f, 0.8f, 6f), geometryRoot);
        GameObject platformC = CreatePrimitiveCube("PlatformC", new Vector3(-10f, 5.5f, 30f), new Vector3(6f, 0.8f, 6f), geometryRoot);
        ApplyMaterial(platformA, platformMaterial);
        ApplyMaterial(platformB, platformMaterial);
        ApplyMaterial(platformC, platformMaterial);

        CreateBoundaries(geometryRoot, wallMaterial);
        CreatePushBoxes(worldRoot, platformMaterial);
    }

    private static void CreateBoundaries(Transform parent, Material wallMaterial)
    {
        float halfSize = 40f;
        float wallHeight = 4f;
        float wallThickness = 1f;

        GameObject northWall = CreatePrimitiveCube("BoundaryNorth", new Vector3(0f, wallHeight * 0.5f, halfSize), new Vector3(80f, wallHeight, wallThickness), parent);
        GameObject southWall = CreatePrimitiveCube("BoundarySouth", new Vector3(0f, wallHeight * 0.5f, -halfSize), new Vector3(80f, wallHeight, wallThickness), parent);
        GameObject eastWall = CreatePrimitiveCube("BoundaryEast", new Vector3(halfSize, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, 80f), parent);
        GameObject westWall = CreatePrimitiveCube("BoundaryWest", new Vector3(-halfSize, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, 80f), parent);

        ApplyMaterial(northWall, wallMaterial);
        ApplyMaterial(southWall, wallMaterial);
        ApplyMaterial(eastWall, wallMaterial);
        ApplyMaterial(westWall, wallMaterial);
    }

    private static void CreatePushBoxes(Transform worldRoot, Material boxMaterial)
    {
        Transform boxRoot = new GameObject("PhysicsBoxes").transform;
        boxRoot.SetParent(worldRoot);

        Vector3[] boxPositions =
        {
            new Vector3(-3f, 0.6f, -12f),
            new Vector3(0f, 0.6f, -10f),
            new Vector3(3f, 0.6f, -8f),
            new Vector3(7f, 0.6f, 4f),
            new Vector3(-7f, 0.6f, 6f),
            new Vector3(2f, 3.8f, 17f),
            new Vector3(10f, 5.2f, 24f),
            new Vector3(-10f, 6.5f, 30f)
        };

        for (int i = 0; i < boxPositions.Length; i++)
        {
            GameObject box = CreatePrimitiveCube(string.Concat("PushBox_", i + 1), boxPositions[i], Vector3.one * 1.2f, boxRoot);
            ApplyMaterial(box, boxMaterial);

            Rigidbody body = box.AddComponent<Rigidbody>();
            body.mass = 6f;
            body.linearDamping = 0.2f;
            body.angularDamping = 0.2f;
        }
    }

    private static Transform CreateRespawnPoint(Vector3 position, Transform parent)
    {
        GameObject respawnPointObject = new GameObject("RespawnPoint");
        respawnPointObject.transform.SetParent(parent);
        respawnPointObject.transform.position = position;
        return respawnPointObject.transform;
    }

    private static GameObject CreatePlayer(Vector3 spawnPosition, Transform respawnPoint)
    {
        GameObject player = new GameObject("Player");
        player.transform.position = spawnPosition;

        CharacterController characterController = player.AddComponent<CharacterController>();
        characterController.height = 1.8f;
        characterController.radius = 0.35f;
        characterController.center = new Vector3(0f, 0.9f, 0f);
        characterController.stepOffset = 0.3f;
        characterController.slopeLimit = 45f;

        GameObject cameraRootObject = new GameObject("CameraRoot");
        cameraRootObject.transform.SetParent(player.transform, false);
        cameraRootObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);

        GameObject cameraObject = new GameObject("PlayerCamera");
        cameraObject.transform.SetParent(cameraRootObject.transform, false);
        cameraObject.tag = "MainCamera";
        Camera cameraComponent = cameraObject.AddComponent<Camera>();
        cameraComponent.nearClipPlane = 0.03f;
        cameraComponent.farClipPlane = 250f;
        cameraObject.AddComponent<AudioListener>();

        GameObject groundCheckObject = new GameObject("GroundCheck");
        groundCheckObject.transform.SetParent(player.transform, false);
        groundCheckObject.transform.localPosition = new Vector3(0f, 0.1f, 0f);

        BrowserFpsController fpsController = player.AddComponent<BrowserFpsController>();
        fpsController.ConfigureReferences(cameraRootObject.transform, groundCheckObject.transform, respawnPoint);

        BrowserPrototypeInteractor interactor = player.AddComponent<BrowserPrototypeInteractor>();
        interactor.SetInteractionOrigin(cameraObject.transform);

        BrowserPrototypeInteractionState interactionState = player.AddComponent<BrowserPrototypeInteractionState>();
        interactionState.SetInteractor(interactor);

        return player;
    }

    private static void CreateCollectibles(Transform worldRoot)
    {
        Material collectibleMaterial = CreateMaterial(new Color(0.9f, 0.77f, 0.25f));
        Transform collectibleRoot = new GameObject("Interactables").transform;
        collectibleRoot.SetParent(worldRoot);

        CreateCollectible(
            collectibleRoot,
            new Vector3(0f, 3.7f, 17f),
            "core_a",
            "Core A",
            1,
            collectibleMaterial);

        CreateCollectible(
            collectibleRoot,
            new Vector3(10f, 5.1f, 25f),
            "core_b",
            "Core B",
            1,
            collectibleMaterial);

        CreateCollectible(
            collectibleRoot,
            new Vector3(-10f, 6.4f, 30f),
            "core_c",
            "Core C",
            1,
            collectibleMaterial);
    }

    private static void CreateCollectible(
        Transform parent,
        Vector3 position,
        string itemId,
        string displayName,
        int value,
        Material material)
    {
        GameObject collectibleObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        collectibleObject.name = string.Concat("Collectible_", displayName.Replace(" ", string.Empty));
        collectibleObject.transform.SetParent(parent);
        collectibleObject.transform.position = position;
        collectibleObject.transform.localScale = Vector3.one * 0.65f;

        SphereCollider sphereCollider = collectibleObject.GetComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        sphereCollider.radius = 0.55f;

        ApplyMaterial(collectibleObject, material);

        BrowserPrototypeCollectible collectible = collectibleObject.AddComponent<BrowserPrototypeCollectible>();
        collectible.Configure(itemId, displayName, value, "E = Collect");

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(collectibleObject.transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 0.9f, 0f);

        TextMesh textMesh = labelObject.AddComponent<TextMesh>();
        textMesh.text = displayName;
        textMesh.fontSize = 48;
        textMesh.characterSize = 0.1f;
        textMesh.color = Color.black;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;

        labelObject.AddComponent<BrowserPrototypeBillboard>();
    }

    private static void CreateHintSign(Transform worldRoot)
    {
        Transform signRoot = new GameObject("HintSign").transform;
        signRoot.SetParent(worldRoot);
        signRoot.position = new Vector3(0f, 1.3f, -22f);

        GameObject post = CreatePrimitiveCube("Post", signRoot.position + new Vector3(0f, 0.8f, 0f), new Vector3(0.2f, 1.6f, 0.2f), signRoot);
        GameObject board = CreatePrimitiveCube("Board", signRoot.position + new Vector3(0f, 1.7f, 0f), new Vector3(2.8f, 1.2f, 0.2f), signRoot);
        post.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.36f, 0.26f, 0.16f));
        board.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.78f, 0.78f, 0.74f));

        GameObject textObject = new GameObject("SignText");
        textObject.transform.SetParent(signRoot, false);
        textObject.transform.localPosition = new Vector3(0f, 1.7f, -0.15f);

        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = "Collect all cores";
        textMesh.fontSize = 60;
        textMesh.characterSize = 0.08f;
        textMesh.color = Color.black;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;

        textObject.AddComponent<BrowserPrototypeBillboard>();
    }

    private static void CreateRuntimeSettingsObject()
    {
        GameObject runtimeSettingsObject = new GameObject("RuntimeSettings");
        runtimeSettingsObject.AddComponent<BrowserPrototypeRuntimeSettings>();
    }

    private static void CreatePrototypeUi(
        BrowserFpsController fpsController,
        BrowserPrototypeInteractor interactor,
        BrowserPrototypeInteractionState interactionState)
    {
        GameObject canvasObject = new GameObject("PrototypeUI", typeof(RectTransform));
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        GameObject overlayObject = CreateUiObject("StartOverlay", canvasObject.transform);
        StretchRect(overlayObject.GetComponent<RectTransform>());
        Image overlayImage = overlayObject.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.74f);
        CanvasGroup overlayGroup = overlayObject.AddComponent<CanvasGroup>();

        Text overlayText = CreateTextElement("OverlayText", overlayObject.transform, uiFont, 30, TextAnchor.MiddleCenter, Color.white);
        RectTransform overlayTextRect = overlayText.rectTransform;
        overlayTextRect.anchorMin = new Vector2(0.1f, 0.2f);
        overlayTextRect.anchorMax = new Vector2(0.9f, 0.8f);
        overlayTextRect.offsetMin = Vector2.zero;
        overlayTextRect.offsetMax = Vector2.zero;
        overlayText.text =
            "Browser Prototype\n\n" +
            "WASD = Move\n" +
            "Shift = Sprint\n" +
            "Space = Jump\n" +
            "Left Click = Lock Cursor\n" +
            "Escape = Release Cursor\n\n" +
            "Press Left Click to start";

        Text interactionPromptText = CreateTextElement("InteractionPrompt", canvasObject.transform, uiFont, 24, TextAnchor.MiddleCenter, Color.white);
        RectTransform promptRect = interactionPromptText.rectTransform;
        promptRect.anchorMin = new Vector2(0.5f, 0f);
        promptRect.anchorMax = new Vector2(0.5f, 0f);
        promptRect.pivot = new Vector2(0.5f, 0f);
        promptRect.anchoredPosition = new Vector2(0f, 34f);
        promptRect.sizeDelta = new Vector2(900f, 50f);
        interactionPromptText.text = string.Empty;
        interactionPromptText.gameObject.SetActive(false);

        Text collectedCounterText = CreateTextElement("CollectedCounter", canvasObject.transform, uiFont, 22, TextAnchor.UpperLeft, Color.white);
        RectTransform counterRect = collectedCounterText.rectTransform;
        counterRect.anchorMin = new Vector2(0f, 1f);
        counterRect.anchorMax = new Vector2(0f, 1f);
        counterRect.pivot = new Vector2(0f, 1f);
        counterRect.anchoredPosition = new Vector2(18f, -18f);
        counterRect.sizeDelta = new Vector2(280f, 40f);
        collectedCounterText.text = "Collected: 0";

        BrowserPrototypeUiController uiController = canvasObject.AddComponent<BrowserPrototypeUiController>();
        uiController.Configure(
            fpsController,
            interactor,
            interactionState,
            overlayGroup,
            overlayText,
            interactionPromptText,
            collectedCounterText);
    }

    private static GameObject CreatePrimitiveCube(string name, Vector3 position, Vector3 scale, Transform parent)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent);
        cube.transform.position = position;
        cube.transform.localScale = scale;
        return cube;
    }

    private static Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            return null;
        }

        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    private static void ApplyMaterial(GameObject target, Material material)
    {
        if (target == null || material == null)
        {
            return;
        }

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject uiObject = new GameObject(name, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private static Text CreateTextElement(string name, Transform parent, Font font, int size, TextAnchor alignment, Color color)
    {
        GameObject textObject = CreateUiObject(name, parent);
        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static void StretchRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void AddSceneToBuildSettings(string path)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        int existingIndex = scenes.FindIndex(scene => scene.path == path);

        if (existingIndex >= 0)
        {
            if (!scenes[existingIndex].enabled)
            {
                scenes[existingIndex] = new EditorBuildSettingsScene(path, true);
            }
        }
        else
        {
            scenes.Add(new EditorBuildSettingsScene(path, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureProjectFolders()
    {
        EnsureFolder("Assets/_Project");
        EnsureFolder("Assets/_Project/Scripts");
        EnsureFolder("Assets/_Project/Scenes");
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parentPath = Path.GetDirectoryName(folderPath);
        if (string.IsNullOrEmpty(parentPath))
        {
            return;
        }

        parentPath = parentPath.Replace("\\", "/");
        if (!AssetDatabase.IsValidFolder(parentPath))
        {
            EnsureFolder(parentPath);
        }

        string folderName = Path.GetFileName(folderPath);
        AssetDatabase.CreateFolder(parentPath, folderName);
    }
}
