using System;
using ConvoySurvival.Core;
using ConvoySurvival.Data;
using ConvoySurvival.Run.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ConvoySurvival.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(HealthDurabilitySystem))]
    public class TruckController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody truckRigidbody;
        [SerializeField] private HealthDurabilitySystem durabilitySystem;
        [SerializeField] private TruckVisualAdapter visualAdapter;

        [Header("Movement")]
        [SerializeField, Min(1f)] private float laneWidth = 3.5f;
        [SerializeField, Min(1)] private int maxLaneIndex = 1;
        [SerializeField, Min(1f)] private float baseForwardSpeed = 24f;
        [SerializeField, Min(1f)] private float baseLaneChangeSpeed = 9f;

        [Header("Fuel")]
        [SerializeField, Min(10f)] private float maxFuel = 100f;
        [SerializeField, Min(0.1f)] private float fuelDrainPerSecond = 1f;
        [SerializeField, Min(0f)] private float outOfFuelDamagePerSecond = 8f;

        [Header("Combat")]
        [SerializeField] private float collisionDamageMultiplier = 1f;
        [SerializeField] private float zombieKillSpeed = 16f;
        [SerializeField] private bool hasSpikes;
        [SerializeField] private bool hasPlow;
        [SerializeField] private bool hasTurret;
        [SerializeField] private bool hasNitro;
        [SerializeField] private float nitroSpeedMultiplier = 1.35f;
        [SerializeField] private float nitroFuelDrainMultiplier = 2f;
        [SerializeField] private float turretDamagePerSecond;

        private int targetLane;
        private bool movementEnabled = true;
        private float currentFuel;
        private float currentForwardSpeed;
        private float currentLaneChangeSpeed;
        private TruckSpecialization currentSpecialization;

        public event Action<float, float> FuelChanged;
        public event Action<TruckSpecialization, string> TierApplied;

        public float CurrentFuel => currentFuel;
        public float MaxFuel => maxFuel;
        public float CurrentSpeed => currentForwardSpeed;
        public bool IsDestroyed => durabilitySystem != null && durabilitySystem.IsDestroyed;
        public bool HasRamParts => hasSpikes || hasPlow;
        public TruckSpecialization CurrentSpecialization => currentSpecialization;

        private void Reset()
        {
            truckRigidbody = GetComponent<Rigidbody>();
            durabilitySystem = GetComponent<HealthDurabilitySystem>();
            visualAdapter = GetComponentInChildren<TruckVisualAdapter>();
        }

        private void Awake()
        {
            if (truckRigidbody == null)
            {
                truckRigidbody = GetComponent<Rigidbody>();
            }

            if (durabilitySystem == null)
            {
                durabilitySystem = GetComponent<HealthDurabilitySystem>();
            }

            if (visualAdapter == null)
            {
                visualAdapter = GetComponentInChildren<TruckVisualAdapter>();
            }

            truckRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            truckRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            truckRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            truckRigidbody.useGravity = false;
            truckRigidbody.isKinematic = true;

            currentFuel = maxFuel;
            currentForwardSpeed = baseForwardSpeed;
            currentLaneChangeSpeed = baseLaneChangeSpeed;
            FuelChanged?.Invoke(currentFuel, maxFuel);
        }

        private void Update()
        {
            if (durabilitySystem != null && durabilitySystem.IsDestroyed)
            {
                return;
            }

            HandleLaneInput();
            DrainFuel();
            HandleTurretPulse();
        }

        private void FixedUpdate()
        {
            if (durabilitySystem != null && durabilitySystem.IsDestroyed)
            {
                return;
            }

            float speed = movementEnabled ? ResolveCurrentForwardSpeed() : 0f;
            float targetX = Mathf.Clamp(targetLane, -maxLaneIndex, maxLaneIndex) * laneWidth;
            Vector3 currentPosition = truckRigidbody.position;
            float nextX = Mathf.MoveTowards(currentPosition.x, targetX, currentLaneChangeSpeed * Time.fixedDeltaTime);

            Vector3 nextPosition = new Vector3(nextX, currentPosition.y, currentPosition.z + speed * Time.fixedDeltaTime);
            truckRigidbody.MovePosition(nextPosition);
            currentForwardSpeed = speed;
        }

        public void ApplyTier(TruckSpecialization specialization, TruckUpgradeTier tier)
        {
            currentSpecialization = specialization;
            baseForwardSpeed = Mathf.Max(5f, tier.ForwardSpeed);
            baseLaneChangeSpeed = Mathf.Max(2f, tier.LaneChangeSpeed * Mathf.Max(0.2f, tier.SteeringFactor));
            maxFuel = Mathf.Max(10f, tier.MaxFuel);
            fuelDrainPerSecond = Mathf.Max(0.1f, tier.FuelDrainPerSecond);
            collisionDamageMultiplier = Mathf.Max(0.2f, tier.CollisionDamageMultiplier);
            zombieKillSpeed = Mathf.Max(4f, tier.ZombieKillSpeed);
            hasSpikes = tier.HasSpikes;
            hasPlow = tier.HasPlow;
            hasTurret = tier.HasTurret;
            hasNitro = tier.HasNitro;
            nitroSpeedMultiplier = Mathf.Max(1f, tier.NitroSpeedMultiplier);
            nitroFuelDrainMultiplier = Mathf.Max(1f, tier.NitroFuelDrainMultiplier);
            turretDamagePerSecond = Mathf.Max(0f, tier.ExtraTurretDamagePerSecond);

            currentLaneChangeSpeed = baseLaneChangeSpeed;
            currentForwardSpeed = baseForwardSpeed;

            if (durabilitySystem != null)
            {
                durabilitySystem.ResetDurability(Mathf.Max(1f, tier.MaxDurability));
            }

            currentFuel = maxFuel;
            FuelChanged?.Invoke(currentFuel, maxFuel);
            visualAdapter?.ApplyTier(tier);
            TierApplied?.Invoke(specialization, tier.TierName);
        }

        public void SetMovementEnabled(bool value)
        {
            movementEnabled = value;
            if (!movementEnabled)
            {
                currentForwardSpeed = 0f;
            }
        }

        public void ConfigureLaneSettings(float width, int laneCountEachSide)
        {
            laneWidth = Mathf.Max(1f, width);
            maxLaneIndex = Mathf.Clamp(laneCountEachSide, 1, 3);
            targetLane = Mathf.Clamp(targetLane, -maxLaneIndex, maxLaneIndex);
        }

        public void ApplyImpactDamage(float baseDamage)
        {
            if (durabilitySystem == null)
            {
                return;
            }

            float amount = Mathf.Max(0.1f, baseDamage) * collisionDamageMultiplier;
            durabilitySystem.ApplyDamage(amount);
        }

        public void RefillFuel(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            currentFuel = Mathf.Min(maxFuel, currentFuel + amount);
            FuelChanged?.Invoke(currentFuel, maxFuel);
        }

        public bool CanRamZombie(float speedRequirement)
        {
            return CurrentSpeed >= Mathf.Min(zombieKillSpeed, speedRequirement) || HasRamParts;
        }

        private void HandleLaneInput()
        {
            if (!movementEnabled)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            {
                targetLane = Mathf.Max(-maxLaneIndex, targetLane - 1);
            }

            if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                targetLane = Mathf.Min(maxLaneIndex, targetLane + 1);
            }
        }

        private float ResolveCurrentForwardSpeed()
        {
            if (currentFuel <= 0f)
            {
                return Mathf.Max(8f, baseForwardSpeed * 0.45f);
            }

            float multiplier = 1f;
            if (hasNitro)
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && keyboard.leftShiftKey.isPressed)
                {
                    multiplier = nitroSpeedMultiplier;
                }
            }

            return baseForwardSpeed * multiplier;
        }

        private void DrainFuel()
        {
            if (!movementEnabled)
            {
                return;
            }

            float drain = fuelDrainPerSecond;
            if (hasNitro)
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && keyboard.leftShiftKey.isPressed)
                {
                    drain *= nitroFuelDrainMultiplier;
                }
            }

            currentFuel = Mathf.Max(0f, currentFuel - drain * Time.deltaTime);
            FuelChanged?.Invoke(currentFuel, maxFuel);

            if (currentFuel <= 0f && durabilitySystem != null)
            {
                durabilitySystem.ApplyDamage(outOfFuelDamagePerSecond * Time.deltaTime);
            }
        }

        private void HandleTurretPulse()
        {
            if (!hasTurret || turretDamagePerSecond <= 0f)
            {
                return;
            }

            Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.forward * 7f, 5f, ~0, QueryTriggerInteraction.Ignore);
            float damagePerTick = turretDamagePerSecond * Time.deltaTime;

            for (int i = 0; i < hits.Length; i++)
            {
                ZombieTarget zombie = hits[i].GetComponentInParent<ZombieTarget>();
                if (zombie == null)
                {
                    continue;
                }

                zombie.ApplyTurretDamage(damagePerTick);
                break;
            }
        }
    }
}

