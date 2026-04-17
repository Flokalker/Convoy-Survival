using UnityEngine;

public static class FirstPersonSceneBootstrap
{
    private const float DefaultEyeHeight = 1.6f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureFirstPersonController()
    {
        BrowserFpsController existingController = Object.FindFirstObjectByType<BrowserFpsController>();
        if (existingController != null)
        {
            EnsureCarInteraction(existingController.gameObject);
            return;
        }

        Camera sourceCamera = Camera.main;
        if (sourceCamera == null)
        {
            sourceCamera = Object.FindFirstObjectByType<Camera>();
        }

        Vector3 spawnPosition = sourceCamera != null
            ? sourceCamera.transform.position - new Vector3(0f, DefaultEyeHeight, 0f)
            : new Vector3(0f, 2f, 0f);

        Quaternion spawnRotation = Quaternion.Euler(0f, sourceCamera != null ? sourceCamera.transform.eulerAngles.y : 0f, 0f);

        GameObject player = new GameObject("AutoPlayer");
        player.transform.SetPositionAndRotation(spawnPosition, spawnRotation);

        CharacterController characterController = player.AddComponent<CharacterController>();
        characterController.height = 1.8f;
        characterController.radius = 0.35f;
        characterController.center = new Vector3(0f, 0.9f, 0f);
        characterController.stepOffset = 0.3f;
        characterController.slopeLimit = 45f;

        Transform cameraRoot = new GameObject("CameraRoot").transform;
        cameraRoot.SetParent(player.transform, false);
        cameraRoot.localPosition = new Vector3(0f, DefaultEyeHeight, 0f);

        Camera playerCamera = sourceCamera;
        if (playerCamera == null)
        {
            GameObject cameraObject = new GameObject("PlayerCamera");
            playerCamera = cameraObject.AddComponent<Camera>();
            playerCamera.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
        }

        playerCamera.transform.SetParent(cameraRoot, false);
        playerCamera.transform.localPosition = Vector3.zero;
        playerCamera.transform.localRotation = Quaternion.identity;

        if (sourceCamera != null)
        {
            float pitch = sourceCamera.transform.eulerAngles.x;
            if (pitch > 180f)
            {
                pitch -= 360f;
            }

            cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        Transform groundCheck = new GameObject("GroundCheck").transform;
        groundCheck.SetParent(player.transform, false);
        groundCheck.localPosition = new Vector3(0f, 0.1f, 0f);

        BrowserFpsController controller = player.AddComponent<BrowserFpsController>();
        controller.ConfigureReferences(cameraRoot, groundCheck, null);
        EnsureCarInteraction(player);
    }

    private static void EnsureCarInteraction(GameObject playerObject)
    {
        if (playerObject.GetComponent<PlayerInteract>() == null)
        {
            playerObject.AddComponent<PlayerInteract>();
        }
    }
}
