using UnityEngine;

public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        BrowserFpsController fpsController = Object.FindFirstObjectByType<BrowserFpsController>();
        if (fpsController == null)
        {
            fpsController = CreatePlayer();
        }

        SetupEnvironment();
        SetupWorld(fpsController.transform);
        SetupHud(fpsController);
        SetupRuntimeSettings();
    }

    private static BrowserFpsController CreatePlayer()
    {
        Vector3 spawnPosition = new Vector3(0f, 3.2f, -10f);

        GameObject player = new("Player");
        player.transform.position = spawnPosition;

        CharacterController characterController = player.AddComponent<CharacterController>();
        characterController.height = 1.8f;
        characterController.radius = 0.35f;
        characterController.center = new Vector3(0f, 0.9f, 0f);
        characterController.stepOffset = 0.3f;
        characterController.slopeLimit = 45f;

        GameObject cameraRootObject = new("CameraRoot");
        cameraRootObject.transform.SetParent(player.transform, false);
        cameraRootObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);

        Camera cameraComponent = Camera.main;
        GameObject cameraObject;
        if (cameraComponent != null)
        {
            cameraObject = cameraComponent.gameObject;
            cameraObject.transform.SetParent(cameraRootObject.transform, false);
            cameraObject.transform.localPosition = Vector3.zero;
            cameraObject.transform.localRotation = Quaternion.identity;
        }
        else
        {
            cameraObject = new GameObject("PlayerCamera");
            cameraObject.transform.SetParent(cameraRootObject.transform, false);
            cameraObject.tag = "MainCamera";
            cameraComponent = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        cameraComponent.nearClipPlane = 0.03f;
        cameraComponent.farClipPlane = 350f;

        GameObject groundCheckObject = new("GroundCheck");
        groundCheckObject.transform.SetParent(player.transform, false);
        groundCheckObject.transform.localPosition = new Vector3(0f, 0.1f, 0f);

        GameObject respawnPoint = new("RespawnPoint");
        respawnPoint.transform.position = spawnPosition;

        BrowserFpsController fpsController = player.AddComponent<BrowserFpsController>();
        fpsController.ConfigureReferences(cameraRootObject.transform, groundCheckObject.transform, respawnPoint.transform);

        BrowserPrototypeInteractor interactor = player.AddComponent<BrowserPrototypeInteractor>();
        interactor.SetInteractionOrigin(cameraObject.transform);

        BrowserPrototypeInteractionState interactionState = player.AddComponent<BrowserPrototypeInteractionState>();
        interactionState.SetInteractor(interactor);

        return fpsController;
    }

    private static void SetupEnvironment()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 120f;
        RenderSettings.fogEndDistance = 420f;
        RenderSettings.fogColor = new Color(0.74f, 0.83f, 0.92f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.76f, 0.79f, 0.74f);

        Light directionalLight = Object.FindFirstObjectByType<Light>();
        if (directionalLight == null)
        {
            GameObject lightObject = new("Directional Light");
            directionalLight = lightObject.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
        }

        directionalLight.intensity = 1.22f;
        directionalLight.color = new Color(1f, 0.95f, 0.84f);
        directionalLight.shadows = LightShadows.Soft;
        directionalLight.shadowStrength = 0.88f;
        directionalLight.transform.rotation = Quaternion.Euler(44f, -36f, 0f);

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.67f, 0.79f, 0.93f);
        }
    }

    private static void SetupWorld(Transform player)
    {
        WorldChunkSpawner spawner = Object.FindFirstObjectByType<WorldChunkSpawner>();
        if (spawner == null)
        {
            GameObject worldObject = new("World");
            spawner = worldObject.AddComponent<WorldChunkSpawner>();
        }

        spawner.SetPlayer(player);
    }

    private static void SetupHud(BrowserFpsController fpsController)
    {
        EndlessRunManager manager = Object.FindFirstObjectByType<EndlessRunManager>();
        if (manager == null)
        {
            GameObject managerObject = new("EndlessRunManager");
            manager = managerObject.AddComponent<EndlessRunManager>();
        }

        BrowserPrototypeInteractor interactor = fpsController.GetComponent<BrowserPrototypeInteractor>();
        BrowserPrototypeInteractionState interactionState = fpsController.GetComponent<BrowserPrototypeInteractionState>();
        manager.Configure(fpsController, interactor, interactionState);
    }

    private static void SetupRuntimeSettings()
    {
        BrowserPrototypeRuntimeSettings runtimeSettings = Object.FindFirstObjectByType<BrowserPrototypeRuntimeSettings>();
        if (runtimeSettings == null)
        {
            GameObject settingsObject = new("RuntimeSettings");
            settingsObject.AddComponent<BrowserPrototypeRuntimeSettings>();
        }
    }
}
