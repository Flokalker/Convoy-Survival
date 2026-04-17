using System;
using ConvoySurvival.Data;
using UnityEngine;

namespace ConvoySurvival.Core
{
    [DisallowMultipleComponent]
    public class PrototypeSessionStateManager : MonoBehaviour
    {
        private static PrototypeSessionStateManager instance;

        [Header("Config")]
        [SerializeField] private TruckUpgradeCatalog upgradeCatalog;
        [SerializeField, Min(0)] private int startingScrap = 120;
        [SerializeField] private TruckSpecialization defaultSpecialization = TruckSpecialization.Tank;

        private readonly CurrencySystem currency = new CurrencySystem();
        private readonly UpgradeSystem upgrades = new UpgradeSystem();
        private readonly PrototypeSaveStateManager saveState = new PrototypeSaveStateManager();

        private bool initialized;
        private float bestDistance;

        public static PrototypeSessionStateManager Instance => instance;

        public CurrencySystem Currency => currency;
        public UpgradeSystem Upgrades => upgrades;
        public PrototypeSaveStateManager SaveState => saveState;
        public TruckUpgradeCatalog UpgradeCatalog => upgradeCatalog;
        public float BestDistance => bestDistance;

        public event Action SessionChanged;

        public static PrototypeSessionStateManager EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            GameObject root = new GameObject("PrototypeSessionState");
            instance = root.AddComponent<PrototypeSessionStateManager>();
            return instance;
        }

        public void SetCatalog(TruckUpgradeCatalog catalog)
        {
            if (catalog == null)
            {
                return;
            }

            upgradeCatalog = catalog;
            SessionChanged?.Invoke();
        }

        public void AddScrap(int amount)
        {
            currency.AddScrap(amount);
            SessionChanged?.Invoke();
        }

        public bool TrySpendScrap(int amount)
        {
            bool success = currency.TrySpendScrap(amount);
            if (success)
            {
                SessionChanged?.Invoke();
            }

            return success;
        }

        public void RecordRunResult(float distance, string reason)
        {
            if (distance > bestDistance)
            {
                bestDistance = distance;
            }

            string summary = string.Format("Last Run: {0:0}m ({1})", Math.Max(0f, distance), reason);
            saveState.SetLastRunSummary(summary);
            saveState.SetBestDistance(bestDistance);
            saveState.Capture(currency.Scrap, upgrades.CreateState(), bestDistance, summary);
            SessionChanged?.Invoke();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                if (instance.upgradeCatalog == null && upgradeCatalog != null)
                {
                    instance.SetCatalog(upgradeCatalog);
                }

                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeIfNeeded();
        }

        private void OnEnable()
        {
            currency.ScrapChanged += HandleCurrencyChanged;
            upgrades.Changed += HandleUpgradesChanged;
        }

        private void OnDisable()
        {
            currency.ScrapChanged -= HandleCurrencyChanged;
            upgrades.Changed -= HandleUpgradesChanged;
        }

        private void InitializeIfNeeded()
        {
            if (initialized)
            {
                return;
            }

            upgrades.Initialize(defaultSpecialization);
            currency.SetScrap(startingScrap);
            saveState.Capture(currency.Scrap, upgrades.CreateState(), bestDistance, string.Empty);
            initialized = true;
            SessionChanged?.Invoke();
        }

        private void HandleCurrencyChanged(int _)
        {
            saveState.Capture(currency.Scrap, upgrades.CreateState(), bestDistance, saveState.Snapshot.LastRunSummary);
        }

        private void HandleUpgradesChanged()
        {
            saveState.Capture(currency.Scrap, upgrades.CreateState(), bestDistance, saveState.Snapshot.LastRunSummary);
            SessionChanged?.Invoke();
        }
    }
}
