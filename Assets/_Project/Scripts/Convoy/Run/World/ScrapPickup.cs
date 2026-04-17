using ConvoySurvival.Core;
using UnityEngine;

namespace ConvoySurvival.Run.World
{
    [DisallowMultipleComponent]
    public class ScrapPickup : MonoBehaviour
    {
        [SerializeField, Min(1)] private int scrapAmount = 6;
        [SerializeField, Min(0f)] private float fuelRefill = 4f;
        [SerializeField] private float rotateSpeed = 120f;

        private bool collected;

        private void Update()
        {
            transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected)
            {
                return;
            }

            TruckController truck = other.GetComponentInParent<TruckController>();
            if (truck == null)
            {
                return;
            }

            collected = true;
            PrototypeSessionStateManager session = PrototypeSessionStateManager.Instance;
            if (session != null)
            {
                session.AddScrap(scrapAmount);
            }

            if (fuelRefill > 0f)
            {
                truck.RefillFuel(fuelRefill);
            }

            Destroy(gameObject);
        }
    }
}
