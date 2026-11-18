using UnityEngine;
using System.Collections.Generic;

namespace TossRush
{
    // Resolve the previous physics segment before Projectile snapshots it again.
    [DefaultExecutionOrder(-100)]
    public sealed class RingTarget : MonoBehaviour
    {
        public StageView stage;
        [Min(1)] public int points = 20;
        [Tooltip("Local height of the narrow capture section, below the object's top.")]
        public float captureHeight = .4f;
        [Tooltip("Conservative preview/entry-search radius. Actual scoring checks collider sections.")]
        [Min(.001f)] public float captureRadius = .07f;
        [Tooltip("Continue checking genuine enclosure below the top entry plane, including rim roll-ins.")]
        [Min(0)] public float captureDepth = .60f;
        public bool IsClaimed { get; private set; }

        Vector3 previousCapturePosition;
        Collider[] captureShapes;
        sealed class VisualState
        {
            public Renderer renderer;
            public MaterialPropertyBlock[] originals;
        }
        List<VisualState> visualStates;
        const float ClaimedBrightness = .28f;
        static readonly int[] ColorProperties =
        {
            Shader.PropertyToID("_BaseColor"), Shader.PropertyToID("_Color"),
            Shader.PropertyToID("_FaceColor"), Shader.PropertyToID("_OutlineColor"),
            Shader.PropertyToID("_UnderlayColor"), Shader.PropertyToID("_GlowColor"), Shader.PropertyToID("_EmissionColor")
        };
        static readonly int RimProperty = Shader.PropertyToID("_Rim");
        // Crossing the neck is only an approach, never a reward. Wait until the
        // ring is genuinely around the body and has stopped bouncing/sliding.
        internal const double SettleDuration = .4;
        const float MaximumSettleSpeed = .25f, MaximumSettleDrift = .015f;
        struct EntryState
        {
            public int launch;
            public bool approachedFromAbove;
            public double settledSince;
            public Vector3 settledPosition;
        }
        readonly Dictionary<Projectile, EntryState> entries = new Dictionary<Projectile, EntryState>();

        void OnEnable()
        {
            if (!stage) stage = GetComponentInParent<StageView>();
            CacheCaptureShapes();
            ResetTracking();
        }

        void CacheCaptureShapes()
        {
            // The plinth is physical scenery, not the prize being encircled.
            // Keep its collision but exclude supports entirely below the origin.
            var shapes = new List<Collider>();
            foreach (var shape in GetComponentsInChildren<Collider>(true))
                if (shape.bounds.max.y > transform.position.y + .001f) shapes.Add(shape);
            captureShapes = shapes.ToArray();
        }

        public void ResetTracking()
        {
            previousCapturePosition = CapturePosition;
            entries.Clear();
        }

        public void ResetForRound()
        {
            RestoreAppearance();
            IsClaimed = false;
            gameObject.SetActive(true);
            ResetTracking();
        }

        internal bool TryClaim()
        {
            if (IsClaimed || !gameObject.activeInHierarchy) return false;
            IsClaimed = true;
            DimAppearance();
            return true;
        }

        void DimAppearance()
        {
            visualStates = new List<VisualState>();
            foreach (var surface in GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = surface.sharedMaterials;
                var state = new VisualState { renderer = surface, originals = new MaterialPropertyBlock[materials.Length] };
                for (int i = 0; i < materials.Length; i++)
                {
                    var original = new MaterialPropertyBlock(); surface.GetPropertyBlock(original, i);
                    state.originals[i] = original;
                    var paint = new MaterialPropertyBlock();
                    if (original.isEmpty) surface.GetPropertyBlock(paint); else surface.GetPropertyBlock(paint, i);
                    Material material = materials[i];
                    if (!material) continue;
                    foreach (int property in ColorProperties)
                    {
                        if (!material.HasProperty(property)) continue;
                        Color color = paint.HasProperty(property) ? paint.GetColor(property) : material.GetColor(property);
                        paint.SetColor(property, new Color(color.r * ClaimedBrightness, color.g * ClaimedBrightness,
                            color.b * ClaimedBrightness, color.a));
                    }
                    if (material.HasProperty(RimProperty)) paint.SetFloat(RimProperty, 0);
                    surface.SetPropertyBlock(paint, i);
                }
                visualStates.Add(state);
            }
        }

        void RestoreAppearance()
        {
            if (visualStates == null) return;
            foreach (var state in visualStates)
            {
                if (!state.renderer) continue;
                for (int i = 0; i < state.originals.Length; i++)
                    state.renderer.SetPropertyBlock(state.originals[i].isEmpty ? null : state.originals[i], i);
            }
            visualStates = null;
        }

        public Vector3 CapturePosition => transform.TransformPoint(Vector3.up * captureHeight);
        public float WorldCaptureRadius
        {
            get
            {
                Vector3 scale = transform.lossyScale;
                return captureRadius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            }
        }

        void FixedUpdate()
            => EvaluateShots();

        // Observe the final physics step in this render frame before GameDirector's
        // Update can end the round. FixedUpdate still covers multi-step frames.
        void Update()
            => EvaluateShots();

        void EvaluateShots()
        {
            var game = stage ? stage.Game : null;
            if (!IsClaimed && game && game.CanScore && game.ActiveStage == stage)
            {
                foreach (var shot in game.Projectiles)
                    if (TryScore(shot)) game.RegisterRingHit(shot, this);
            }
            previousCapturePosition = CapturePosition;
        }

        public bool TryScore(Projectile shot)
        {
            var game = stage ? stage.Game : null;
            if (IsClaimed || !gameObject.activeInHierarchy || !game || !game.CanScore || game.ActiveStage != stage || !shot || !shot.InFlight
                || shot.Scored || shot.Owner != game || shot.StageToken != game.StageToken) return false;
            var ring = shot.GetComponent<RingFlight>();
            if (!ring) return false;
            if (!entries.TryGetValue(shot, out var entry) || entry.launch != shot.LaunchId)
                entry = new EntryState { launch = shot.LaunchId, settledSince = -1 };
            Vector3 before = shot.PreviousPosition + CapturePosition - previousCapturePosition;
            Vector3 after = shot.Body.position;
            Quaternion beforeRotation = shot.PreviousScoringRotation, afterRotation = shot.Body.rotation;
            if (after.y < before.y && (before.y > CapturePosition.y + .00001f
                || NearSweep(before, after, ring) && SectionHeight(before, beforeRotation, out float previousHeight)
                    && previousHeight > captureHeight + .00001f))
                entry.approachedFromAbove = true;
            bool enclosed = entry.approachedFromAbove && EnclosedAtPose(after, afterRotation, ring)
                && SectionHeight(after, afterRotation, out float height)
                && height <= captureHeight - ring.tubeRadius * 2;
            bool quiet = shot.Body.linearVelocity.sqrMagnitude <= MaximumSettleSpeed * MaximumSettleSpeed
                && shot.Body.angularVelocity.sqrMagnitude <= .25f;
            double now = Time.timeAsDouble;
            if (!enclosed || !quiet)
                entry.settledSince = -1;
            else if (entry.settledSince < 0
                || (after - entry.settledPosition).sqrMagnitude > MaximumSettleDrift * MaximumSettleDrift)
            {
                entry.settledSince = now;
                entry.settledPosition = after;
            }
            entries[shot] = entry;
            return entry.settledSince >= 0 && now - entry.settledSince >= SettleDuration;
        }

        bool NearSweep(Vector3 before, Vector3 after, RingFlight ring)
        {
            Vector3 from = Vector3.ProjectOnPlane(before - transform.position, transform.up);
            Vector3 delta = Vector3.ProjectOnPlane(after - before, transform.up);
            float t = delta.sqrMagnitude > .000001f ? Mathf.Clamp01(-Vector3.Dot(from, delta) / delta.sqrMagnitude) : 0;
            float range = ring.WorldInnerRadius + ring.tubeRadius * 2 + WorldCaptureRadius + .05f;
            return (from + delta * t).sqrMagnitude <= range * range;
        }

        bool SectionHeight(Vector3 center, Quaternion rotation, out float height)
        {
            Vector3 normal = RingFlight.UpNormal(rotation);
            float along = Vector3.Dot(normal, transform.up);
            height = 0;
            if (along < .7f) return false;
            height = Vector3.Dot(center - transform.position, normal) / along / Mathf.Max(.0001f, Mathf.Abs(transform.lossyScale.y));
            return true;
        }

        public bool EnclosedAtPose(Vector3 center, Quaternion rotation, RingFlight ring)
        {
            if (!ring || !SectionHeight(center, rotation, out float height)
                || RingFlight.UpNormal(rotation).y < ring.minimumUpAlignment
                || height > captureHeight + .0005f
                // A nose-up ring can rest around the plinth with its centre just
                // below the prize's origin. Keep testing the real base section.
                || height < Mathf.Max(-ring.tubeRadius * 2, captureHeight - captureDepth) - .0005f) return false;
            Vector3 axisPoint = transform.position + transform.up * (height * Mathf.Abs(transform.lossyScale.y));
            float opening = ring.WorldInnerRadius + .0005f;
            if (Vector3.ProjectOnPlane(center - axisPoint, RingFlight.UpNormal(rotation)).sqrMagnitude > opening * opening) return false;
            if (captureShapes == null) CacheCaptureShapes();
            // Once supported at the base, the tilted ring plane can fall below
            // the prize's foot. Check the lower body projected into the same
            // opening along the upright prize axis, rather than the stand corners.
            Vector3 sectionCenter = center + transform.up * (Mathf.Max(0, .04f - height) * Mathf.Abs(transform.lossyScale.y));
            return RingCaptureGeometry.Encloses(captureShapes, sectionCenter, RingFlight.UpNormal(rotation), ring.WorldInnerRadius);
        }

        // A swept section test prevents fast rings tunnelling through a thin scoring plane.
        // Requiring the full neck section inside the hole excludes side/rim contacts.
        public static bool ContainsDownwardCrossing(Vector3 previousRing, Quaternion previousRotation,
            Vector3 currentRing, Quaternion currentRotation, Vector3 previousTarget, Vector3 currentTarget,
            float innerRadius, float targetRadius, float minimumUpAlignment = .7f)
        {
            Vector3 beforeNormal = RingFlight.UpNormal(previousRotation);
            Vector3 afterNormal = RingFlight.UpNormal(currentRotation);
            if (beforeNormal.y < minimumUpAlignment || afterNormal.y < minimumUpAlignment) return false;
            Vector3 from = previousRing - previousTarget, to = currentRing - currentTarget;
            float before = Vector3.Dot(from, beforeNormal), after = Vector3.Dot(to, afterNormal);
            if (before <= .00001f || after > 0 || to.y >= from.y) return false;

            float low = 0, high = 1;
            for (int iteration = 0; iteration < 10; iteration++)
            {
                float probe = (low + high) * .5f;
                Vector3 probeNormal = RingFlight.UpNormal(Quaternion.Slerp(previousRotation, currentRotation, probe));
                if (Vector3.Dot(Vector3.Lerp(from, to, probe), probeNormal) > 0) low = probe;
                else high = probe;
            }
            float t = (low + high) * .5f;
            Vector3 normal = RingFlight.UpNormal(Quaternion.Slerp(previousRotation, currentRotation, t));
            if (normal.y < minimumUpAlignment) return false;
            Vector3 offset = Vector3.Lerp(from, to, t);
            // A vertical cylindrical neck cuts a tilted ring plane as an ellipse.
            // Its largest radius is conservative and keeps the whole section in the hole.
            float clearance = innerRadius - targetRadius / Mathf.Max(.001f, normal.y);
            return clearance > 0 && Vector3.ProjectOnPlane(offset, normal).sqrMagnitude <= clearance * clearance;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(CapturePosition, WorldCaptureRadius);
        }
    }
}
