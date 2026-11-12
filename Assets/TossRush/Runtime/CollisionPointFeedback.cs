using TMPro;
using UnityEngine;

namespace TossRush
{
    public sealed class CollisionPointFeedback : MonoBehaviour
    {
        public TMP_Text[] labels;
        [Min(.1f)] public float duration = .85f;
        [Min(0)] public float rise = 65;
        struct Popup
        {
            public Camera view;
            public Vector3 point;
            public float started;
            public bool active;
        }
        Popup[] popups;
        Canvas canvas;
        RectTransform area;
        int next;

        void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            area = (RectTransform)transform;
            popups = new Popup[labels.Length];
            Clear();
        }

        public void Clear()
        {
            next = 0;
            if (popups != null) System.Array.Clear(popups, 0, popups.Length);
            foreach (var label in labels) label.gameObject.SetActive(false);
        }

        public void Show(int points, Vector3 point, Camera view, Color color)
        {
            if (popups == null || popups.Length == 0 || !view) return;
            int index = next;
            next = (next + 1) % popups.Length;
            popups[index] = new Popup { view = view, point = point, started = Time.time, active = true };
            labels[index].text = "+" + points;
            labels[index].color = Color.Lerp(color, Color.white, .65f);
            UpdatePopup(index);
        }

        void LateUpdate()
        {
            for (int i = 0; i < popups.Length; i++) if (popups[i].active) UpdatePopup(i);
        }

        void UpdatePopup(int index)
        {
            var popup = popups[index];
            var label = labels[index];
            float progress = (Time.time - popup.started) / duration;
            if (progress >= 1 || !popup.view)
            {
                popups[index].active = false;
                label.gameObject.SetActive(false);
                return;
            }
            Vector3 screen = popup.view.WorldToScreenPoint(popup.point);
            bool visible = screen.z > 0 && popup.view.pixelRect.Contains(new Vector2(screen.x, screen.y));
            label.gameObject.SetActive(visible);
            if (!visible) return;
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, uiCamera, out var local))
                label.rectTransform.anchoredPosition = local + Vector2.up * (rise * progress);
            var color = label.color;
            color.a = 1 - Mathf.SmoothStep(0, 1, progress);
            label.color = color;
        }
    }
}
