using System.Collections.Generic;
using UnityEngine;

namespace TossRush
{
    // Runs before Projectile.FixedUpdate updates its previous position.
    [DefaultExecutionOrder(-100)]
    public sealed class ScoreGate : MonoBehaviour
    {
        [Tooltip("Local normal of the entry plane. Up for bins/hoops; back for a vertical hole.")]
        public Vector3 entryNormal = Vector3.up;
        public StageView stage;
        [Tooltip("Optional rectangular opening in gate-local X/Z units. Zero uses the original circular opening.")]
        public Vector2 openingHalfExtents;
        [Tooltip("World-space depth below the entry plane to finish a rim roll-in. Zero requires crossing the plane fully inside the opening.")]
        [Min(0)] public float rollInDepth;
        Vector3 previousCenter;
        readonly HashSet<Projectile> pendingRollIns = new HashSet<Projectile>();

        void OnEnable() => ResetTracking();
        public void ResetTracking()
        {
            previousCenter = transform.position;
            pendingRollIns.Clear();
        }

        void FixedUpdate()
        {
            var game = stage.Game;
            if (!game || !game.CanScore || game.ActiveStage != stage) { ResetTracking(); return; }
            Vector3 center = transform.position;
            Vector3 normal = transform.TransformDirection(entryNormal).normalized;
            foreach (var shot in game.Projectiles)
            {
                if (!shot.InFlight || shot.Scored || shot.StageToken != game.StageToken) { pendingRollIns.Remove(shot); continue; }
                Vector3 from = shot.PreviousPosition - previousCenter;
                Vector3 to = shot.Body.position - center;
                float before = Vector3.Dot(from, normal);
                float after = Vector3.Dot(to, normal);
                if (after > 0) { pendingRollIns.Remove(shot); continue; }
                bool entered = false;
                if (before > 0)
                {
                    float t = before / (before - after);
                    Vector3 crossing = Vector3.Lerp(from, to, t);
                    entered = FitsOpening(crossing, shot.Radius, normal);
                    // A rim contact can carry the center below the plane before the
                    // whole paper fits. Only track shots that entered at the opening.
                    if (!entered && rollInDepth > 0 && FitsOpening(crossing, 0, normal)) pendingRollIns.Add(shot);
                }
                if (!entered && pendingRollIns.Contains(shot))
                {
                    if (after < -rollInDepth || !FitsOpening(to, 0, normal)) pendingRollIns.Remove(shot);
                    else entered = after < before && FitsOpening(to, shot.Radius, normal);
                }
                if (!entered) continue;
                pendingRollIns.Remove(shot);
                shot.MarkScored();
                game.RegisterHit(shot);
                break;
            }
            previousCenter = center;
        }

        public bool FitsOpening(Vector3 crossingOffset, float radius, Vector3 normal)
        {
            if (openingHalfExtents.x > 0 && openingHalfExtents.y > 0) {
                Vector3 local = transform.InverseTransformVector(crossingOffset);
                Vector3 scale = transform.lossyScale;
                float width = openingHalfExtents.x - radius / Mathf.Max(0.001f, Mathf.Abs(scale.x));
                float depth = openingHalfExtents.y - radius / Mathf.Max(0.001f, Mathf.Abs(scale.z));
                return width > 0 && depth > 0 && Mathf.Abs(local.x) <= width && Mathf.Abs(local.z) <= depth;
            }
            float clearance = stage.data.targetRadius - radius;
            return clearance > 0 && Vector3.ProjectOnPlane(crossingOffset, normal).sqrMagnitude <= clearance * clearance;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, stage && stage.data ? stage.data.targetRadius : 0.6f);
            Gizmos.DrawRay(transform.position, transform.TransformDirection(entryNormal));
        }
    }
}
