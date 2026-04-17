using UnityEngine;

namespace ConvoySurvival.Run.World
{
    [DisallowMultipleComponent]
    public class ObjectiveWaypointMarker : MonoBehaviour
    {
        [SerializeField] private float spinSpeed = 80f;

        private ConvoyObjectiveSystem owner;
        private int rewardScrap;
        private bool claimed;

        public void Configure(ConvoyObjectiveSystem objectiveSystem, int reward)
        {
            owner = objectiveSystem;
            rewardScrap = Mathf.Max(0, reward);
        }

        private void Update()
        {
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (claimed)
            {
                return;
            }

            TruckController truck = other.GetComponentInParent<TruckController>();
            if (truck == null)
            {
                return;
            }

            claimed = true;
            owner?.CompleteCurrentObjective(this, rewardScrap);
        }
    }
}
