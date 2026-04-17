using UnityEngine;

namespace ConvoySurvival.Run.World
{
    [DisallowMultipleComponent]
    public class TradingPostZone : MonoBehaviour
    {
        private TradingPostSystem tradingPostSystem;
        private TradingGateController gateController;
        private bool activated;

        public void Configure(TradingPostSystem system, TradingGateController gate)
        {
            Unsubscribe();

            tradingPostSystem = system;
            gateController = gate;

            if (tradingPostSystem != null)
            {
                tradingPostSystem.Opened += HandleTradingOpened;
                tradingPostSystem.Closed += HandleTradingClosed;
            }

            gateController?.SetBlocked(true);
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (activated)
            {
                return;
            }

            TruckController truck = other.GetComponentInParent<TruckController>();
            if (truck == null)
            {
                return;
            }

            activated = true;
            gateController?.SetBlocked(true);
            tradingPostSystem?.OpenTradingPost();
        }

        private void HandleTradingOpened()
        {
            gateController?.SetBlocked(true);
        }

        private void HandleTradingClosed()
        {
            gateController?.SetBlocked(false);
        }

        private void Unsubscribe()
        {
            if (tradingPostSystem == null)
            {
                return;
            }

            tradingPostSystem.Opened -= HandleTradingOpened;
            tradingPostSystem.Closed -= HandleTradingClosed;
        }
    }
}
