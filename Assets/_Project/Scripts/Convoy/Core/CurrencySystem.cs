using System;

namespace ConvoySurvival.Core
{
    [Serializable]
    public class CurrencySystem
    {
        private int scrap;

        public event Action<int> ScrapChanged;

        public int Scrap => scrap;

        public void SetScrap(int value)
        {
            int clamped = Math.Max(0, value);
            if (clamped == scrap)
            {
                return;
            }

            scrap = clamped;
            ScrapChanged?.Invoke(scrap);
        }

        public void AddScrap(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            scrap += amount;
            ScrapChanged?.Invoke(scrap);
        }

        public bool TrySpendScrap(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (scrap < amount)
            {
                return false;
            }

            scrap -= amount;
            ScrapChanged?.Invoke(scrap);
            return true;
        }
    }
}
