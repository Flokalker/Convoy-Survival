using UnityEngine;

namespace ConvoySurvival.Run.World
{
    [DisallowMultipleComponent]
    public class RoadHazard : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float impactDamage = 20f;
        [SerializeField] private bool destroyOnImpact = true;

        private bool consumed;

        private void OnCollisionEnter(Collision collision)
        {
            if (consumed)
            {
                return;
            }

            TruckController truck = collision.collider.GetComponentInParent<TruckController>();
            if (truck == null)
            {
                return;
            }

            consumed = true;
            truck.ApplyImpactDamage(impactDamage);

            if (destroyOnImpact)
            {
                Destroy(gameObject);
            }
        }
    }
}
