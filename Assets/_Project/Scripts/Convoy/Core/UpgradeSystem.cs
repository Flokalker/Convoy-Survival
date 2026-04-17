using System;
using ConvoySurvival.Data;

namespace ConvoySurvival.Core
{
    [Serializable]
    public class UpgradeSystem
    {
        [Serializable]
        public struct UpgradeState
        {
            public TruckSpecialization ActiveSpecialization;
            public int TankTier;
            public int ScoutTier;
            public int FortressTier;
        }

        private UpgradeState state;

        public event Action Changed;

        public TruckSpecialization ActiveSpecialization => state.ActiveSpecialization;

        public void Initialize(TruckSpecialization defaultSpecialization)
        {
            state.ActiveSpecialization = defaultSpecialization;
            state.TankTier = Math.Max(0, state.TankTier);
            state.ScoutTier = Math.Max(0, state.ScoutTier);
            state.FortressTier = Math.Max(0, state.FortressTier);
        }

        public void SetActiveSpecialization(TruckSpecialization specialization)
        {
            if (state.ActiveSpecialization == specialization)
            {
                return;
            }

            state.ActiveSpecialization = specialization;
            Changed?.Invoke();
        }

        public int GetTierIndex(TruckSpecialization specialization)
        {
            return specialization switch
            {
                TruckSpecialization.Tank => state.TankTier,
                TruckSpecialization.Scout => state.ScoutTier,
                TruckSpecialization.Fortress => state.FortressTier,
                _ => 0
            };
        }

        public TruckUpgradeTier GetActiveTier(TruckUpgradeCatalog catalog)
        {
            return GetTier(catalog, state.ActiveSpecialization);
        }

        public TruckUpgradeTier GetTier(TruckUpgradeCatalog catalog, TruckSpecialization specialization)
        {
            if (catalog == null)
            {
                return TruckUpgradeTier.Default;
            }

            return catalog.GetTier(specialization, GetTierIndex(specialization));
        }

        public int GetNextTierCost(TruckUpgradeCatalog catalog, TruckSpecialization specialization)
        {
            if (catalog == null)
            {
                return 0;
            }

            int currentTier = GetTierIndex(specialization);
            int maxTier = catalog.GetMaxTierIndex(specialization);
            if (currentTier >= maxTier)
            {
                return 0;
            }

            return Math.Max(0, catalog.GetTier(specialization, currentTier + 1).ScrapCost);
        }

        public bool TryPurchaseNextTier(
            TruckUpgradeCatalog catalog,
            CurrencySystem currency,
            TruckSpecialization specialization,
            out string message)
        {
            message = string.Empty;
            if (catalog == null)
            {
                message = "Missing upgrade catalog.";
                return false;
            }

            int currentTier = GetTierIndex(specialization);
            int maxTier = catalog.GetMaxTierIndex(specialization);
            if (currentTier >= maxTier)
            {
                message = specialization + " path is already max tier.";
                return false;
            }

            int nextTier = currentTier + 1;
            TruckUpgradeTier tierData = catalog.GetTier(specialization, nextTier);
            int cost = Math.Max(0, tierData.ScrapCost);
            if (currency != null && !currency.TrySpendScrap(cost))
            {
                message = "Not enough scrap.";
                return false;
            }

            SetTierIndex(specialization, nextTier);
            message = specialization + " upgraded to " + tierData.TierName + ".";
            return true;
        }

        public UpgradeState CreateState()
        {
            return state;
        }

        public void ApplyState(UpgradeState newState)
        {
            state = newState;
            state.TankTier = Math.Max(0, state.TankTier);
            state.ScoutTier = Math.Max(0, state.ScoutTier);
            state.FortressTier = Math.Max(0, state.FortressTier);
            Changed?.Invoke();
        }

        private void SetTierIndex(TruckSpecialization specialization, int tier)
        {
            int value = Math.Max(0, tier);
            switch (specialization)
            {
                case TruckSpecialization.Tank:
                    state.TankTier = value;
                    break;
                case TruckSpecialization.Scout:
                    state.ScoutTier = value;
                    break;
                case TruckSpecialization.Fortress:
                    state.FortressTier = value;
                    break;
            }

            Changed?.Invoke();
        }
    }
}
