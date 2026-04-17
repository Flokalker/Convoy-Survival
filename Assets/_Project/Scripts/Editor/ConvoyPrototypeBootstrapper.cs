using System;
using System.Collections.Generic;
using System.IO;
using ConvoySurvival.Core;
using ConvoySurvival.Data;
using ConvoySurvival.Hub;
using ConvoySurvival.Run;
using ConvoySurvival.Run.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ConvoyPrototypeBootstrapper
{
    private const string RootPath = "Assets/_Project";
    private const string ScenesPath = RootPath + "/Scenes";
    private const string PrefabsPath = RootPath + "/Prefabs";
    private const string MaterialsPath = RootPath + "/Materials";
    private const string DataPath = RootPath + "/Data";

    private const string HubScenePath = ScenesPath + "/Hub.unity";
    private const string MainRunScenePath = ScenesPath + "/MainRun.unity";

    [MenuItem("Tools/Convoy Survival/Bootstrap Prototype")]
    public static void BootstrapPrototype()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EnsureFolders();

        Material roadMat = CreateOrUpdateMaterial(MaterialsPath + "/M_Road.mat", new Color(0.13f, 0.13f, 0.14f));
        Material lineMat = CreateOrUpdateMaterial(MaterialsPath + "/M_Line.mat", new Color(0.92f, 0.88f, 0.62f));
        Material truckMat = CreateOrUpdateMaterial(MaterialsPath + "/M_Truck.mat", new Color(0.2f, 0.36f, 0.18f));
        Material zombieMat = CreateOrUpdateMaterial(MaterialsPath + "/M_Zombie.mat", new Color(0.24f, 0.48f, 0.25f));
        Material pickupMat = CreateOrUpdateMaterial(MaterialsPath + "/M_Pickup.mat", new Color(0.82f, 0.65f, 0.18f));
        Material hazardMat = CreateOrUpdateMaterial(MaterialsPath + "/M_Hazard.mat", new Color(0.45f, 0.34f, 0.26f));
        Material gateMat = CreateOrUpdateMaterial(MaterialsPath + "/M_Gate.mat", new Color(0.39f, 0.29f, 0.21f));
        Material propMat = CreateOrUpdateMaterial(MaterialsPath + "/M_TradingProp.mat", new Color(0.68f, 0.66f, 0.56f));
        Material hubGroundMat = CreateOrUpdateMaterial(MaterialsPath + "/M_HubGround.mat", new Color(0.32f, 0.37f, 0.33f));
        Material hubConcreteMat = CreateOrUpdateMaterial(MaterialsPath + "/M_HubConcrete.mat", new Color(0.46f, 0.47f, 0.49f));
        Material hubAccentMat = CreateOrUpdateMaterial(MaterialsPath + "/M_HubAccent.mat", new Color(0.95f, 0.57f, 0.23f));
        Material shopWallMat = CreateOrUpdateMaterial(MaterialsPath + "/M_ShopWall.mat", new Color(0.29f, 0.34f, 0.39f));
        Material shopAwningMat = CreateOrUpdateMaterial(MaterialsPath + "/M_ShopAwning.mat", new Color(0.73f, 0.16f, 0.11f));
        Material neonMat = CreateOrUpdateMaterial(MaterialsPath + "/M_Neon.mat", new Color(0.2f, 0.85f, 0.78f));
        Texture2D hubPavementTex = CreateOrUpdateCheckerTexture(
            MaterialsPath + "/T_HubPavement.asset",
            new Color(0.31f, 0.35f, 0.36f),
            new Color(0.27f, 0.3f, 0.31f),
            64,
            8);
        Texture2D shopMetalTex = CreateOrUpdateStripedTexture(
            MaterialsPath + "/T_ShopMetal.asset",
            new Color(0.33f, 0.39f, 0.45f),
            new Color(0.24f, 0.28f, 0.32f),
            64,
            6);
        Texture2D awningTex = CreateOrUpdateStripedTexture(
            MaterialsPath + "/T_Awning.asset",
            new Color(0.72f, 0.15f, 0.1f),
            new Color(0.86f, 0.81f, 0.73f),
            64,
            10);
        ApplyTextureToMaterial(hubGroundMat, hubPavementTex, new Vector2(8f, 8f));
        ApplyTextureToMaterial(hubConcreteMat, hubPavementTex, new Vector2(6f, 6f));
        ApplyTextureToMaterial(shopWallMat, shopMetalTex, new Vector2(5f, 3f));
        ApplyTextureToMaterial(shopAwningMat, awningTex, new Vector2(3f, 2f));

        TruckUpgradeCatalog catalog = CreateOrUpdateUpgradeCatalog(DataPath + "/TruckUpgradeCatalog.asset");

        GameObject roadPrefab = CreateRoadSegmentPrefab(PrefabsPath + "/PF_RoadSegment.prefab", roadMat, lineMat);
        GameObject truckPrefab = CreateTruckPrefab(PrefabsPath + "/PF_Truck.prefab", truckMat, hazardMat);
        GameObject zombiePrefab = CreateZombiePrefab(PrefabsPath + "/PF_Zombie.prefab", zombieMat);
        GameObject pickupPrefab = CreatePickupPrefab(PrefabsPath + "/PF_ScrapPickup.prefab", pickupMat);
        GameObject hazardPrefab = CreateHazardPrefab(PrefabsPath + "/PF_RoadHazard.prefab", hazardMat);
        GameObject gatePrefab = CreateGatePrefab(PrefabsPath + "/PF_TradingGate.prefab", gateMat);
        GameObject tradingPropPrefab = CreateTradingPropPrefab(PrefabsPath + "/PF_TradingProp.prefab", propMat);
        GameObject waypointPrefab = CreateWaypointPrefab(PrefabsPath + "/PF_Waypoint.prefab", pickupMat);

        CreateHubScene(catalog, hubGroundMat, hubConcreteMat, hubAccentMat, shopWallMat, shopAwningMat, neonMat, truckPrefab);
        CreateMainRunScene(catalog, truckPrefab, roadPrefab, zombiePrefab, pickupPrefab, hazardPrefab, gatePrefab, tradingPropPrefab, waypointPrefab);

        AddScenesToBuildSettings(HubScenePath, MainRunScenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorSceneManager.OpenScene(HubScenePath, OpenSceneMode.Single);
        Debug.Log("Convoy prototype bootstrap complete. Open Hub scene and press Play.");
    }

    private static void CreateHubScene(
        TruckUpgradeCatalog catalog,
        Material hubGroundMat,
        Material hubConcreteMat,
        Material hubAccentMat,
        Material shopWallMat,
        Material shopAwningMat,
        Material neonMat,
        GameObject truckPrefab)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SetupEnvironment(new Color(0.63f, 0.7f, 0.77f), new Color(0.57f, 0.63f, 0.69f), true);

        CreateSessionStateObject(catalog);
        CreateGround("HubGround", new Vector3(0f, -0.5f, 0f), new Vector3(90f, 1f, 90f), hubGroundMat, null);

        CreatePerimeterWalls(hubConcreteMat);
        CreateHubVisualDistrict(hubConcreteMat, hubAccentMat, shopWallMat, shopAwningMat, neonMat);

        GameObject parkedTruck = (GameObject)PrefabUtility.InstantiatePrefab(truckPrefab);
        parkedTruck.name = "ParkedTruck";
        parkedTruck.transform.position = new Vector3(0f, 1.0f, 8f);
        parkedTruck.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        TruckController parkedController = parkedTruck.GetComponent<TruckController>();
        if (parkedController != null)
        {
            parkedController.enabled = false;
        }

        if (parkedTruck.GetComponent<HubParkedTruckPreview>() == null)
        {
            parkedTruck.AddComponent<HubParkedTruckPreview>();
        }

        Rigidbody parkedBody = parkedTruck.GetComponent<Rigidbody>();
        if (parkedBody != null)
        {
            parkedBody.isKinematic = true;
        }

        HubInteractionController interactionController;
        CreateHubPlayer(new Vector3(0f, 1.2f, -10f), out interactionController);

        CreateTruckEntryInteractable(new Vector3(0f, 1f, 4f), hubConcreteMat, hubAccentMat);
        CreateCustomizationStation(TruckSpecialization.Tank, new Vector3(-18f, 1f, 7f), hubConcreteMat, neonMat);
        CreateCustomizationStation(TruckSpecialization.Scout, new Vector3(0f, 1f, 18f), hubConcreteMat, neonMat);
        CreateCustomizationStation(TruckSpecialization.Fortress, new Vector3(18f, 1f, 7f), hubConcreteMat, neonMat);

        CreateHubHud(interactionController);

        EditorSceneManager.SaveScene(scene, HubScenePath);
    }

    private static void CreateMainRunScene(
        TruckUpgradeCatalog catalog,
        GameObject truckPrefab,
        GameObject roadPrefab,
        GameObject zombiePrefab,
        GameObject pickupPrefab,
        GameObject hazardPrefab,
        GameObject gatePrefab,
        GameObject tradingPropPrefab,
        GameObject waypointPrefab)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SetupEnvironment(new Color(0.56f, 0.62f, 0.68f), new Color(0.55f, 0.58f, 0.6f), true);

        CreateSessionStateObject(catalog);

        GameObject truckObject = (GameObject)PrefabUtility.InstantiatePrefab(truckPrefab);
        truckObject.name = "RunTruck";
        truckObject.transform.position = new Vector3(0f, 1.0f, 0f);
        truckObject.transform.rotation = Quaternion.identity;

        TruckController truck = truckObject.GetComponent<TruckController>();

        GameObject systemsObject = new GameObject("RunSystems");
        TradingPostSystem tradingSystem = systemsObject.AddComponent<TradingPostSystem>();
        RoadSegmentSpawner roadSpawner = systemsObject.AddComponent<RoadSegmentSpawner>();
        ConvoyObjectiveSystem objectiveSystem = systemsObject.AddComponent<ConvoyObjectiveSystem>();
        RunGameManager runManager = systemsObject.AddComponent<RunGameManager>();

        tradingSystem.Configure(truck);
        roadSpawner.Configure(truck, tradingSystem, roadPrefab, zombiePrefab, pickupPrefab, hazardPrefab, gatePrefab, tradingPropPrefab);
        objectiveSystem.Configure(truck, waypointPrefab, roadSpawner.LaneWidth);

        GameObject cameraObject = new GameObject("MainCamera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.nearClipPlane = 0.03f;
        camera.farClipPlane = 600f;
        camera.clearFlags = CameraClearFlags.Skybox;
        cameraObject.AddComponent<AudioListener>();

        CameraFollow follow = cameraObject.AddComponent<CameraFollow>();
        follow.SetTarget(truckObject.transform);
        cameraObject.transform.position = new Vector3(0f, 7f, -12f);

        RunHudController hud = CreateRunHud();

        runManager.Configure(truck, roadSpawner, objectiveSystem, tradingSystem, hud);
        runManager.SetHubSceneName("Hub");

        EditorSceneManager.SaveScene(scene, MainRunScenePath);
    }

    private static void CreateSessionStateObject(TruckUpgradeCatalog catalog)
    {
        GameObject sessionObject = new GameObject("PrototypeSessionState");
        PrototypeSessionStateManager session = sessionObject.AddComponent<PrototypeSessionStateManager>();
        session.SetCatalog(catalog);
    }

    private static void CreateHubPlayer(Vector3 position, out HubInteractionController interactionController)
    {
        GameObject player = new GameObject("HubPlayer");
        player.transform.position = position;

        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.35f;
        controller.center = new Vector3(0f, 0.9f, 0f);

        HubPlayerController hubPlayer = player.AddComponent<HubPlayerController>();

        GameObject cameraPivot = new GameObject("CameraPivot");
        cameraPivot.transform.SetParent(player.transform, false);
        cameraPivot.transform.localPosition = new Vector3(0f, 1.6f, 0f);

        GameObject cameraObject = new GameObject("MainCamera");
        cameraObject.transform.SetParent(cameraPivot.transform, false);
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.nearClipPlane = 0.03f;
        camera.farClipPlane = 250f;
        cameraObject.AddComponent<AudioListener>();

        hubPlayer.Configure(cameraPivot.transform);

        interactionController = player.AddComponent<HubInteractionController>();
        interactionController.Configure(cameraObject.transform);
    }

    private static void CreateTruckEntryInteractable(Vector3 position, Material baseMaterial, Material accentMaterial)
    {
        GameObject pedestal = CreateGround("TruckEntry", position, new Vector3(2.8f, 2f, 2.8f), baseMaterial, null);
        CreateGround("EntryHighlight", position + new Vector3(0f, 0.81f, 0f), new Vector3(2.2f, 0.05f, 2.2f), accentMaterial, null);
        HubTruckEntryPoint entryPoint = pedestal.AddComponent<HubTruckEntryPoint>();
        entryPoint.Configure("MainRun");

        CreateLabel("START RUN", pedestal.transform, new Vector3(0f, 1.8f, -1.1f), 180f, new Color(0.98f, 0.94f, 0.8f), 64, 0.075f);
    }

    private static void CreateCustomizationStation(TruckSpecialization specialization, Vector3 position, Material baseMaterial, Material accentMaterial)
    {
        GameObject station = CreateGround(specialization + "Station", position, new Vector3(2.8f, 2f, 2.8f), baseMaterial, null);
        CreateGround("StationAccent", position + new Vector3(0f, 0.81f, 0f), new Vector3(2.2f, 0.05f, 2.2f), accentMaterial, null);
        HubCustomizationStation customization = station.AddComponent<HubCustomizationStation>();
        customization.Configure(specialization, true);

        CreateLabel(specialization + "\nStation", station.transform, new Vector3(0f, 1.85f, -1.1f), 180f, new Color(0.91f, 0.95f, 0.99f), 54, 0.072f);
    }

    private static void CreateHubVisualDistrict(
        Material concreteMat,
        Material accentMat,
        Material shopWallMat,
        Material shopAwningMat,
        Material neonMat)
    {
        GameObject root = new GameObject("HubDistrict");
        Transform district = root.transform;

        CreateGround("MainPlaza", new Vector3(0f, -0.35f, 2f), new Vector3(42f, 0.3f, 44f), concreteMat, district);
        CreateGround("CenterRoad", new Vector3(0f, -0.28f, 2f), new Vector3(10f, 0.08f, 48f), shopWallMat, district);

        for (int i = 0; i < 8; i++)
        {
            float z = -24f + i * 6.8f;
            CreateGround("RoadStripe_" + i, new Vector3(0f, -0.21f, z), new Vector3(0.28f, 0.02f, 2.8f), accentMat, district);
        }

        for (int i = -1; i <= 1; i += 2)
        {
            CreateGround("RoadEdge_" + i, new Vector3(i * 4.6f, -0.21f, 2f), new Vector3(0.18f, 0.02f, 47f), accentMat, district);
        }

        CreateGround("TruckBay", new Vector3(0f, -0.22f, 8f), new Vector3(8.4f, 0.04f, 10f), accentMat, district);
        CreateGround("MarketPadLeft", new Vector3(-18f, -0.25f, 8f), new Vector3(12f, 0.1f, 16f), concreteMat, district);
        CreateGround("MarketPadRight", new Vector3(18f, -0.25f, 8f), new Vector3(12f, 0.1f, 16f), concreteMat, district);
        CreateGround("RadioPad", new Vector3(0f, -0.25f, 18f), new Vector3(14f, 0.1f, 10f), concreteMat, district);

        CreateHubShop("MechanicBay", new Vector3(-18f, 0f, 10.5f), shopWallMat, shopAwningMat, neonMat, "MECHANIC BAY");
        CreateHubShop("RadioPost", new Vector3(0f, 0f, 20f), shopWallMat, shopAwningMat, neonMat, "RADIO POST");
        CreateHubShop("ArmoryDepot", new Vector3(18f, 0f, 10.5f), shopWallMat, shopAwningMat, neonMat, "ARMORY DEPOT");

        CreateRadioTower(new Vector3(0f, 0f, 27f), shopWallMat, neonMat);
        CreateHubLamp(new Vector3(-10f, 0f, -4f), shopWallMat, neonMat);
        CreateHubLamp(new Vector3(10f, 0f, -4f), shopWallMat, neonMat);
        CreateHubLamp(new Vector3(-10f, 0f, 22f), shopWallMat, neonMat);
        CreateHubLamp(new Vector3(10f, 0f, 22f), shopWallMat, neonMat);

        CreateGround("CratePile_A", new Vector3(-24f, 0.6f, 2f), new Vector3(1.4f, 1.2f, 1.4f), concreteMat, district);
        CreateGround("CratePile_B", new Vector3(24f, 0.75f, 2f), new Vector3(1.8f, 1.5f, 1.6f), concreteMat, district);
        CreateGround("BarrelCluster_A", new Vector3(-22f, 0.9f, 15f), new Vector3(0.8f, 1.8f, 0.8f), accentMat, district);
        CreateGround("BarrelCluster_B", new Vector3(22f, 0.9f, 15f), new Vector3(0.8f, 1.8f, 0.8f), accentMat, district);
    }

    private static void CreateHubShop(
        string name,
        Vector3 center,
        Material wallMat,
        Material awningMat,
        Material neonMat,
        string signText)
    {
        GameObject shop = new GameObject(name);
        Transform root = shop.transform;
        root.position = Vector3.zero;

        CreateGround("BackWall", center + new Vector3(0f, 2f, 3.4f), new Vector3(8f, 4f, 0.4f), wallMat, root);
        CreateGround("LeftWall", center + new Vector3(-3.8f, 2f, 0f), new Vector3(0.4f, 4f, 6.8f), wallMat, root);
        CreateGround("RightWall", center + new Vector3(3.8f, 2f, 0f), new Vector3(0.4f, 4f, 6.8f), wallMat, root);
        CreateGround("Roof", center + new Vector3(0f, 4.1f, 0f), new Vector3(8f, 0.3f, 6.8f), wallMat, root);
        CreateGround("Counter", center + new Vector3(0f, 0.9f, -2.2f), new Vector3(6.8f, 1.8f, 1.2f), wallMat, root);
        CreateGround("Awning", center + new Vector3(0f, 3.1f, -2.2f), new Vector3(7.2f, 0.16f, 1.5f), awningMat, root);
        CreateGround("SignBoard", center + new Vector3(0f, 3.6f, -2.2f), new Vector3(5.2f, 0.6f, 0.2f), neonMat, root);
        CreateGround("ShopCrateA", center + new Vector3(-2.2f, 0.45f, -1.1f), new Vector3(1f, 0.9f, 1f), awningMat, root);
        CreateGround("ShopCrateB", center + new Vector3(2f, 0.35f, -0.8f), new Vector3(0.8f, 0.7f, 0.8f), awningMat, root);

        CreateLabel(signText, root, new Vector3(0f, 3.55f, -2.35f), 180f, new Color(0.1f, 0.08f, 0.08f), 64, 0.055f);
    }

    private static void CreateRadioTower(Vector3 basePosition, Material metalMat, Material lightMat)
    {
        CreateGround("RadioTower_Base", basePosition + new Vector3(0f, 1.4f, 0f), new Vector3(1.8f, 2.8f, 1.8f), metalMat, null);
        CreateGround("RadioTower_Mid", basePosition + new Vector3(0f, 4.8f, 0f), new Vector3(1.1f, 4f, 1.1f), metalMat, null);
        CreateGround("RadioTower_Top", basePosition + new Vector3(0f, 8.6f, 0f), new Vector3(0.7f, 3.5f, 0.7f), metalMat, null);
        CreateGround("RadioAntenna", basePosition + new Vector3(0f, 11.8f, 0f), new Vector3(0.18f, 3f, 0.18f), lightMat, null);
        CreateGround("RadioLight", basePosition + new Vector3(0f, 13.1f, 0f), new Vector3(0.45f, 0.45f, 0.45f), lightMat, null);
    }

    private static void CreateHubLamp(Vector3 basePosition, Material poleMat, Material lightMat)
    {
        CreateGround("LampPole", basePosition + new Vector3(0f, 2f, 0f), new Vector3(0.3f, 4f, 0.3f), poleMat, null);
        CreateGround("LampArm", basePosition + new Vector3(0f, 3.85f, 0.8f), new Vector3(0.22f, 0.22f, 1.6f), poleMat, null);
        CreateGround("LampGlow", basePosition + new Vector3(0f, 3.55f, 1.45f), new Vector3(0.42f, 0.42f, 0.42f), lightMat, null);
    }

    private static void CreateHubHud(HubInteractionController interactionController)
    {
        Font font = GetDefaultFont();

        GameObject canvasObject = new GameObject("HubCanvas", typeof(RectTransform));
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        canvasObject.AddComponent<GraphicRaycaster>();

        Text scrapText = CreateUiText(canvasObject.transform, "ScrapText", font, 24, TextAnchor.UpperLeft, Color.white);
        SetRect(scrapText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(420f, 42f));

        Text specializationText = CreateUiText(canvasObject.transform, "SpecializationText", font, 22, TextAnchor.UpperLeft, Color.white);
        SetRect(specializationText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -58f), new Vector2(620f, 140f));

        Text promptText = CreateUiText(canvasObject.transform, "PromptText", font, 24, TextAnchor.LowerCenter, Color.white);
        SetRect(promptText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(900f, 50f));

        Text statusText = CreateUiText(canvasObject.transform, "StatusText", font, 22, TextAnchor.MiddleCenter, new Color(0.96f, 0.93f, 0.74f));
        SetRect(statusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(840f, 40f));

        Text helpText = CreateUiText(canvasObject.transform, "HelpText", font, 19, TextAnchor.UpperRight, new Color(0.92f, 0.95f, 0.98f));
        SetRect(helpText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(440f, 140f));

        HubHudController hud = canvasObject.AddComponent<HubHudController>();
        hud.Configure(interactionController, scrapText, specializationText, promptText, statusText, helpText);
    }

    private static RunHudController CreateRunHud()
    {
        Font font = GetDefaultFont();

        GameObject canvasObject = new GameObject("RunCanvas", typeof(RectTransform));
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        canvasObject.AddComponent<GraphicRaycaster>();

        Text distanceText = CreateUiText(canvasObject.transform, "DistanceText", font, 24, TextAnchor.UpperLeft, Color.white);
        SetRect(distanceText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(330f, 40f));

        Text scrapText = CreateUiText(canvasObject.transform, "ScrapText", font, 24, TextAnchor.UpperLeft, Color.white);
        SetRect(scrapText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -54f), new Vector2(330f, 40f));

        Text durabilityText = CreateUiText(canvasObject.transform, "DurabilityText", font, 22, TextAnchor.UpperLeft, Color.white);
        SetRect(durabilityText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -90f), new Vector2(380f, 36f));

        Text fuelText = CreateUiText(canvasObject.transform, "FuelText", font, 22, TextAnchor.UpperLeft, Color.white);
        SetRect(fuelText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -124f), new Vector2(380f, 36f));

        Text specializationText = CreateUiText(canvasObject.transform, "SpecializationText", font, 21, TextAnchor.UpperLeft, Color.white);
        SetRect(specializationText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -158f), new Vector2(420f, 36f));

        Text objectiveText = CreateUiText(canvasObject.transform, "ObjectiveText", font, 22, TextAnchor.LowerLeft, new Color(0.95f, 0.95f, 0.78f));
        SetRect(objectiveText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 20f), new Vector2(-16f, 56f));

        GameObject tradingPanel = new GameObject("TradingPanel", typeof(RectTransform));
        tradingPanel.transform.SetParent(canvasObject.transform, false);
        Image panelImage = tradingPanel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);
        RectTransform tradingPanelRect = tradingPanel.GetComponent<RectTransform>();
        SetRect(tradingPanelRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 320f));

        Text tradingText = CreateUiText(tradingPanel.transform, "TradingText", font, 22, TextAnchor.UpperLeft, Color.white);
        tradingText.font = font;
        tradingText.horizontalOverflow = HorizontalWrapMode.Wrap;
        tradingText.verticalOverflow = VerticalWrapMode.Overflow;
        SetRect(tradingText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 280f));
        tradingPanel.SetActive(false);

        RunHudController hud = canvasObject.AddComponent<RunHudController>();
        hud.ConfigureTextElements(distanceText, scrapText, durabilityText, fuelText, objectiveText, specializationText, tradingText);
        return hud;
    }

    private static GameObject CreateRoadSegmentPrefab(string path, Material roadMat, Material lineMat)
    {
        GameObject root = new GameObject("PF_RoadSegment");

        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "RoadMesh";
        road.transform.SetParent(root.transform, false);
        road.transform.localScale = new Vector3(12f, 0.5f, 1f);
        ApplyMaterial(road, roadMat);

        for (int i = -1; i <= 1; i++)
        {
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "LaneStripe_" + i;
            stripe.transform.SetParent(root.transform, false);
            stripe.transform.localPosition = new Vector3(i * 3.5f, 0.26f, 0f);
            stripe.transform.localScale = new Vector3(0.2f, 0.02f, 1f);
            ApplyMaterial(stripe, lineMat);
            Collider stripeCollider = stripe.GetComponent<Collider>();
            if (stripeCollider != null)
            {
                UnityEngine.Object.DestroyImmediate(stripeCollider);
            }
        }

        return SavePrefab(root, path);
    }

    private static GameObject CreateTruckPrefab(string path, Material truckMat, Material attachmentMat)
    {
        GameObject root = new GameObject("PF_Truck");

        Rigidbody body = root.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.8f, 0f);
        collider.size = new Vector3(2.4f, 1.6f, 4.8f);

        root.AddComponent<HealthDurabilitySystem>();
        root.AddComponent<TruckController>();
        TruckVisualAdapter visual = root.AddComponent<TruckVisualAdapter>();

        GameObject bodyRoot = new GameObject("BodyRoot");
        bodyRoot.transform.SetParent(root.transform, false);

        CreateTruckPart(bodyRoot.transform, "Chassis", PrimitiveType.Cube, new Vector3(0f, 0.7f, 0f), new Vector3(2.2f, 0.8f, 4.6f), truckMat);
        CreateTruckPart(bodyRoot.transform, "Cab", PrimitiveType.Cube, new Vector3(0f, 1.45f, -0.45f), new Vector3(1.8f, 0.9f, 2.1f), truckMat);

        for (int i = 0; i < 4; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            float z = i < 2 ? 1.4f : -1.4f;
            GameObject wheel = CreateTruckPart(bodyRoot.transform, "Wheel_" + i, PrimitiveType.Cylinder, new Vector3(side * 1.15f, 0.35f, z), new Vector3(0.45f, 0.2f, 0.45f), attachmentMat);
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        GameObject spikes = new GameObject("Spikes");
        spikes.transform.SetParent(bodyRoot.transform, false);
        for (int i = 0; i < 3; i++)
        {
            CreateTruckPart(spikes.transform, "Spike_" + i, PrimitiveType.Cube, new Vector3(-0.8f + i * 0.8f, 0.65f, 2.45f), new Vector3(0.12f, 0.12f, 0.6f), attachmentMat);
        }
        spikes.SetActive(false);

        GameObject plow = new GameObject("Plow");
        plow.transform.SetParent(bodyRoot.transform, false);
        GameObject plowMesh = CreateTruckPart(plow.transform, "PlowMesh", PrimitiveType.Cube, new Vector3(0f, 0.45f, 2.45f), new Vector3(2.3f, 0.45f, 0.7f), attachmentMat);
        plowMesh.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
        plow.SetActive(false);

        GameObject turret = new GameObject("Turret");
        turret.transform.SetParent(bodyRoot.transform, false);
        CreateTruckPart(turret.transform, "TurretBase", PrimitiveType.Cylinder, new Vector3(0f, 1.9f, -0.2f), new Vector3(0.35f, 0.2f, 0.35f), attachmentMat);
        CreateTruckPart(turret.transform, "TurretBarrel", PrimitiveType.Cube, new Vector3(0f, 1.95f, 0.8f), new Vector3(0.12f, 0.12f, 1.2f), attachmentMat);
        turret.SetActive(false);

        visual.Configure(bodyRoot.transform, spikes, plow, turret);

        return SavePrefab(root, path);
    }

    private static GameObject CreateZombiePrefab(string path, Material zombieMat)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        root.name = "PF_Zombie";
        root.transform.localScale = new Vector3(1f, 1.25f, 1f);
        ApplyMaterial(root, zombieMat);

        Rigidbody body = root.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeAll;

        root.AddComponent<ZombieTarget>();

        return SavePrefab(root, path);
    }

    private static GameObject CreatePickupPrefab(string path, Material pickupMat)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "PF_ScrapPickup";
        root.transform.localScale = Vector3.one * 0.7f;
        ApplyMaterial(root, pickupMat);

        SphereCollider collider = root.GetComponent<SphereCollider>();
        if (collider == null)
        {
            collider = root.AddComponent<SphereCollider>();
        }

        collider.isTrigger = true;
        collider.radius = 0.6f;

        root.AddComponent<ScrapPickup>();

        return SavePrefab(root, path);
    }

    private static GameObject CreateHazardPrefab(string path, Material hazardMat)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "PF_RoadHazard";
        root.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        ApplyMaterial(root, hazardMat);

        root.AddComponent<RoadHazard>();
        return SavePrefab(root, path);
    }

    private static GameObject CreateGatePrefab(string path, Material gateMat)
    {
        GameObject root = new GameObject("PF_TradingGate");
        TradingGateController gateController = root.AddComponent<TradingGateController>();

        CreateGatePart(root.transform, "LeftPillar", new Vector3(-2.6f, 1.8f, 0f), new Vector3(0.7f, 3.6f, 0.7f), gateMat);
        CreateGatePart(root.transform, "RightPillar", new Vector3(2.6f, 1.8f, 0f), new Vector3(0.7f, 3.6f, 0.7f), gateMat);
        CreateGatePart(root.transform, "TopBeam", new Vector3(0f, 3.5f, 0f), new Vector3(6f, 0.6f, 0.7f), gateMat);
        GameObject blocker = CreateGatePart(root.transform, "RoadBlocker", new Vector3(0f, 1.1f, 0f), new Vector3(4.8f, 2.2f, 0.7f), gateMat);
        gateController.Configure(blocker);

        return SavePrefab(root, path);
    }

    private static GameObject CreateTradingPropPrefab(string path, Material propMat)
    {
        GameObject root = new GameObject("PF_TradingProp");

        CreateGatePart(root.transform, "CrateA", new Vector3(0f, 0.5f, 0f), new Vector3(1.2f, 1f, 1.2f), propMat);
        CreateGatePart(root.transform, "CrateB", new Vector3(0.8f, 0.4f, 0.9f), new Vector3(0.8f, 0.8f, 0.8f), propMat);

        return SavePrefab(root, path);
    }

    private static GameObject CreateWaypointPrefab(string path, Material waypointMat)
    {
        GameObject root = new GameObject("PF_Waypoint");

        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "Ring";
        ring.transform.SetParent(root.transform, false);
        ring.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        ring.transform.localScale = new Vector3(0.6f, 0.08f, 0.6f);
        ApplyMaterial(ring, waypointMat);

        Collider ringCollider = ring.GetComponent<Collider>();
        if (ringCollider != null)
        {
            UnityEngine.Object.DestroyImmediate(ringCollider);
        }

        SphereCollider trigger = root.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 1.2f;

        root.AddComponent<ObjectiveWaypointMarker>();
        return SavePrefab(root, path);
    }

    private static TruckUpgradeCatalog CreateOrUpdateUpgradeCatalog(string path)
    {
        TruckUpgradeCatalog catalog = AssetDatabase.LoadAssetAtPath<TruckUpgradeCatalog>(path);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<TruckUpgradeCatalog>();
            AssetDatabase.CreateAsset(catalog, path);
        }

        SpecializationPath tankPath = new SpecializationPath
        {
            specialization = TruckSpecialization.Tank,
            displayName = "Tank",
            keyFeature = "Heavy armor, spiked bumper, and plow for ramming.",
            weakness = "Slow and fuel hungry",
            tiers = new[]
            {
                new TruckUpgradeTier
                {
                    TierName = "Tank I",
                    ScrapCost = 0,
                    MaxDurability = 180f,
                    MaxFuel = 120f,
                    ForwardSpeed = 20f,
                    LaneChangeSpeed = 6.2f,
                    FuelDrainPerSecond = 1.55f,
                    CollisionDamageMultiplier = 0.72f,
                    ZombieKillSpeed = 10f,
                    SteeringFactor = 0.9f,
                    TruckScale = 1.05f,
                    HasSpikes = true,
                    HasPlow = false,
                    HasTurret = false,
                    HasNitro = false,
                    NitroSpeedMultiplier = 1f,
                    NitroFuelDrainMultiplier = 1f,
                    ExtraTurretDamagePerSecond = 0f
                },
                new TruckUpgradeTier
                {
                    TierName = "Tank II",
                    ScrapCost = 100,
                    MaxDurability = 230f,
                    MaxFuel = 140f,
                    ForwardSpeed = 21f,
                    LaneChangeSpeed = 6f,
                    FuelDrainPerSecond = 1.75f,
                    CollisionDamageMultiplier = 0.6f,
                    ZombieKillSpeed = 9f,
                    SteeringFactor = 0.85f,
                    TruckScale = 1.1f,
                    HasSpikes = true,
                    HasPlow = true,
                    HasTurret = false,
                    HasNitro = false,
                    NitroSpeedMultiplier = 1f,
                    NitroFuelDrainMultiplier = 1f,
                    ExtraTurretDamagePerSecond = 0f
                },
                new TruckUpgradeTier
                {
                    TierName = "Tank III",
                    ScrapCost = 180,
                    MaxDurability = 290f,
                    MaxFuel = 160f,
                    ForwardSpeed = 22f,
                    LaneChangeSpeed = 5.8f,
                    FuelDrainPerSecond = 1.95f,
                    CollisionDamageMultiplier = 0.48f,
                    ZombieKillSpeed = 8f,
                    SteeringFactor = 0.8f,
                    TruckScale = 1.14f,
                    HasSpikes = true,
                    HasPlow = true,
                    HasTurret = false,
                    HasNitro = false,
                    NitroSpeedMultiplier = 1f,
                    NitroFuelDrainMultiplier = 1f,
                    ExtraTurretDamagePerSecond = 0f
                }
            }
        };

        SpecializationPath scoutPath = new SpecializationPath
        {
            specialization = TruckSpecialization.Scout,
            displayName = "Scout",
            keyFeature = "Nitro boost, off-road tires, and solar roof.",
            weakness = "Fast but fragile",
            tiers = new[]
            {
                new TruckUpgradeTier
                {
                    TierName = "Scout I",
                    ScrapCost = 0,
                    MaxDurability = 90f,
                    MaxFuel = 95f,
                    ForwardSpeed = 27f,
                    LaneChangeSpeed = 10.5f,
                    FuelDrainPerSecond = 0.95f,
                    CollisionDamageMultiplier = 1.28f,
                    ZombieKillSpeed = 17f,
                    SteeringFactor = 1.2f,
                    TruckScale = 0.95f,
                    HasSpikes = false,
                    HasPlow = false,
                    HasTurret = false,
                    HasNitro = true,
                    NitroSpeedMultiplier = 1.55f,
                    NitroFuelDrainMultiplier = 2.35f,
                    ExtraTurretDamagePerSecond = 0f
                },
                new TruckUpgradeTier
                {
                    TierName = "Scout II",
                    ScrapCost = 90,
                    MaxDurability = 105f,
                    MaxFuel = 110f,
                    ForwardSpeed = 29f,
                    LaneChangeSpeed = 11.5f,
                    FuelDrainPerSecond = 0.82f,
                    CollisionDamageMultiplier = 1.2f,
                    ZombieKillSpeed = 16f,
                    SteeringFactor = 1.25f,
                    TruckScale = 0.95f,
                    HasSpikes = false,
                    HasPlow = false,
                    HasTurret = false,
                    HasNitro = true,
                    NitroSpeedMultiplier = 1.6f,
                    NitroFuelDrainMultiplier = 2.2f,
                    ExtraTurretDamagePerSecond = 0f
                },
                new TruckUpgradeTier
                {
                    TierName = "Scout III",
                    ScrapCost = 160,
                    MaxDurability = 120f,
                    MaxFuel = 130f,
                    ForwardSpeed = 31f,
                    LaneChangeSpeed = 12.5f,
                    FuelDrainPerSecond = 0.72f,
                    CollisionDamageMultiplier = 1.14f,
                    ZombieKillSpeed = 15f,
                    SteeringFactor = 1.3f,
                    TruckScale = 0.94f,
                    HasSpikes = false,
                    HasPlow = false,
                    HasTurret = false,
                    HasNitro = true,
                    NitroSpeedMultiplier = 1.68f,
                    NitroFuelDrainMultiplier = 2.1f,
                    ExtraTurretDamagePerSecond = 0f
                }
            }
        };

        SpecializationPath fortressPath = new SpecializationPath
        {
            specialization = TruckSpecialization.Fortress,
            displayName = "Fortress",
            keyFeature = "Roof turrets, reinforced windows, and extra seats.",
            weakness = "Large target and hard steering",
            tiers = new[]
            {
                new TruckUpgradeTier
                {
                    TierName = "Fortress I",
                    ScrapCost = 0,
                    MaxDurability = 160f,
                    MaxFuel = 120f,
                    ForwardSpeed = 23f,
                    LaneChangeSpeed = 6.4f,
                    FuelDrainPerSecond = 1.2f,
                    CollisionDamageMultiplier = 1.04f,
                    ZombieKillSpeed = 12f,
                    SteeringFactor = 0.8f,
                    TruckScale = 1.12f,
                    HasSpikes = false,
                    HasPlow = false,
                    HasTurret = true,
                    HasNitro = false,
                    NitroSpeedMultiplier = 1f,
                    NitroFuelDrainMultiplier = 1f,
                    ExtraTurretDamagePerSecond = 14f
                },
                new TruckUpgradeTier
                {
                    TierName = "Fortress II",
                    ScrapCost = 110,
                    MaxDurability = 210f,
                    MaxFuel = 140f,
                    ForwardSpeed = 24f,
                    LaneChangeSpeed = 6f,
                    FuelDrainPerSecond = 1.3f,
                    CollisionDamageMultiplier = 1.1f,
                    ZombieKillSpeed = 11f,
                    SteeringFactor = 0.75f,
                    TruckScale = 1.16f,
                    HasSpikes = false,
                    HasPlow = false,
                    HasTurret = true,
                    HasNitro = false,
                    NitroSpeedMultiplier = 1f,
                    NitroFuelDrainMultiplier = 1f,
                    ExtraTurretDamagePerSecond = 20f
                },
                new TruckUpgradeTier
                {
                    TierName = "Fortress III",
                    ScrapCost = 190,
                    MaxDurability = 260f,
                    MaxFuel = 165f,
                    ForwardSpeed = 25f,
                    LaneChangeSpeed = 5.7f,
                    FuelDrainPerSecond = 1.42f,
                    CollisionDamageMultiplier = 1.18f,
                    ZombieKillSpeed = 10f,
                    SteeringFactor = 0.7f,
                    TruckScale = 1.2f,
                    HasSpikes = false,
                    HasPlow = false,
                    HasTurret = true,
                    HasNitro = false,
                    NitroSpeedMultiplier = 1f,
                    NitroFuelDrainMultiplier = 1f,
                    ExtraTurretDamagePerSecond = 26f
                }
            }
        };

        catalog.SetPaths(new[] { tankPath, scoutPath, fortressPath });
        EditorUtility.SetDirty(catalog);
        return catalog;
    }

    private static GameObject CreateGround(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.position = position;
        cube.transform.localScale = scale;
        if (parent != null)
        {
            cube.transform.SetParent(parent, false);
        }

        if (material != null)
        {
            ApplyMaterial(cube, material);
        }

        return cube;
    }

    private static void CreatePerimeterWalls(Material wallMaterial)
    {
        CreateGround("Wall_North", new Vector3(0f, 2f, 45f), new Vector3(90f, 4f, 1f), wallMaterial, null);
        CreateGround("Wall_South", new Vector3(0f, 2f, -45f), new Vector3(90f, 4f, 1f), wallMaterial, null);
        CreateGround("Wall_East", new Vector3(45f, 2f, 0f), new Vector3(1f, 4f, 90f), wallMaterial, null);
        CreateGround("Wall_West", new Vector3(-45f, 2f, 0f), new Vector3(1f, 4f, 90f), wallMaterial, null);
    }

    private static void CreateLabel(string textValue, Transform parent, Vector3 localPosition)
    {
        CreateLabel(textValue, parent, localPosition, 180f, Color.white, 60, 0.08f);
    }

    private static void CreateLabel(
        string textValue,
        Transform parent,
        Vector3 localPosition,
        float yRotation,
        Color color,
        int fontSize,
        float characterSize)
    {
        GameObject textObject = new GameObject("Label");
        textObject.transform.SetParent(parent, false);
        textObject.transform.localPosition = localPosition;

        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = textValue;
        textMesh.fontSize = Mathf.Max(24, fontSize);
        textMesh.characterSize = Mathf.Max(0.03f, characterSize);
        textMesh.color = color;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;

        textObject.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    private static Material CreateOrUpdateMaterial(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        float smoothness = path.Contains("Neon", StringComparison.OrdinalIgnoreCase) ? 0.45f : 0.16f;
        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        if (path.Contains("Neon", StringComparison.OrdinalIgnoreCase) && material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", color * 2.2f);
            material.EnableKeyword("_EMISSION");
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D CreateOrUpdateCheckerTexture(string path, Color colorA, Color colorB, int size, int cellSize)
    {
        int textureSize = Mathf.Max(16, size);
        int grid = Mathf.Max(2, cellSize);
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
        {
            texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false, false);
            texture.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(texture, path);
        }

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                bool even = ((x / grid) + (y / grid)) % 2 == 0;
                texture.SetPixel(x, y, even ? colorA : colorB);
            }
        }

        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        texture.Apply(false, false);
        EditorUtility.SetDirty(texture);
        return texture;
    }

    private static Texture2D CreateOrUpdateStripedTexture(string path, Color baseColor, Color stripeColor, int size, int stripeWidth)
    {
        int textureSize = Mathf.Max(16, size);
        int stripe = Mathf.Max(1, stripeWidth);
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
        {
            texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false, false);
            texture.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(texture, path);
        }

        for (int y = 0; y < textureSize; y++)
        {
            bool stripeLine = (y % stripe) < Mathf.Max(1, stripe / 2);
            Color rowColor = stripeLine ? stripeColor : baseColor;
            for (int x = 0; x < textureSize; x++)
            {
                texture.SetPixel(x, y, rowColor);
            }
        }

        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        texture.Apply(false, false);
        EditorUtility.SetDirty(texture);
        return texture;
    }

    private static void ApplyTextureToMaterial(Material material, Texture2D texture, Vector2 tiling)
    {
        if (material == null || texture == null)
        {
            return;
        }

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
        }

        material.mainTexture = texture;
        material.mainTextureScale = tiling;
        EditorUtility.SetDirty(material);
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

    private static GameObject SavePrefab(GameObject source, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
        UnityEngine.Object.DestroyImmediate(source);
        return prefab;
    }

    private static GameObject CreateTruckPart(Transform parent, string name, PrimitiveType primitive, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        ApplyMaterial(part, material);

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            UnityEngine.Object.DestroyImmediate(collider);
        }

        return part;
    }

    private static GameObject CreateGatePart(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        ApplyMaterial(part, material);
        return part;
    }

    private static Text CreateUiText(Transform parent, string name, Font font, int size, TextAnchor anchor, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.text = string.Empty;
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0f, 1f);

        if (Mathf.Approximately(anchorMin.x, 0.5f) && Mathf.Approximately(anchorMax.x, 0.5f))
        {
            rect.pivot = new Vector2(0.5f, rect.pivot.y);
        }

        if (Mathf.Approximately(anchorMin.y, 0f) && Mathf.Approximately(anchorMax.y, 0f))
        {
            rect.pivot = new Vector2(rect.pivot.x, 0f);
        }

        if (Mathf.Approximately(anchorMin.x, 1f) && Mathf.Approximately(anchorMax.x, 1f))
        {
            rect.pivot = new Vector2(1f, rect.pivot.y);
        }

        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return font;
    }

    private static void SetupEnvironment(Color ambient, Color fogColor, bool fog)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = ambient;
        RenderSettings.fog = fog;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0035f;

        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.08f;
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(42f, -34f, 0f);
    }

    private static void AddScenesToBuildSettings(params string[] scenePaths)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        for (int i = 0; i < scenePaths.Length; i++)
        {
            string path = scenePaths[i];
            int index = scenes.FindIndex(item => item.path == path);
            if (index >= 0)
            {
                scenes[index] = new EditorBuildSettingsScene(path, true);
            }
            else
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureFolders()
    {
        EnsureFolder(RootPath);
        EnsureFolder(ScenesPath);
        EnsureFolder(PrefabsPath);
        EnsureFolder(MaterialsPath);
        EnsureFolder(DataPath);
        EnsureFolder(RootPath + "/Scripts");
        EnsureFolder(RootPath + "/Scripts/Editor");
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath);
        if (string.IsNullOrEmpty(parent))
        {
            return;
        }

        parent = parent.Replace("\\", "/");
        if (!AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, Path.GetFileName(folderPath));
    }
}
