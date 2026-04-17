using ConvoySurvival.Core;
using UnityEngine;

namespace ConvoySurvival.Run.World
{
    [DisallowMultipleComponent]
    public class ZombieTarget : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxHealth = 20f;
        [SerializeField, Min(1f)] private float speedRequirementToKill = 14f;
        [SerializeField, Min(1f)] private float impactDamageIfNotKilled = 16f;
        [SerializeField, Min(0f)] private float chipDamageOnKill = 3f;
        [SerializeField, Min(0)] private int scrapReward = 3;
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Collider[] colliders;

        private float health;
        private bool dead;

        private void Awake()
        {
            health = maxHealth;

            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>(true);
            }

            if (colliders == null || colliders.Length == 0)
            {
                colliders = GetComponentsInChildren<Collider>(true);
            }
        }

        private void Update()
        {
            if (dead)
            {
                return;
            }

            transform.Rotate(0f, 30f * Time.deltaTime, 0f, Space.World);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (dead)
            {
                return;
            }

            TruckController truck = collision.collider.GetComponentInParent<TruckController>();
            if (truck == null)
            {
                return;
            }

            ResolveTruckImpact(truck);
        }

        public void ApplyTurretDamage(float damage)
        {
            if (dead || damage <= 0f)
            {
                return;
            }

            health -= damage;
            if (health <= 0f)
            {
                Kill();
            }
        }

        private void ResolveTruckImpact(TruckController truck)
        {
            bool killByRam = truck != null && truck.CanRamZombie(speedRequirementToKill);
            if (killByRam)
            {
                if (truck != null)
                {
                    truck.ApplyImpactDamage(chipDamageOnKill);
                }

                Kill();
                return;
            }

            if (truck != null)
            {
                truck.ApplyImpactDamage(impactDamageIfNotKilled);
            }

            Kill();
        }

        private void Kill()
        {
            if (dead)
            {
                return;
            }

            dead = true;

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = false;
                }
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = false;
                }
            }

            PrototypeSessionStateManager session = PrototypeSessionStateManager.Instance;
            if (session != null && scrapReward > 0)
            {
                session.AddScrap(scrapReward);
            }

            Destroy(gameObject, 0.1f);
        }
    }
}
