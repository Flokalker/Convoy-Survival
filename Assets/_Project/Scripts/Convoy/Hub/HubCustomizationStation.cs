using ConvoySurvival.Core;
using ConvoySurvival.Data;
using UnityEngine;

namespace ConvoySurvival.Hub
{
    [DisallowMultipleComponent]
    public class HubCustomizationStation : HubInteractable
    {
        [SerializeField] private TruckSpecialization specialization = TruckSpecialization.Tank;
        [SerializeField] private bool attemptUpgrade = true;

        public void Configure(TruckSpecialization stationSpecialization, bool shouldAttemptUpgrade)
        {
            specialization = stationSpecialization;
            attemptUpgrade = shouldAttemptUpgrade;
        }

        public override string GetPrompt()
        {
            PrototypeSessionStateManager session = PrototypeSessionStateManager.Instance;
            if (session == null || session.UpgradeCatalog == null)
            {
                return "E = Configure " + specialization;
            }

            int cost = session.Upgrades.GetNextTierCost(session.UpgradeCatalog, specialization);
            string costText = cost > 0 ? cost + " scrap" : "MAX";
            return "E = " + specialization + " station (next: " + costText + ")";
        }

        public override void Interact(HubInteractionController interactor)
        {
            PrototypeSessionStateManager session = PrototypeSessionStateManager.EnsureInstance();
            if (session == null)
            {
                return;
            }

            session.Upgrades.SetActiveSpecialization(specialization);

            string message = specialization + " selected.";
            if (attemptUpgrade && session.UpgradeCatalog != null)
            {
                session.Upgrades.TryPurchaseNextTier(
                    session.UpgradeCatalog,
                    session.Currency,
                    specialization,
                    out string upgradeMessage);

                message = upgradeMessage;
            }

            HubHudController hud = Object.FindAnyObjectByType<HubHudController>();
            if (hud != null)
            {
                hud.PostStatus(message);
            }
        }
    }
}
