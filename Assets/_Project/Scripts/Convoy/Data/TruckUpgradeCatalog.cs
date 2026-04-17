using System;
using UnityEngine;

namespace ConvoySurvival.Data
{
    [CreateAssetMenu(menuName = "Convoy Survival/Truck Upgrade Catalog", fileName = "TruckUpgradeCatalog")]
    public class TruckUpgradeCatalog : ScriptableObject
    {
        [SerializeField] private SpecializationPath[] paths = Array.Empty<SpecializationPath>();

        public SpecializationPath[] Paths => paths;

        public void SetPaths(SpecializationPath[] newPaths)
        {
            paths = newPaths ?? Array.Empty<SpecializationPath>();
        }

        public SpecializationPath GetPath(TruckSpecialization specialization)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                if (paths[i] != null && paths[i].Specialization == specialization)
                {
                    return paths[i];
                }
            }

            return null;
        }

        public TruckUpgradeTier GetTier(TruckSpecialization specialization, int tierIndex)
        {
            SpecializationPath path = GetPath(specialization);
            if (path == null || path.Tiers == null || path.Tiers.Length == 0)
            {
                return TruckUpgradeTier.Default;
            }

            int clamped = Mathf.Clamp(tierIndex, 0, path.Tiers.Length - 1);
            return path.Tiers[clamped];
        }

        public int GetMaxTierIndex(TruckSpecialization specialization)
        {
            SpecializationPath path = GetPath(specialization);
            if (path == null || path.Tiers == null || path.Tiers.Length == 0)
            {
                return 0;
            }

            return path.Tiers.Length - 1;
        }
    }

    [Serializable]
    public class SpecializationPath
    {
        [SerializeField] public TruckSpecialization specialization;
        [SerializeField] public string displayName = "Path";
        [SerializeField, TextArea(1, 3)] public string keyFeature = string.Empty;
        [SerializeField, TextArea(1, 3)] public string weakness = string.Empty;
        [SerializeField] public TruckUpgradeTier[] tiers = Array.Empty<TruckUpgradeTier>();

        public TruckSpecialization Specialization => specialization;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? specialization.ToString() : displayName;
        public string KeyFeature => keyFeature;
        public string Weakness => weakness;
        public TruckUpgradeTier[] Tiers => tiers;
    }

    [Serializable]
    public struct TruckUpgradeTier
    {
        public static TruckUpgradeTier Default => new TruckUpgradeTier
        {
            TierName = "Stock",
            ScrapCost = 0,
            MaxDurability = 100f,
            MaxFuel = 100f,
            ForwardSpeed = 25f,
            LaneChangeSpeed = 7f,
            FuelDrainPerSecond = 1f,
            CollisionDamageMultiplier = 1f,
            ZombieKillSpeed = 15f,
            NitroSpeedMultiplier = 1.4f,
            NitroFuelDrainMultiplier = 2f,
            SteeringFactor = 1f,
            TruckScale = 1f,
            HasSpikes = false,
            HasPlow = false,
            HasTurret = false,
            HasNitro = false,
            ExtraTurretDamagePerSecond = 0f
        };

        public string TierName;
        public int ScrapCost;
        public float MaxDurability;
        public float MaxFuel;
        public float ForwardSpeed;
        public float LaneChangeSpeed;
        public float FuelDrainPerSecond;
        public float CollisionDamageMultiplier;
        public float ZombieKillSpeed;
        public float NitroSpeedMultiplier;
        public float NitroFuelDrainMultiplier;
        public float SteeringFactor;
        public float TruckScale;
        public bool HasSpikes;
        public bool HasPlow;
        public bool HasTurret;
        public bool HasNitro;
        public float ExtraTurretDamagePerSecond;
    }
}
