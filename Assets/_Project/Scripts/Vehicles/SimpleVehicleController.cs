using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class SimpleVehicleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody vehicleBody;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform seatPoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private VehicleEnterTrigger enterTrigger;
    [SerializeField] private VehicleFollowCamera vehicleCamera;
    [SerializeField] private GameObject playerRoot;
    [SerializeField] private Transform frontLeftWheel;
    [SerializeField] private Transform frontRightWheel;

    [Header("Player Handling")]
    [SerializeField] private Behaviour[] playerComponentsToDisable;
    [SerializeField] private Collider[] playerCollidersToDisable;
    [SerializeField] private GameObject[] playerObjectsToHide;
    [SerializeField] private bool autoDisableCharacterController = true;
    [SerializeField] private bool autoDisablePlayerRigidbody = true;
    [SerializeField] private Vector3 fallbackExitOffset = new Vector3(-1.35f, 0.1f, 0.6f);

    [Header("Driving")]
    [SerializeField, Min(0.1f)] private float maxForwardSpeed = 12f;
    [SerializeField, Min(0.1f)] private float maxReverseSpeed = 5f;
    [SerializeField, Min(0.1f)] private float acceleration = 14f;
    [SerializeField, Min(0.1f)] private float brakeAcceleration = 18f;
    [SerializeField, Min(0f)] private float turnSpeed = 85f;
    [SerializeField, Min(0f)] private float lateralDamping = 10f;

    [Header("Wheel Visuals")]
    [SerializeField, Range(0f, 45f)] private float maxWheelSteerAngle = 30f;
    [SerializeField, Min(0f)] private float wheelSteerSmoothSpeed = 12f;

    [Header("UI")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private Text interactionPromptText;
    [SerializeField] private Text controlsHintText;
    [SerializeField] private Image interactionPromptBackground;
    [SerializeField] private Image controlsHintBackground;
    [SerializeField] private string enterPromptMessage = "Drücke E zum Einsteigen";
    [SerializeField] private string exitPromptMessage = "Drücke E zum Aussteigen";
    [SerializeField] private string controlsHintMessage = "E = Ein-/Aussteigen\nWASD = Fahren\nMaus = Kamera";

    private readonly List<BehaviourState> disabledBehaviours = new List<BehaviourState>();
    private readonly List<ColliderState> disabledColliders = new List<ColliderState>();
    private readonly List<GameObjectState> hiddenObjects = new List<GameObjectState>();

    private CharacterController cachedCharacterController;
    private bool cachedCharacterControllerEnabled;
    private Rigidbody cachedPlayerRigidbody;
    private bool cachedPlayerRigidbodyWasKinematic;
    private Transform cachedPlayerOriginalParent;
    private Camera cachedDefaultCamera;
    private AudioListener cachedDefaultAudioListener;
    private GameObject activeDriver;
    private bool isDriving;
    private float currentVisualSteerAngle;
    private Quaternion frontLeftWheelBaseRotation;
    private Quaternion frontRightWheelBaseRotation;
    private bool wheelBaseRotationsCached;

    public GameObject PlayerRoot => playerRoot;
    public bool IsOccupied => isDriving;
    public bool IsDriving => isDriving;
    public bool HasAssignedPlayer => playerRoot != null;

    public bool TryResolvePlayerFromCollider(Collider candidateCollider, out GameObject resolvedPlayer)
    {
        resolvedPlayer = null;
        if (candidateCollider == null)
        {
            return false;
        }

        Transform candidateTransform = candidateCollider.attachedRigidbody != null
            ? candidateCollider.attachedRigidbody.transform
            : candidateCollider.transform;

        if (candidateTransform == null)
        {
            return false;
        }

        Transform candidateRoot = candidateTransform.root;

        if (playerRoot != null)
        {
            Transform playerTransform = playerRoot.transform;
            if (candidateRoot == playerTransform
                || candidateTransform == playerTransform
                || candidateTransform.IsChildOf(playerTransform)
                || playerTransform.IsChildOf(candidateTransform))
            {
                resolvedPlayer = playerRoot;
                return true;
            }

            return false;
        }

        if (HasSupportedPlayerName(candidateRoot.name))
        {
            resolvedPlayer = candidateRoot.gameObject;
            return true;
        }

        if (HasPlayerTag(candidateTransform) || HasPlayerTag(candidateRoot))
        {
            resolvedPlayer = candidateRoot.gameObject;
            return true;
        }

        return false;
    }

    private void Reset()
    {
        vehicleBody = GetComponent<Rigidbody>();
        visualRoot = transform.Find("SF_Model");
        seatPoint = transform.Find("SeatPoint");
        exitPoint = transform.Find("ExitPoint");
        enterTrigger = GetComponentInChildren<VehicleEnterTrigger>(true);
        vehicleCamera = GetComponentInChildren<VehicleFollowCamera>(true);
        frontLeftWheel = transform.Find("FrontLeftWheel");
        frontRightWheel = transform.Find("FrontRightWheel");
    }

    private void Awake()
    {
        if (vehicleBody == null)
        {
            vehicleBody = GetComponent<Rigidbody>();
        }

        TryAutoAssignVisualRoot();
        TryAutoAssignWheelReferences();

        // Keep the imported mesh under the physics root so collider, seat and camera move together.
        if (visualRoot != null && visualRoot.parent != transform)
        {
            visualRoot.SetParent(transform, true);
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one;
        }

        vehicleBody.mass = Mathf.Max(vehicleBody.mass, 1200f);
        vehicleBody.linearDamping = Mathf.Max(vehicleBody.linearDamping, 0.5f);
        vehicleBody.angularDamping = Mathf.Max(vehicleBody.angularDamping, 2f);
        vehicleBody.interpolation = RigidbodyInterpolation.Interpolate;
        vehicleBody.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (enterTrigger == null)
        {
            enterTrigger = GetComponentInChildren<VehicleEnterTrigger>(true);
        }

        if (enterTrigger != null)
        {
            enterTrigger.SetVehicle(this);
        }

        if (vehicleCamera == null)
        {
            vehicleCamera = GetComponentInChildren<VehicleFollowCamera>(true);
        }

        if (vehicleCamera != null)
        {
            vehicleCamera.SetTarget(transform);
            vehicleCamera.SetCameraActive(false);
        }

        EnsureVehicleUiExists();
        CacheWheelBaseRotations();
        SetInteractionPromptVisible(false);
        SetControlsHintVisible(false);
    }

    private void Start()
    {
        TryAutoAssignPlayer();
        UpdateVehicleUi();
    }

    private void Update()
    {
        TryAutoAssignPlayer();

        Keyboard keyboard = Keyboard.current;
        if (!isDriving)
        {
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                TryEnterVehicle();
            }
        }
        else if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
        {
            ExitVehicle();
        }

        UpdateWheelVisuals();
        UpdateVehicleUi();
    }

    private void FixedUpdate()
    {
        if (!isDriving || Keyboard.current == null)
        {
            return;
        }

        float throttleInput = ReadThrottleInput();
        float steerInput = ReadSteerInput();

        Vector3 velocity = vehicleBody.linearVelocity;
        Vector3 localVelocity = transform.InverseTransformDirection(velocity);

        float targetSpeed = throttleInput >= 0f
            ? throttleInput * maxForwardSpeed
            : throttleInput * maxReverseSpeed;

        float currentAcceleration = Mathf.Abs(throttleInput) > 0.01f ? acceleration : brakeAcceleration;
        localVelocity.z = Mathf.MoveTowards(localVelocity.z, targetSpeed, currentAcceleration * Time.fixedDeltaTime);
        localVelocity.x = Mathf.MoveTowards(localVelocity.x, 0f, lateralDamping * Time.fixedDeltaTime);

        Vector3 adjustedVelocity = transform.TransformDirection(new Vector3(localVelocity.x, 0f, localVelocity.z));
        adjustedVelocity.y = velocity.y;
        vehicleBody.linearVelocity = adjustedVelocity;

        float speedFactor = Mathf.InverseLerp(0.15f, maxForwardSpeed, Mathf.Abs(localVelocity.z));
        if (Mathf.Abs(steerInput) > 0.01f && speedFactor > 0f)
        {
            float movementDirection = Mathf.Sign(localVelocity.z);
            float turnAmount = steerInput * turnSpeed * speedFactor * movementDirection * Time.fixedDeltaTime;
            vehicleBody.MoveRotation(vehicleBody.rotation * Quaternion.Euler(0f, turnAmount, 0f));
        }
    }

    private void OnDisable()
    {
        if (isDriving)
        {
            ExitVehicle();
        }

        SetInteractionPromptVisible(false);
        SetControlsHintVisible(false);
    }

    private bool IsAssignedPlayerInRange()
    {
        if (enterTrigger == null || !enterTrigger.PlayerInRange)
        {
            return false;
        }

        GameObject nearbyPlayer = enterTrigger.PlayerObject;
        if (nearbyPlayer == null)
        {
            return false;
        }

        return playerRoot == null || nearbyPlayer == playerRoot;
    }

    private void TryEnterVehicle()
    {
        if (isDriving || enterTrigger == null || seatPoint == null)
        {
            return;
        }

        if (!IsAssignedPlayerInRange())
        {
            return;
        }

        GameObject candidate = enterTrigger.PlayerObject;
        if (candidate == null)
        {
            return;
        }

        EnterVehicle(candidate);
    }

    private void EnterVehicle(GameObject targetPlayer)
    {
        if (isDriving || targetPlayer == null || seatPoint == null)
        {
            return;
        }

        Debug.Log("Entering vehicle", this);

        playerRoot = targetPlayer;
        activeDriver = targetPlayer;
        cachedPlayerOriginalParent = targetPlayer.transform.parent;

        CacheDefaultCamera();
        DisablePlayerForDriving(targetPlayer);

        // The player is snapped onto the seat to keep this first version predictable.
        targetPlayer.transform.SetParent(seatPoint, true);
        targetPlayer.transform.SetPositionAndRotation(seatPoint.position, seatPoint.rotation);

        currentVisualSteerAngle = 0f;
        SwitchToVehicleCamera(true);
        isDriving = true;
    }

    private void ExitVehicle()
    {
        if (!isDriving || activeDriver == null)
        {
            isDriving = false;
            return;
        }

        Debug.Log("Exiting vehicle", this);

        Transform driverTransform = activeDriver.transform;
        driverTransform.SetParent(cachedPlayerOriginalParent, true);

        Vector3 targetExitPosition = exitPoint != null
            ? exitPoint.position
            : transform.TransformPoint(fallbackExitOffset);

        Quaternion targetExitRotation = exitPoint != null
            ? Quaternion.Euler(0f, exitPoint.eulerAngles.y, 0f)
            : Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        if (cachedCharacterController != null)
        {
            cachedCharacterController.enabled = false;
        }

        driverTransform.SetPositionAndRotation(targetExitPosition, targetExitRotation);

        RestorePlayerAfterDriving();
        SwitchToVehicleCamera(false);

        activeDriver = null;
        isDriving = false;
    }

    private void DisablePlayerForDriving(GameObject targetPlayer)
    {
        disabledBehaviours.Clear();
        disabledColliders.Clear();
        hiddenObjects.Clear();

        cachedCharacterController = null;
        cachedPlayerRigidbody = null;

        foreach (Behaviour behaviour in playerComponentsToDisable)
        {
            DisableBehaviourIfNeeded(behaviour);
        }

        DisableCommonPlayerBehaviours(targetPlayer);

        foreach (Collider targetCollider in playerCollidersToDisable)
        {
            if (targetCollider == null)
            {
                continue;
            }

            disabledColliders.Add(new ColliderState(targetCollider, targetCollider.enabled));
            targetCollider.enabled = false;
        }

        foreach (GameObject hiddenObject in playerObjectsToHide)
        {
            if (hiddenObject == null)
            {
                continue;
            }

            hiddenObjects.Add(new GameObjectState(hiddenObject, hiddenObject.activeSelf));
            hiddenObject.SetActive(false);
        }

        if (autoDisableCharacterController)
        {
            cachedCharacterController = targetPlayer.GetComponent<CharacterController>();
            if (cachedCharacterController != null)
            {
                cachedCharacterControllerEnabled = cachedCharacterController.enabled;
                cachedCharacterController.enabled = false;
            }
        }

        if (autoDisablePlayerRigidbody)
        {
            cachedPlayerRigidbody = targetPlayer.GetComponent<Rigidbody>();
            if (cachedPlayerRigidbody != null)
            {
                cachedPlayerRigidbodyWasKinematic = cachedPlayerRigidbody.isKinematic;
                cachedPlayerRigidbody.linearVelocity = Vector3.zero;
                cachedPlayerRigidbody.angularVelocity = Vector3.zero;
                cachedPlayerRigidbody.isKinematic = true;
            }
        }
    }

    private void RestorePlayerAfterDriving()
    {
        foreach (BehaviourState state in disabledBehaviours)
        {
            if (state.Behaviour != null)
            {
                state.Behaviour.enabled = state.WasEnabled;
            }
        }

        foreach (ColliderState state in disabledColliders)
        {
            if (state.Collider != null)
            {
                state.Collider.enabled = state.WasEnabled;
            }
        }

        foreach (GameObjectState state in hiddenObjects)
        {
            if (state.GameObject != null)
            {
                state.GameObject.SetActive(state.WasActive);
            }
        }

        if (cachedCharacterController != null)
        {
            cachedCharacterController.enabled = cachedCharacterControllerEnabled;
            cachedCharacterController = null;
        }

        if (cachedPlayerRigidbody != null)
        {
            cachedPlayerRigidbody.isKinematic = cachedPlayerRigidbodyWasKinematic;
            cachedPlayerRigidbody = null;
        }
    }

    private void CacheDefaultCamera()
    {
        cachedDefaultCamera = null;
        cachedDefaultAudioListener = null;

        if (playerRoot != null)
        {
            Camera playerCamera = playerRoot.GetComponentInChildren<Camera>(true);
            if (playerCamera != null && playerCamera != vehicleCamera?.GetComponent<Camera>())
            {
                cachedDefaultCamera = playerCamera;
            }
        }

        if (cachedDefaultCamera == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null && mainCamera != vehicleCamera?.GetComponent<Camera>())
            {
                cachedDefaultCamera = mainCamera;
            }
        }

        if (cachedDefaultCamera != null)
        {
            cachedDefaultAudioListener = cachedDefaultCamera.GetComponent<AudioListener>();
        }
    }

    private void SwitchToVehicleCamera(bool driving)
    {
        if (cachedDefaultCamera != null)
        {
            cachedDefaultCamera.enabled = !driving;
        }

        if (cachedDefaultAudioListener != null)
        {
            cachedDefaultAudioListener.enabled = !driving;
        }

        if (vehicleCamera != null)
        {
            vehicleCamera.SetCameraActive(driving);
        }
    }

    private void UpdateWheelVisuals()
    {
        if (!wheelBaseRotationsCached)
        {
            CacheWheelBaseRotations();
        }

        float targetAngle = isDriving ? ReadSteerInput() * maxWheelSteerAngle : 0f;
        float smoothStep = wheelSteerSmoothSpeed <= 0f
            ? Mathf.Abs(targetAngle - currentVisualSteerAngle)
            : wheelSteerSmoothSpeed * Time.deltaTime * maxWheelSteerAngle;

        currentVisualSteerAngle = Mathf.MoveTowards(currentVisualSteerAngle, targetAngle, smoothStep);

        if (frontLeftWheel != null)
        {
            frontLeftWheel.localRotation = frontLeftWheelBaseRotation * Quaternion.Euler(0f, currentVisualSteerAngle, 0f);
        }

        if (frontRightWheel != null)
        {
            frontRightWheel.localRotation = frontRightWheelBaseRotation * Quaternion.Euler(0f, currentVisualSteerAngle, 0f);
        }
    }

    private void UpdateVehicleUi()
    {
        EnsureVehicleUiExists();

        bool playerInRange = IsAssignedPlayerInRange();
        bool shouldShowPrompt = playerInRange && !isDriving;
        bool shouldShowControls = playerInRange || isDriving;

        if (interactionPromptText != null)
        {
            interactionPromptText.text = enterPromptMessage;

            Color promptColor = Color.white;
            promptColor.a = shouldShowPrompt
                ? Mathf.Lerp(0.8f, 1f, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f))
                : 0f;

            interactionPromptText.color = promptColor;
        }

        if (controlsHintText != null)
        {
            controlsHintText.text = controlsHintMessage;
        }

        SetInteractionPromptVisible(shouldShowPrompt);
        SetControlsHintVisible(shouldShowControls);
    }

    private void EnsureVehicleUiExists()
    {
        if (uiCanvas == null)
        {
            GameObject canvasObject = new GameObject("VehicleUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            uiCanvas = canvasObject.GetComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiCanvas.pixelPerfect = true;
            uiCanvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (interactionPromptBackground == null || interactionPromptText == null)
        {
            Image createdBackground = CreatePanel(
                "InteractionPrompt",
                uiCanvas.transform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 110f),
                new Vector2(420f, 56f),
                new Color(0f, 0f, 0f, 0.55f));

            interactionPromptBackground = createdBackground;
            interactionPromptText = CreateText(
                "InteractionPromptText",
                createdBackground.transform,
                enterPromptMessage,
                TextAnchor.MiddleCenter,
                28,
                Color.white);
        }

        if (controlsHintBackground == null || controlsHintText == null)
        {
            Image createdBackground = CreatePanel(
                "ControlsHint",
                uiCanvas.transform,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(20f, 20f),
                new Vector2(300f, 110f),
                new Color(0f, 0f, 0f, 0.48f));

            controlsHintBackground = createdBackground;
            controlsHintText = CreateText(
                "ControlsHintText",
                createdBackground.transform,
                controlsHintMessage,
                TextAnchor.UpperLeft,
                22,
                Color.white);

            RectTransform controlsTextRect = controlsHintText.rectTransform;
            controlsTextRect.anchorMin = Vector2.zero;
            controlsTextRect.anchorMax = Vector2.one;
            controlsTextRect.offsetMin = new Vector2(14f, 12f);
            controlsTextRect.offsetMax = new Vector2(-14f, -12f);
        }
    }

    private void SetInteractionPromptVisible(bool visible)
    {
        if (interactionPromptBackground != null)
        {
            interactionPromptBackground.gameObject.SetActive(visible);
        }
        else if (interactionPromptText != null)
        {
            interactionPromptText.gameObject.SetActive(visible);
        }
    }

    private void SetControlsHintVisible(bool visible)
    {
        if (controlsHintBackground != null)
        {
            controlsHintBackground.gameObject.SetActive(visible);
        }
        else if (controlsHintText != null)
        {
            controlsHintText.gameObject.SetActive(visible);
        }
    }

    private void DisableCommonPlayerBehaviours(GameObject targetPlayer)
    {
        if (targetPlayer == null)
        {
            return;
        }

        Behaviour[] behaviours = targetPlayer.GetComponentsInChildren<Behaviour>(true);
        foreach (Behaviour behaviour in behaviours)
        {
            if (behaviour == null)
            {
                continue;
            }

            if (behaviour is SimpleFPSController
                || behaviour is BrowserFpsController
                || behaviour is BrowserPrototypeInteractor)
            {
                DisableBehaviourIfNeeded(behaviour);
            }
        }
    }

    private void DisableBehaviourIfNeeded(Behaviour behaviour)
    {
        if (behaviour == null)
        {
            return;
        }

        for (int i = 0; i < disabledBehaviours.Count; i++)
        {
            if (disabledBehaviours[i].Behaviour == behaviour)
            {
                return;
            }
        }

        disabledBehaviours.Add(new BehaviourState(behaviour, behaviour.enabled));
        behaviour.enabled = false;
    }

    private void CacheWheelBaseRotations()
    {
        if (frontLeftWheel != null)
        {
            frontLeftWheelBaseRotation = frontLeftWheel.localRotation;
        }

        if (frontRightWheel != null)
        {
            frontRightWheelBaseRotation = frontRightWheel.localRotation;
        }

        wheelBaseRotationsCached = frontLeftWheel != null || frontRightWheel != null;
    }

    private void TryAutoAssignPlayer()
    {
        if (playerRoot != null)
        {
            return;
        }

        GameObject taggedPlayer = FindTaggedPlayer();
        if (taggedPlayer != null)
        {
            playerRoot = taggedPlayer;
            return;
        }

        GameObject namedPlayer = FindNamedPlayer();
        if (namedPlayer != null)
        {
            playerRoot = namedPlayer;
        }
    }

    private void TryAutoAssignVisualRoot()
    {
        if (visualRoot != null)
        {
            return;
        }

        Transform namedChild = transform.Find("SF_Model");
        if (namedChild != null)
        {
            visualRoot = namedChild;
            return;
        }

        GameObject namedVisual = GameObject.Find($"{name}_Model");
        if (namedVisual != null)
        {
            visualRoot = namedVisual.transform;
        }
    }

    private void TryAutoAssignWheelReferences()
    {
        if (exitPoint == null)
        {
            exitPoint = transform.Find("ExitPoint");
        }

        if (frontLeftWheel == null)
        {
            frontLeftWheel = transform.Find("FrontLeftWheel");
        }

        if (frontRightWheel == null)
        {
            frontRightWheel = transform.Find("FrontRightWheel");
        }
    }

    private Image CreatePanel(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color backgroundColor)
    {
        GameObject panelObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;

        Image image = panelObject.GetComponent<Image>();
        image.color = backgroundColor;
        image.raycastTarget = false;

        return image;
    }

    private Text CreateText(
        string objectName,
        Transform parent,
        string textValue,
        TextAnchor alignment,
        int fontSize,
        Color textColor)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text), typeof(Outline));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.font = LoadBuiltinFont();
        text.text = textValue;
        text.alignment = alignment;
        text.fontSize = fontSize;
        text.color = textColor;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = textObject.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(1f, -1f);

        return text;
    }

    private static Font LoadBuiltinFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            return font;
        }

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static GameObject FindTaggedPlayer()
    {
        try
        {
            return GameObject.FindGameObjectWithTag("Player");
        }
        catch (UnityException)
        {
            return null;
        }
    }

    private static GameObject FindNamedPlayer()
    {
        GameObject namedPlayer = GameObject.Find("Player");
        if (namedPlayer != null)
        {
            return namedPlayer;
        }

        return GameObject.Find("TestPlayer");
    }

    private static bool HasPlayerTag(Component component)
    {
        if (component == null)
        {
            return false;
        }

        try
        {
            return component.CompareTag("Player");
        }
        catch (UnityException)
        {
            return false;
        }
    }

    private static bool HasSupportedPlayerName(string candidateName)
    {
        return string.Equals(candidateName, "Player", StringComparison.OrdinalIgnoreCase)
            || string.Equals(candidateName, "TestPlayer", StringComparison.OrdinalIgnoreCase);
    }

    private float ReadThrottleInput()
    {
        float input = 0f;

        if (Keyboard.current == null)
        {
            return input;
        }

        if (Keyboard.current.wKey.isPressed)
        {
            input += 1f;
        }

        if (Keyboard.current.sKey.isPressed)
        {
            input -= 1f;
        }

        return Mathf.Clamp(input, -1f, 1f);
    }

    private float ReadSteerInput()
    {
        float input = 0f;

        if (Keyboard.current == null)
        {
            return input;
        }

        if (Keyboard.current.dKey.isPressed)
        {
            input += 1f;
        }

        if (Keyboard.current.aKey.isPressed)
        {
            input -= 1f;
        }

        return Mathf.Clamp(input, -1f, 1f);
    }

    private readonly struct BehaviourState
    {
        public BehaviourState(Behaviour behaviour, bool wasEnabled)
        {
            Behaviour = behaviour;
            WasEnabled = wasEnabled;
        }

        public Behaviour Behaviour { get; }
        public bool WasEnabled { get; }
    }

    private readonly struct ColliderState
    {
        public ColliderState(Collider collider, bool wasEnabled)
        {
            Collider = collider;
            WasEnabled = wasEnabled;
        }

        public Collider Collider { get; }
        public bool WasEnabled { get; }
    }

    private readonly struct GameObjectState
    {
        public GameObjectState(GameObject gameObject, bool wasActive)
        {
            GameObject = gameObject;
            WasActive = wasActive;
        }

        public GameObject GameObject { get; }
        public bool WasActive { get; }
    }
}
