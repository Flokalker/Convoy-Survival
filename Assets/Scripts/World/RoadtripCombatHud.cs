using UnityEngine;

namespace PostApocRoadtrip.World
{
    public class RoadtripCombatHud : MonoBehaviour
    {
        private VehicleHealth vehicleHealth;
        private ZombieWaveManager waveManager;
        private GUIStyle labelStyle;

        private void Start()
        {
            vehicleHealth = FindObjectOfType<VehicleHealth>();
            waveManager = FindObjectOfType<ZombieWaveManager>();
        }

        private void OnGUI()
        {
            vehicleHealth ??= FindObjectOfType<VehicleHealth>();
            waveManager ??= FindObjectOfType<ZombieWaveManager>();
            if (vehicleHealth == null)
            {
                return;
            }

            labelStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.9f, 0.95f, 1f) }
            };

            var x = 24f;
            var y = 22f;
            var width = 280f;
            var height = 22f;
            GUI.color = new Color(0.02f, 0.025f, 0.03f, 0.78f);
            GUI.DrawTexture(new Rect(x - 10f, y - 10f, width + 20f, 118f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y, width, 24f), "AUTO-LEBEN", labelStyle);

            GUI.color = new Color(0.16f, 0.17f, 0.18f, 1f);
            GUI.DrawTexture(new Rect(x, y + 30f, width, height), Texture2D.whiteTexture);
            GUI.color = vehicleHealth.Health01 > 0.35f ? new Color(0.34f, 0.82f, 0.56f) : new Color(0.9f, 0.28f, 0.22f);
            GUI.DrawTexture(new Rect(x, y + 30f, width * vehicleHealth.Health01, height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y + 58f, width, 24f), $"HP {Mathf.CeilToInt(vehicleHealth.currentHealth)} / {Mathf.CeilToInt(vehicleHealth.maxHealth)}", labelStyle);

            var waveText = waveManager == null ? "Welle wird vorbereitet" : $"Welle {waveManager.CurrentWave}  Zombies: {waveManager.ActiveZombies}";
            GUI.Label(new Rect(x, y + 84f, width, 24f), $"{waveText}   |   H halten = reparieren", labelStyle);
        }
    }
}
