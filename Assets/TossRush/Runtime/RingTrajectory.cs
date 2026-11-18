using UnityEngine;

namespace TossRush
{
    // Shares the real ring pose, swept neck test and changing wind with the preview.
    public static class RingTrajectory
    {
        public struct Result
        {
            public RingTarget target;
            public Vector3 end;
            public Quaternion rotation;
            public float seconds;
            public int count;
            public bool blocked;
        }
        const int WorldMask = ~((1 << 8) | (1 << 9));

        public static bool Crossing(StageView stage, RingTarget target, Vector3 origin, Vector3 velocity, out Vector3 point)
        {
            var data = stage.data;
            Quaternion rotation = RingFlight.LaunchRotation(data, velocity);
            Vector3 normal = RingFlight.UpNormal(rotation), position = origin;
            for (int i = 1; i <= 150; i++)
            {
                Vector3 before = position;
                ProjectileFlight.Step(ref position, ref velocity, data.physicsProfile,
                    stage.WindAtFutureTime(i * Time.fixedDeltaTime) * data.windInfluence, data.gravityScale, Time.fixedDeltaTime);
                float from = Vector3.Dot(before - target.CapturePosition, normal), to = Vector3.Dot(position - target.CapturePosition, normal);
                if (from > 0 && to <= 0 && position.y < before.y)
                { point = Vector3.Lerp(before, position, from / (from - to)); return true; }
            }
            point = position; return false;
        }

        public static Result Predict(StageView stage, Vector3 origin, Vector3 velocity, Vector3[] path)
        {
            var data = stage.data;
            var ring = data.projectilePrefab.GetComponent<RingFlight>();
            var result = new Result { end = origin, rotation = RingFlight.LaunchRotation(data, velocity), count = 1 };
            path[0] = origin;
            Vector3 position = origin;
            float outerRadius = ring.innerRadius + ring.tubeRadius * 2;
            for (int i = 1; i < path.Length; i++)
            {
                Vector3 before = position;
                ProjectileFlight.Step(ref position, ref velocity, data.physicsProfile,
                    stage.WindAtFutureTime(i * Time.fixedDeltaTime) * data.windInfluence, data.gravityScale, Time.fixedDeltaTime);
                float collisionFraction = CollisionFraction(before, position, result.rotation, ring, outerRadius);
                RingTarget first = null; float captureFraction = 2;
                var targets = stage.RingTargets;
                for (int j = 0; j < targets.Count; j++)
                {
                    var target = targets[j];
                    if (!target || target.IsClaimed || !target.gameObject.activeInHierarchy) continue;
                    if (!RingTarget.ContainsDownwardCrossing(before, result.rotation, position, result.rotation,
                        target.CapturePosition, target.CapturePosition, ring.WorldInnerRadius, target.WorldCaptureRadius, ring.minimumUpAlignment)) continue;
                    Vector3 normal = RingFlight.UpNormal(result.rotation);
                    float from = Vector3.Dot(before - target.CapturePosition, normal), to = Vector3.Dot(position - target.CapturePosition, normal);
                    float t = from / (from - to);
                    if (t < captureFraction) { first = target; captureFraction = t; }
                }
                float fraction = Mathf.Min(1, Mathf.Min(collisionFraction, captureFraction));
                result.end = Vector3.Lerp(before, position, fraction);
                path[i] = result.end; result.count = i + 1;
                result.seconds = (i - 1 + fraction) * Time.fixedDeltaTime;
                if (first && captureFraction <= collisionFraction)
                { result.target = first; return result; }
                if (collisionFraction <= 1) { result.blocked = true; return result; }
            }
            return result;
        }

        static float CollisionFraction(Vector3 from, Vector3 to, Quaternion rotation, RingFlight ring, float radius)
        {
            Vector3 delta = to - from; float length = delta.magnitude;
            if (length < .000001f) return 2;
            Vector3 direction = delta / length;
            // The enclosing sphere is only a broad-phase. The centre of a ring is empty.
            if (!Physics.SphereCast(from, radius + .01f, direction, out _, length, WorldMask, QueryTriggerInteraction.Ignore)
                && !Physics.CheckSphere(from, radius + .01f, WorldMask, QueryTriggerInteraction.Ignore)) return 2;
            float first = 2;
            const int segments = 32;
            for (int segment = 0; segment < segments; segment++)
            {
                float a = segment * Mathf.PI * 2 / segments;
                Vector3 offset = rotation * new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (ring.innerRadius + ring.tubeRadius);
                Vector3 start = from + offset;
                // Small contact margin avoids a green preview that PhysX stops at a cap.
                float tube = ring.tubeRadius + .004f;
                if (Physics.CheckSphere(start, tube, WorldMask, QueryTriggerInteraction.Ignore)) return 0;
                if (Physics.SphereCast(start, tube, direction, out var hit, length, WorldMask, QueryTriggerInteraction.Ignore))
                    first = Mathf.Min(first, hit.distance / length);
            }
            return first;
        }
    }
}
