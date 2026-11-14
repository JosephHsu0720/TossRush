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
        readonly Dictionary<Projectile, int> pendingRollIns = new Dictionary<Projectile, int>();
        readonly Vector3[] boxCorners = new Vector3[8];

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
                if (pendingRollIns.TryGetValue(shot, out int launchId) && launchId != shot.LaunchId) pendingRollIns.Remove(shot);
                Vector3 from = shot.PreviousScoringPosition - previousCenter;
                Vector3 to = shot.ScoringPosition - center;
                float before = Vector3.Dot(from, normal);
                float after = Vector3.Dot(to, normal);
                if (UsesBoxEntry(shot))
                {
                    if (TryBoxEntry(from, to, shot, normal, true))
                    {
                        pendingRollIns.Remove(shot);
                        shot.MarkScored();
                        game.RegisterHit(shot);
                        break;
                    }
                    continue;
                }
                if (after > 0) { pendingRollIns.Remove(shot); continue; }
                bool entered = false;
                if (before > 0)
                {
                    float t = before / (before - after);
                    Vector3 crossing = Vector3.Lerp(from, to, t);
                    entered = FitsOpening(crossing, shot, normal);
                    // A sphere overlapping the rim can roll inside after its center has
                    // crossed the plane. A new shot already below the plane never qualifies.
                    if (!entered && rollInDepth > 0 && OverlapsOpening(crossing, shot.ScoringRadius, normal)) pendingRollIns[shot] = shot.LaunchId;
                }
                if (!entered && pendingRollIns.ContainsKey(shot))
                {
                    // Test the segment at the bottom of the tracking region before discarding
                    // a fast shot that passes through the whole region in one physics step.
                    Vector3 probe = after < -rollInDepth && before >= -rollInDepth
                        ? Vector3.Lerp(from, to, (before + rollInDepth) / (before - after)) : to;
                    entered = before >= -rollInDepth && after <= before + .001f && FitsTrackedOpening(probe, shot, normal);
                    if (!entered && (after < -rollInDepth || !FitsOpening(to, 0, normal))) pendingRollIns.Remove(shot);
                }
                if (!entered) continue;
                pendingRollIns.Remove(shot);
                shot.MarkScored();
                game.RegisterHit(shot);
                break;
            }
            previousCenter = center;
        }

        public bool TryScoreBeforeInteriorContact(Projectile shot, Collision collision)
        {
            // A fast shot can enter and hit an inner wall within one physics step.
            // Resolve the earlier clean entry before recording that later interior contact.
            if (collision.contactCount == 0) return false;
            var game = stage ? stage.Game : null;
            if (!game || !game.CanScore || game.ActiveStage != stage || shot.Owner != game || shot.StageToken != game.StageToken) return false;
            if (collision.collider.GetComponentInParent<TargetMarker>() != GetComponentInParent<TargetMarker>()) return false;
            Vector3 normal = transform.TransformDirection(entryNormal).normalized;
            // A rim contact just below the plane still belongs to the entry itself.
            // Only contacts well inside the target can be treated as later than a clean entry.
            if (Vector3.Dot(collision.GetContact(0).point - transform.position, normal) >= -Mathf.Max(.01f, shot.ScoringRadius)) return false;
            Vector3 from = shot.PreviousScoringPosition - previousCenter, to = shot.ScoringPosition - transform.position;
            if (UsesBoxEntry(shot))
            {
                // Keep lip contacts as collisions. A later fully-contained fallback is
                // resolved in FixedUpdate, after the contact has cancelled the clean bonus.
                if (!TryBoxEntry(from, to, shot, normal, false)) return false;
                pendingRollIns.Remove(shot);
                shot.MarkScored(); game.RegisterHit(shot); return true;
            }
            float before = Vector3.Dot(from, normal), after = Vector3.Dot(to, normal);
            if (before <= 0 || after > 0) return false;
            Vector3 crossing = Vector3.Lerp(from, to, before / (before - after));
            if (!FitsOpening(crossing, shot, normal)) return false;
            shot.MarkScored(); game.RegisterHit(shot); return true;
        }

        bool UsesBoxEntry(Projectile shot) => rollInDepth > 0 && openingHalfExtents.x > 0
            && openingHalfExtents.y > 0 && shot.GetComponent<BoxCollider>();

        void BoxAxes(Projectile shot, Quaternion rotation, out Vector3 x, out Vector3 y, out Vector3 z)
        {
            var size = shot.GetComponent<BoxCollider>().size * .5f;
            Vector3 scale = shot.transform.lossyScale;
            x = transform.InverseTransformVector(rotation * Vector3.right * (size.x * scale.x));
            y = transform.InverseTransformVector(rotation * Vector3.up * (size.y * scale.y));
            z = transform.InverseTransformVector(rotation * Vector3.forward * (size.z * scale.z));
        }

        static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        bool TryBoxEntry(Vector3 from, Vector3 to, Projectile shot, Vector3 normal, bool allowContained)
        {
            float before = Vector3.Dot(from, normal), after = Vector3.Dot(to, normal);
            if (pendingRollIns.TryGetValue(shot, out int id) && id != shot.LaunchId) pendingRollIns.Remove(shot);
            BoxAxes(shot, shot.Body.rotation, out var x, out var y, out var z);
            Vector3 extent = Abs(x) + Abs(y) + Abs(z);
            Vector3 local = transform.InverseTransformVector(to);
            float normalExtent = Mathf.Abs(Vector3.Dot(transform.TransformVector(x), normal))
                + Mathf.Abs(Vector3.Dot(transform.TransformVector(y), normal))
                + Mathf.Abs(Vector3.Dot(transform.TransformVector(z), normal));

            // Remember a front approach as soon as the leading edge overlaps the slot.
            // The centre need not fit the whole projected box when it later rotates in.
            float t = before > 0 && after <= 0 ? before / (before - after) : 1;
            Vector3 crossing = Vector3.Lerp(from, to, t);
            Vector3 probe = transform.InverseTransformVector(crossing);
            if (before > 0 && after <= normalExtent
                && Mathf.Abs(probe.x) <= openingHalfExtents.x + extent.x
                && Mathf.Abs(probe.z) <= openingHalfExtents.y + extent.z)
                pendingRollIns[shot] = shot.LaunchId;

            if (before > 0 && after <= 0 && FitsBoxSection(crossing, shot,
                Quaternion.Slerp(shot.PreviousScoringRotation, shot.Body.rotation, t))) return true;

            if (!pendingRollIns.ContainsKey(shot)) return false;
            if (after <= 0 && FitsBoxSection(to, shot, shot.Body.rotation)) return true;

            // A newspaper can clear the lip and fall below its height inside the box.
            // Require front-entry history and the entire box behind the entry plane,
            // between the jambs, below the lintel, and above the interior floor region.
            const float tolerance = .002f;
            float height = to.y;
            float verticalExtent = Mathf.Abs(transform.TransformVector(x).y)
                + Mathf.Abs(transform.TransformVector(y).y) + Mathf.Abs(transform.TransformVector(z).y);
            float halfHeight = Mathf.Abs(transform.TransformVector(Vector3.right).y) * openingHalfExtents.x
                + Mathf.Abs(transform.TransformVector(Vector3.forward).y) * openingHalfExtents.y;
            if (allowContained && after + normalExtent < -tolerance && after >= -rollInDepth - normalExtent
                && Mathf.Abs(local.x) + extent.x <= openingHalfExtents.x + tolerance
                && height + verticalExtent <= halfHeight + tolerance
                && height - verticalExtent >= -halfHeight - rollInDepth - tolerance) return true;

            if (after > normalExtent + .02f || after < -rollInDepth - normalExtent - .02f
                || Mathf.Abs(local.x) > openingHalfExtents.x + extent.x
                || height - verticalExtent > halfHeight
                || height + verticalExtent < -halfHeight - rollInDepth)
                pendingRollIns.Remove(shot);
            return false;
        }

        bool FitsBoxSection(Vector3 offset, Projectile shot, Quaternion rotation)
        {
            BoxAxes(shot, rotation, out var x, out var y, out var z);
            Vector3 centre = transform.InverseTransformVector(offset);
            for (int i = 0; i < 8; i++) boxCorners[i] = centre
                + ((i & 1) == 0 ? -x : x) + ((i & 2) == 0 ? -y : y) + ((i & 4) == 0 ? -z : z);
            bool intersects = false;
            Vector3 localNormal = entryNormal.normalized;
            for (int i = 0; i < 8; i++)
            for (int axis = 1; axis <= 4; axis *= 2)
            {
                int j = i ^ axis;
                if (j <= i) continue;
                float a = Vector3.Dot(boxCorners[i], localNormal), b = Vector3.Dot(boxCorners[j], localNormal);
                if (a > .00001f && b > .00001f || a < -.00001f && b < -.00001f) continue;
                Vector3 point = Mathf.Abs(a - b) < .00001f ? boxCorners[i]
                    : Vector3.Lerp(boxCorners[i], boxCorners[j], Mathf.Clamp01(a / (a - b)));
                intersects = true;
                if (Mathf.Abs(point.x) > openingHalfExtents.x + .002f
                    || Mathf.Abs(point.z) > openingHalfExtents.y + .002f) return false;
                // Coplanar edges have two endpoints in the section.
                if (Mathf.Abs(a) < .00001f && Mathf.Abs(b) < .00001f
                    && (Mathf.Abs(boxCorners[j].x) > openingHalfExtents.x + .002f
                        || Mathf.Abs(boxCorners[j].z) > openingHalfExtents.y + .002f)) return false;
            }
            return intersects;
        }

        bool OverlapsOpening(Vector3 offset, float radius, Vector3 normal)
        {
            if (openingHalfExtents.x <= 0 || openingHalfExtents.y <= 0)
                return Vector3.ProjectOnPlane(offset, normal).magnitude <= stage.data.targetRadius + radius;
            Vector3 local = transform.InverseTransformVector(offset), scale = transform.lossyScale;
            float x = Mathf.Max(0, Mathf.Abs(local.x) - openingHalfExtents.x) * Mathf.Abs(scale.x);
            float z = Mathf.Max(0, Mathf.Abs(local.z) - openingHalfExtents.y) * Mathf.Abs(scale.z);
            return x * x + z * z <= radius * radius;
        }

        bool FitsTrackedOpening(Vector3 offset, Projectile shot, Vector3 normal)
        {
            if (!shot.GetComponent<SphereCollider>()) return FitsOpening(offset, shot, normal);
            // The sphere section intersecting the rim gets smaller as the sphere descends.
            // Once entirely below the rim, its center must remain inside the opening.
            float radius = shot.ScoringRadius, depth = Vector3.Dot(offset, normal);
            float section = Mathf.Sqrt(Mathf.Max(0, radius * radius - depth * depth));
            return FitsOpening(offset, section, normal);
        }

        public bool FitsOpening(Vector3 crossingOffset, Projectile shot, Vector3 normal)
        {
            // Flat newspapers need their actual projected box dimensions to fit a narrow mail slot.
            var box = shot.GetComponent<BoxCollider>();
            if (openingHalfExtents.x > 0 && openingHalfExtents.y > 0 && box)
            {
                Matrix4x4 boxToBody = shot.transform.worldToLocalMatrix * box.transform.localToWorldMatrix;
                Matrix4x4 boxToWorld = Matrix4x4.TRS(shot.Body.position, shot.Body.rotation, shot.transform.lossyScale) * boxToBody;
                Matrix4x4 toGate = transform.worldToLocalMatrix * boxToWorld;
                Vector3 x = toGate.MultiplyVector(Vector3.right * box.size.x * .5f);
                Vector3 y = toGate.MultiplyVector(Vector3.up * box.size.y * .5f);
                Vector3 z = toGate.MultiplyVector(Vector3.forward * box.size.z * .5f);
                Vector3 local = transform.InverseTransformVector(crossingOffset);
                float width = openingHalfExtents.x - Mathf.Abs(x.x) - Mathf.Abs(y.x) - Mathf.Abs(z.x);
                float height = openingHalfExtents.y - Mathf.Abs(x.z) - Mathf.Abs(y.z) - Mathf.Abs(z.z);
                return width > 0 && height > 0 && Mathf.Abs(local.x) <= width && Mathf.Abs(local.z) <= height;
            }
            return FitsOpening(crossingOffset, shot.ScoringRadius, normal);
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
