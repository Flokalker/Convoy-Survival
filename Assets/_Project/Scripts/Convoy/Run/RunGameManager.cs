using ConvoySurvival.Core;
using ConvoySurvival.Run.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace ConvoySurvival.Run
{
    [DisallowMultipleComponent]
    public class RunGameManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TruckController truck;
        [SerializeField] private RoadSegmentSpawner roadSpawner;
        [SerializeField] private ConvoyObjectiveSystem objectiveSystem;
        [SerializeField] private TradingPostSystem tradingPostSystem;
        [SerializeField] private RunHudController hud;

        [Header("Scenes")]
        [SerializeField] private string hubSceneName = "Hub";
        [SerializeField] private bool showFallbackOverlay = true;

        private PrototypeSessionStateManager session;
        private float startZ;
        private bool runEnded;
        private GUIStyle overlayStyle;

        public float DistanceMeters { get; private set; }

        public void Configure(
            TruckController truckController,
            RoadSegmentSpawner spawner,
            ConvoyObjectiveSystem objectives,
            TradingPostSystem trading,
            RunHudController runHud)
        {
            truck = truckController;
            roadSpawner = spawner;
            objectiveSystem = objectives;
            tradingPostSystem = trading;
            hud = runHud;
        }

        public void SetHubSceneName(string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                hubSceneName = sceneName;
            }
        }

        private void Start()
        {
            session = PrototypeSessionStateManager.EnsureInstance();
            startZ = truck != null ? truck.transform.position.z : 0f;
            if (hud == null)
            {
                hud = Object.FindAnyObjectByType<RunHudController>();
            }

            ApplySessionUpgradeToTruck();

            if (roadSpawner != null && truck != null)
            {
                roadSpawner.ConfigureLaneSpacingOnTruck();
            }

            if (truck != null)
            {
                HealthDurabilitySystem durability = truck.GetComponent<HealthDurabilitySystem>();
                if (durability != null)
                {
                    durability.Destroyed += HandleTruckDestroyed;
                }
            }

            if (hud != null)
            {
                hud.Configure(this, truck, objectiveSystem, tradingPostSystem);
            }

            LockCursor(true);
        }

        private void OnDestroy()
        {
            if (truck != null)
            {
                HealthDurabilitySystem durability = truck.GetComponent<HealthDurabilitySystem>();
                if (durability != null)
                {
                    durability.Destroyed -= HandleTruckDestroyed;
                }
            }
        }

        private void Update()
        {
            if (runEnded || truck == null)
            {
                return;
            }

            DistanceMeters = Mathf.Max(0f, truck.transform.position.z - startZ);

            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    LockCursor(false);
                }

                if (keyboard.backspaceKey.wasPressedThisFrame)
                {
                    FinishRun("manual return");
                }
            }

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                LockCursor(true);
            }
        }

        public void ReapplyCurrentTruckTier()
        {
            ApplySessionUpgradeToTruck();
        }

        private void HandleTruckDestroyed()
        {
            FinishRun("truck destroyed");
        }

        private void FinishRun(string reason)
        {
            if (runEnded)
            {
                return;
            }

            runEnded = true;
            if (session != null)
            {
                session.RecordRunResult(DistanceMeters, reason);
            }

            LockCursor(false);
            if (Application.CanStreamedLevelBeLoaded(hubSceneName))
            {
                SceneManager.LoadScene(hubSceneName);
                return;
            }

            Debug.LogError("Hub scene '" + hubSceneName + "' is not loadable. Add it to Build Settings or run the bootstrap tool.");
            runEnded = false;
            if (truck != null)
            {
                truck.SetMovementEnabled(true);
            }
        }

        private void ApplySessionUpgradeToTruck()
        {
            if (session == null || truck == null || session.UpgradeCatalog == null)
            {
                return;
            }

            var specialization = session.Upgrades.ActiveSpecialization;
            var tier = session.Upgrades.GetActiveTier(session.UpgradeCatalog);
            truck.ApplyTier(specialization, tier);
        }

        private static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void OnGUI()
        {
            bool hasActiveHud = hud != null && hud.isActiveAndEnabled && hud.gameObject.activeInHierarchy;
            if (!showFallbackOverlay || hasActiveHud)
            {
                return;
            }

            if (overlayStyle == null)
            {
                overlayStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    normal = { textColor = Color.white },
                    wordWrap = true
                };
            }

            string objective = objectiveSystem != null ? objectiveSystem.CurrentObjectiveText : "No objective system";
            string fuel = truck != null ? truck.CurrentFuel.ToString("0") + "/" + truck.MaxFuel.ToString("0") : "-";
            string trading = tradingPostSystem != null && tradingPostSystem.IsOpen
                ? "Trading post active: press Enter/E/Space to continue"
                : "Backspace = return to hub";

            Rect box = new Rect(12f, 12f, 700f, 150f);
            GUI.Box(box, string.Empty);
            GUI.Label(
                new Rect(24f, 20f, 680f, 136f),
                "Distance: " + DistanceMeters.ToString("0") + "m\n" +
                "Fuel: " + fuel + "\n" +
                "Objective: " + objective + "\n" +
                trading,
                overlayStyle);
        }
    }
}
