using UnityEngine;

namespace ConvoySurvival.Run.World
{
    [DisallowMultipleComponent]
    public class TradingGateController : MonoBehaviour
    {
        [SerializeField] private GameObject blockerObject;

        private bool blocked = true;

        public bool IsBlocked => blocked;

        public void Configure(GameObject blocker)
        {
            blockerObject = blocker;
            ApplyState();
        }

        private void Awake()
        {
            if (blockerObject == null)
            {
                Transform blocker = transform.Find("RoadBlocker");
                if (blocker != null)
                {
                    blockerObject = blocker.gameObject;
                }
            }

            ApplyState();
        }

        public void SetBlocked(bool value)
        {
            if (blocked == value)
            {
                return;
            }

            blocked = value;
            ApplyState();
        }

        private void ApplyState()
        {
            if (blockerObject != null)
            {
                blockerObject.SetActive(blocked);
            }
        }
    }
}
