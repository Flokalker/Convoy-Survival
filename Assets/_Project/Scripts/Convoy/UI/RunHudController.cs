using ConvoySurvival.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ConvoySurvival.Run
{
    [DisallowMultipleComponent]
    public class RunHudController : MonoBehaviour
    {
        [SerializeField] private Text distanceText;
        [SerializeField] private Text scrapText;
        [SerializeField] private Text durabilityText;
        [SerializeField] private Text fuelText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text specializationText;
        [SerializeField] private Text tradingText;

        private RunGameManager runManager;
        private TruckController truck;
        private ConvoyObjectiveSystem objectiveSystem;
        private TradingPostSystem tradingPostSystem;
        private PrototypeSessionStateManager session;
        private HealthDurabilitySystem durability;

        public void ConfigureTextElements(
            Text distance,
            Text scrap,
            Text durabilityValue,
            Text fuel,
            Text objective,
            Text specialization,
            Text trading)
        {
            distanceText = distance;
            scrapText = scrap;
            durabilityText = durabilityValue;
            fuelText = fuel;
            objectiveText = objective;
            specializationText = specialization;
            tradingText = trading;
        }

        public void Configure(
            RunGameManager manager,
            TruckController truckController,
            ConvoyObjectiveSystem objectives,
            TradingPostSystem trading)
        {
            runManager = manager;
            truck = truckController;
            objectiveSystem = objectives;
            tradingPostSystem = trading;
        }

        private void Start()
        {
            session = PrototypeSessionStateManager.Instance;
            durability = truck != null ? truck.GetComponent<HealthDurabilitySystem>() : null;
        }

        private void Update()
        {
            if (distanceText != null && runManager != null)
            {
                distanceText.text = "Distance: " + runManager.DistanceMeters.ToString("0") + " m";
            }

            if (scrapText != null && session != null)
            {
                scrapText.text = "Scrap: " + session.Currency.Scrap;
            }

            if (durabilityText != null && durability != null)
            {
                durabilityText.text = "Durability: " + durability.CurrentDurability.ToString("0") + "/" + durability.MaxDurability.ToString("0");
            }

            if (fuelText != null && truck != null)
            {
                fuelText.text = "Fuel: " + truck.CurrentFuel.ToString("0") + "/" + truck.MaxFuel.ToString("0");
            }

            if (objectiveText != null && objectiveSystem != null)
            {
                objectiveText.text = "Objective: " + objectiveSystem.CurrentObjectiveText;
            }

            if (specializationText != null && session != null)
            {
                var specialization = session.Upgrades.ActiveSpecialization;
                int tier = session.Upgrades.GetTierIndex(specialization) + 1;
                specializationText.text = "Path: " + specialization + " T" + tier;
            }

            if (tradingText != null && tradingPostSystem != null)
            {
                bool visible = tradingPostSystem.IsOpen;
                tradingText.gameObject.SetActive(visible);
                if (visible)
                {
                    tradingText.text = tradingPostSystem.CurrentUiText;
                }
            }
        }
    }
}
