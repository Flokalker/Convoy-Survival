using UnityEngine;

namespace PostApocRoadtrip.World
{
    [DisallowMultipleComponent]
    public class ZombieAmbientSway : MonoBehaviour
    {
        [SerializeField] private float swayDegrees = 2.2f;
        [SerializeField] private float swaySpeed = 0.72f;
        [SerializeField] private float phaseOffset;

        private Quaternion baseRotation;

        private void Awake()
        {
            baseRotation = transform.localRotation;
            if (phaseOffset <= 0f)
            {
                phaseOffset = Random.Range(0f, 12f);
            }
        }

        private void Update()
        {
            var sway = Mathf.Sin(Time.time * swaySpeed + phaseOffset) * swayDegrees;
            transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, sway);
        }
    }
}
