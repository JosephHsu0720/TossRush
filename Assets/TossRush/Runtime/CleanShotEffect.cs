using TMPro;
using UnityEngine;

namespace TossRush
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class CleanShotEffect : MonoBehaviour
    {
        public TMP_Text label;
        public RectTransform rays;
        CanvasGroup group;
        float startedAt = -10;
        bool playing;

        void Awake() { group = GetComponent<CanvasGroup>(); ResetEffect(); }

        public void Play(int bonus)
        {
            gameObject.SetActive(true);
            startedAt = Time.unscaledTime;
            label.text = "SWISH!   +" + bonus;
            playing = true;
            rays.gameObject.SetActive(true);
            label.gameObject.SetActive(true);
        }

        public void ResetEffect()
        {
            playing = false;
            startedAt = -10;
            if (group) group.alpha = 0;
            rays.gameObject.SetActive(false);
            label.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!playing) return;
            float elapsed = Time.unscaledTime - startedAt;
            if (elapsed >= 1.05f) { ResetEffect(); return; }
            group.alpha = elapsed < 0.6f ? Mathf.Clamp01(elapsed * 12) : Mathf.Clamp01((1.05f - elapsed) / 0.45f);
            float scale = Mathf.Lerp(0.7f, 1.15f, Mathf.Clamp01(elapsed * 3));
            rays.localScale = Vector3.one * scale;
            label.transform.localScale = Vector3.one * (1 + Mathf.Sin(Mathf.Clamp01(elapsed * 4) * Mathf.PI) * 0.12f);
        }
    }
}
