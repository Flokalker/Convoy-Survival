using System;
using ConvoySurvival.Data;

namespace ConvoySurvival.Core
{
    [Serializable]
    public class PrototypeSaveStateManager
    {
        [Serializable]
        public struct SessionSnapshot
        {
            public int Scrap;
            public UpgradeSystem.UpgradeState UpgradeState;
            public float BestDistance;
            public string LastRunSummary;
        }

        private SessionSnapshot snapshot;

        public SessionSnapshot Snapshot => snapshot;

        public void Capture(int scrap, UpgradeSystem.UpgradeState upgradeState, float bestDistance, string summary)
        {
            snapshot.Scrap = Math.Max(0, scrap);
            snapshot.UpgradeState = upgradeState;
            snapshot.BestDistance = Math.Max(0f, bestDistance);
            snapshot.LastRunSummary = summary ?? string.Empty;
        }

        public void SetLastRunSummary(string summary)
        {
            snapshot.LastRunSummary = summary ?? string.Empty;
        }

        public void SetBestDistance(float bestDistance)
        {
            snapshot.BestDistance = Math.Max(0f, bestDistance);
        }
    }
}
