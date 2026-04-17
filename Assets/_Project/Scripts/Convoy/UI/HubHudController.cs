using ConvoySurvival.Core;
using ConvoySurvival.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ConvoySurvival.Hub
{
    [DisallowMultipleComponent]
    public class HubHudController : MonoBehaviour
    {
        [SerializeField] private HubInteractionController interactionController;
        [SerializeField] private Text scrapText;
        [SerializeField] private Text specializationText;
        [SerializeField] private Text promptText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text helpText;

        private PrototypeSessionStateManager session;
        private float statusTimer;

        public void Configure(
            HubInteractionController interaction,
            Text scrap,
            Text specialization,
            Text prompt,
            Text status,
            Text help)
        {
            interactionController = interaction;
            scrapText = scrap;
            specializationText = specialization;
            promptText = prompt;
            statusText = status;
            helpText = help;
        }

        private void Start()
        {
            session = PrototypeSessionStateManager.EnsureInstance();
            if (helpText != null)
            {
                helpText.text =
                    "Hub Controls:\n" +
                    "WASD = Move, Mouse = Look, E = Interact\n" +
                    "Shops: Mechanic (Tank), Radio (Scout), Armory (Fortress)\n" +
                    "Stations: choose path + buy upgrades with scrap\n" +
                    "Enter truck to start MainRun";
            }

            if (session != null)
            {
                string summary = session.SaveState.Snapshot.LastRunSummary;
                if (!string.IsNullOrWhiteSpace(summary))
                {
                    PostStatus(summary);
                }
            }
        }

        private void Update()
        {
            if (session != null)
            {
                if (scrapText != null)
                {
                    scrapText.text = "Scrap: " + session.Currency.Scrap;
                }

                if (specializationText != null)
                {
                    var specialization = session.Upgrades.ActiveSpecialization;
                    int tier = session.Upgrades.GetTierIndex(specialization) + 1;
                    SpecializationPath path = session.UpgradeCatalog != null
                        ? session.UpgradeCatalog.GetPath(specialization)
                        : null;

                    string feature = path != null && !string.IsNullOrWhiteSpace(path.KeyFeature)
                        ? path.KeyFeature
                        : "Unknown feature";
                    string weakness = path != null && !string.IsNullOrWhiteSpace(path.Weakness)
                        ? path.Weakness
                        : "Unknown weakness";

                    specializationText.text = "Truck Path: " + specialization + " T" + tier +
                                              "\nFeature: " + feature +
                                              "\nWeakness: " + weakness +
                                              "\nBest Distance: " + session.BestDistance.ToString("0") + " m";
                }
            }

            if (promptText != null && interactionController != null)
            {
                promptText.text = interactionController.CurrentPrompt;
            }

            if (statusTimer > 0f)
            {
                statusTimer -= Time.deltaTime;
                if (statusTimer <= 0f && statusText != null)
                {
                    statusText.text = string.Empty;
                }
            }
        }

        public void PostStatus(string message, float duration = 4f)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = message;
            statusTimer = Mathf.Max(1f, duration);
        }
    }
}
