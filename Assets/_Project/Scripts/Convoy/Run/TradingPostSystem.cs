using System;
using ConvoySurvival.Core;
using ConvoySurvival.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ConvoySurvival.Run
{
    [DisallowMultipleComponent]
    public class TradingPostSystem : MonoBehaviour
    {
        [SerializeField] private TruckController truck;

        private PrototypeSessionStateManager session;
        private string statusMessage = string.Empty;

        public bool IsOpen { get; private set; }
        public event Action Opened;
        public event Action Closed;

        public string CurrentUiText
        {
            get
            {
                if (!IsOpen)
                {
                    return string.Empty;
                }

                if (session == null || session.UpgradeCatalog == null)
                {
                    return "Trading post unavailable.";
                }

                TruckSpecialization active = session.Upgrades.ActiveSpecialization;
                SpecializationPath path = session.UpgradeCatalog.GetPath(active);
                int cost = session.Upgrades.GetNextTierCost(session.UpgradeCatalog, active);
                string costText = cost > 0 ? cost + " scrap" : "MAX";
                string feature = path != null && !string.IsNullOrWhiteSpace(path.KeyFeature)
                    ? path.KeyFeature
                    : "No feature listed";
                string weakness = path != null && !string.IsNullOrWhiteSpace(path.Weakness)
                    ? path.Weakness
                    : "No weakness listed";

                return "Trading Post\n" +
                       "Path: " + active + "\n" +
                       "Feature: " + feature + "\n" +
                       "Weakness: " + weakness + "\n" +
                       "1 = Upgrade current path (" + costText + ")\n" +
                       "2 = Tank, 3 = Scout, 4 = Fortress\n" +
                       "Enter = Open gate / continue run\n" +
                       "Scrap: " + session.Currency.Scrap + "\n" +
                       statusMessage;
            }
        }

        public void Configure(TruckController truckController)
        {
            truck = truckController;
        }

        private void Start()
        {
            session = PrototypeSessionStateManager.Instance;
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                TryBuyUpgrade();
            }

            if (keyboard.digit2Key.wasPressedThisFrame)
            {
                SetSpecialization(TruckSpecialization.Tank);
            }

            if (keyboard.digit3Key.wasPressedThisFrame)
            {
                SetSpecialization(TruckSpecialization.Scout);
            }

            if (keyboard.digit4Key.wasPressedThisFrame)
            {
                SetSpecialization(TruckSpecialization.Fortress);
            }

            if (keyboard.enterKey.wasPressedThisFrame)
            {
                CloseTradingPost();
            }
        }

        public void OpenTradingPost()
        {
            if (IsOpen)
            {
                return;
            }

            IsOpen = true;
            statusMessage = "Select an option.";
            if (truck != null)
            {
                truck.SetMovementEnabled(false);
            }

            Opened?.Invoke();
        }

        public void CloseTradingPost()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            statusMessage = string.Empty;
            if (truck != null)
            {
                truck.SetMovementEnabled(true);
            }

            Closed?.Invoke();
        }

        private void TryBuyUpgrade()
        {
            if (session == null || session.UpgradeCatalog == null)
            {
                statusMessage = "No upgrade catalog.";
                return;
            }

            TruckSpecialization active = session.Upgrades.ActiveSpecialization;
            bool success = session.Upgrades.TryPurchaseNextTier(
                session.UpgradeCatalog,
                session.Currency,
                active,
                out string message);

            statusMessage = message;
            if (success)
            {
                ApplyCurrentTierToTruck();
            }
        }

        private void SetSpecialization(TruckSpecialization specialization)
        {
            if (session == null)
            {
                return;
            }

            session.Upgrades.SetActiveSpecialization(specialization);
            statusMessage = specialization + " selected.";
            ApplyCurrentTierToTruck();
        }

        private void ApplyCurrentTierToTruck()
        {
            if (session == null || session.UpgradeCatalog == null || truck == null)
            {
                return;
            }

            TruckSpecialization specialization = session.Upgrades.ActiveSpecialization;
            TruckUpgradeTier tier = session.Upgrades.GetActiveTier(session.UpgradeCatalog);
            truck.ApplyTier(specialization, tier);
        }
    }
}
