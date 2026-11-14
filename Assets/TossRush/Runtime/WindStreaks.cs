using UnityEngine;

namespace TossRush
{
    // A fixed pool of camera-local lines. Their direction follows the existing wind arrow.
    public sealed class WindStreaks : MonoBehaviour
    {
        public ThrowController thrower;
        public LineRenderer[] streaks;
        [Min(.01f)] public float minimumWind = .05f;
        [Min(.5f)] public float depth = 4;
        static readonly Vector2[] Anchors = {
            new Vector2(.08f,.76f), new Vector2(.92f,.64f),
            new Vector2(.11f,.49f), new Vector2(.88f,.37f),
            new Vector2(.28f,.88f), new Vector2(.72f,.84f),
            new Vector2(.07f,.3f), new Vector2(.95f,.22f),
            new Vector2(.45f,.94f), new Vector2(.88f,.92f),
            new Vector2(.22f,.78f), new Vector2(.83f,.74f)
        };
        readonly Vector3[] points = new Vector3[6];
        int stageToken = -1;
        float started;

        void Awake() => Hide();
        void OnDisable() => Hide();
        void Hide()
        {
            if (streaks == null) return;
            foreach (var line in streaks) if (line) line.enabled = false;
        }

        void LateUpdate()
        {
            var game = thrower ? thrower.game : null;
            if (!game || !game.CanScore || !thrower.view || !game.hud.playPanel.activeInHierarchy) { Hide(); return; }
            Vector3 wind = game.ActiveStage.CurrentWind * game.ActiveStage.data.windInfluence;
            float speed = wind.magnitude;
            if (speed <= minimumWind) { Hide(); return; }
            var cameraView = thrower.view;
            Vector2 direction = new Vector2(Vector3.Dot(wind, cameraView.transform.right), Vector3.Dot(wind, cameraView.transform.forward));
            if (direction.sqrMagnitude < .0001f) { Hide(); return; }
            direction.Normalize();
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            // Viewport X and Y have different pixel scales on a wide screen.
            // Use screen-height units so diagonal flow matches the arrow angle.
            direction.x /= cameraView.aspect;
            perpendicular.x /= cameraView.aspect;
            if (stageToken != game.StageToken) { stageToken = game.StageToken; started = Time.time; }
            float strength = Mathf.Clamp01(speed / 7);
            int count = Mathf.Min(streaks.Length, Mathf.CeilToInt(6 + strength * 6));
            float elapsed = Time.time - started;
            float height = 2 * depth * Mathf.Tan(cameraView.fieldOfView * .5f * Mathf.Deg2Rad);
            float width = height * Mathf.Lerp(1.7f, 2.5f, strength) / Mathf.Max(1, cameraView.pixelHeight);
            width *= Mathf.Max(.1f, game.ActiveStage.data.windLineWidthMultiplier);
            for (int i = 0; i < streaks.Length; i++)
            {
                var line = streaks[i];
                if (i >= count) { line.enabled = false; continue; }
                float phase = Mathf.Repeat(elapsed * Mathf.Lerp(.32f, .85f, strength) + i * .173f, 1);
                Vector2 centre = Anchors[i % Anchors.Length] + direction * ((phase - .5f) * .24f);
                float length = Mathf.Lerp(.11f, .18f, strength) * (1 + (i % 3) * .1f);
                bool clear = true;
                for (int j = 0; j < points.Length; j++)
                {
                    float t = j / (float)(points.Length - 1);
                    Vector2 p = centre + direction * ((t - .5f) * length)
                        + perpendicular * (Mathf.Sin(t * Mathf.PI) * .007f * (i % 2 == 0 ? 1 : -1));
                    // Leave the central aiming area entirely free, even when looking sideways.
                    if (p.x > .34f && p.x < .66f && p.y > .32f && p.y < .72f) clear = false;
                    points[j] = transform.InverseTransformPoint(cameraView.ViewportToWorldPoint(new Vector3(p.x, p.y, depth)));
                }
                line.enabled = clear;
                if (!clear) continue;
                float alpha = Mathf.Sin(phase * Mathf.PI) * Mathf.Lerp(.28f, .65f, strength);
                var color = new Color(.12f, .54f, 1, alpha);
                line.startColor = line.endColor = color;
                line.widthMultiplier = width;
                line.SetPositions(points);
            }
        }
    }
}
