using ConvoySurvival.Core;
using ConvoySurvival.Data;
using ConvoySurvival.Run;
using UnityEngine;

namespace ConvoySurvival.Hub
{
    [DisallowMultipleComponent]
    public class HubParkedTruckPreview : MonoBehaviour
    {
        [SerializeField] private TruckVisualAdapter visualAdapter;

        private PrototypeSessionStateManager session;
        private TruckSpecialization lastSpecialization;
        private int lastTier = -1;

        private void Awake()
        {
            if (visualAdapter == null)
            {
                visualAdapter = GetComponentInChildren<TruckVisualAdapter>();
            }
        }

        private void Start()
        {
            session = PrototypeSessionStateManager.EnsureInstance();
            ApplyIfChanged(true);
        }

        private void Update()
        {
            ApplyIfChanged(false);
        }

        private void ApplyIfChanged(bool force)
        {
            if (session == null || session.UpgradeCatalog == null || visualAdapter == null)
            {
                return;
            }

            TruckSpecialization specialization = session.Upgrades.ActiveSpecialization;
            int tierIndex = session.Upgrades.GetTierIndex(specialization);

            if (!force && specialization == lastSpecialization && tierIndex == lastTier)
            {
                return;
            }

            TruckUpgradeTier tier = session.Upgrades.GetTier(session.UpgradeCatalog, specialization);
            visualAdapter.ApplyTier(tier);
            lastSpecialization = specialization;
            lastTier = tierIndex;
        }
    }
}
