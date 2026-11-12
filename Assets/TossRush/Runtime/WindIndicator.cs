using TMPro;
using UnityEngine;

namespace TossRush
{
    public sealed class WindIndicator : MonoBehaviour
    {
        public RectTransform arrow;
        public UnityEngine.UI.Image[] coreImages, glowImages;
        public TMP_Text speedText;
        Vector3 wind;
        Color tint;

        public void SetWind(Vector3 worldWind, Transform view, Color accent)
        {
            wind = worldWind;
            tint = accent;
            Vector2 direction = new Vector2(Vector3.Dot(worldWind, view.right), Vector3.Dot(worldWind, view.forward));
            arrow.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            speedText.text = worldWind.magnitude.ToString("0.0") + " m/s";
        }

        void Update()
        {
            // Local neon breathing, never a full-screen flash or a completely unreadable direction.
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6.5f);
            float flicker = 0.94f + 0.06f * Mathf.Sin(Time.unscaledTime * 29f);
            float strength = wind.sqrMagnitude < 0.0025f ? 0.18f : 1;
            Color core = Color.Lerp(tint, Color.white, 0.55f);
            core.a = strength * flicker;
            Color glow = tint;
            glow.a = strength * (0.18f + 0.22f * pulse);
            foreach (var image in coreImages) image.color = core;
            foreach (var image in glowImages) image.color = glow;
        }
    }
}
