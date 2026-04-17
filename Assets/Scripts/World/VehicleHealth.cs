using UnityEngine;

namespace PostApocRoadtrip.World
{
    public class VehicleHealth : MonoBehaviour
    {
        public float maxHealth = 100f;
        public float currentHealth = 100f;
        public float holdRepairRate = 14f;
        public float repairDelayAfterDamage = 2.2f;

        private float lastDamageTime = -99f;

        public float Health01 => maxHealth <= 0f ? 0f : Mathf.Clamp01(currentHealth / maxHealth);
        public bool IsDestroyed => currentHealth <= 0f;

        private void Awake()
        {
            currentHealth = Mathf.Clamp(currentHealth <= 0f ? maxHealth : currentHealth, 0f, maxHealth);
        }

        private void Update()
        {
            if (IsDestroyed)
            {
                return;
            }

            if (Input.GetKey(KeyCode.H) && Time.time - lastDamageTime >= repairDelayAfterDamage)
            {
                Heal(holdRepairRate * Time.deltaTime);
            }
        }

        public void Damage(float amount)
        {
            if (amount <= 0f || IsDestroyed)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            lastDamageTime = Time.time;
        }

        public void Heal(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }
    }
}
