using ConvoySurvival.Data;
using UnityEngine;

namespace ConvoySurvival.Run
{
    [DisallowMultipleComponent]
    public class TruckVisualAdapter : MonoBehaviour
    {
        [SerializeField] private Transform bodyRoot;
        [SerializeField] private GameObject spikesObject;
        [SerializeField] private GameObject plowObject;
        [SerializeField] private GameObject turretObject;

        public void Configure(Transform root, GameObject spikes, GameObject plow, GameObject turret)
        {
            bodyRoot = root;
            spikesObject = spikes;
            plowObject = plow;
            turretObject = turret;
        }

        public void ApplyTier(TruckUpgradeTier tier)
        {
            if (bodyRoot != null)
            {
                float scale = Mathf.Max(0.6f, tier.TruckScale);
                bodyRoot.localScale = new Vector3(scale, scale, scale);
            }

            if (spikesObject != null)
            {
                spikesObject.SetActive(tier.HasSpikes);
            }

            if (plowObject != null)
            {
                plowObject.SetActive(tier.HasPlow);
            }

            if (turretObject != null)
            {
                turretObject.SetActive(tier.HasTurret);
            }
        }
    }
}
