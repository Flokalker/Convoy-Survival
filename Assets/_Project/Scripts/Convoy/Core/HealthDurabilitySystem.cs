using System;
using UnityEngine;

namespace ConvoySurvival.Core
{
    [DisallowMultipleComponent]
    public class HealthDurabilitySystem : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxDurability = 100f;

        private float currentDurability;
        private bool destroyed;

        public event Action<float, float> DurabilityChanged;
        public event Action Destroyed;

        public float MaxDurability => maxDurability;
        public float CurrentDurability => currentDurability;
        public bool IsDestroyed => destroyed;

        private void Awake()
        {
            ResetDurability(maxDurability);
        }

        public void Configure(float newMaxDurability)
        {
            maxDurability = Mathf.Max(1f, newMaxDurability);
            currentDurability = Mathf.Min(currentDurability, maxDurability);
            DurabilityChanged?.Invoke(currentDurability, maxDurability);
        }

        public void ResetDurability(float newMaxDurability)
        {
            maxDurability = Mathf.Max(1f, newMaxDurability);
            currentDurability = maxDurability;
            destroyed = false;
            DurabilityChanged?.Invoke(currentDurability, maxDurability);
        }

        public void Repair(float amount)
        {
            if (destroyed || amount <= 0f)
            {
                return;
            }

            currentDurability = Mathf.Min(maxDurability, currentDurability + amount);
            DurabilityChanged?.Invoke(currentDurability, maxDurability);
        }

        public void ApplyDamage(float amount)
        {
            if (destroyed || amount <= 0f)
            {
                return;
            }

            currentDurability = Mathf.Max(0f, currentDurability - amount);
            DurabilityChanged?.Invoke(currentDurability, maxDurability);

            if (currentDurability <= 0f)
            {
                destroyed = true;
                Destroyed?.Invoke();
            }
        }
    }
}
