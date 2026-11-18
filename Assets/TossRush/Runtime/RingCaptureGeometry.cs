using UnityEngine;

namespace TossRush
{
    // Tests the actual collider section cut by the ring plane, rather than a
    // fixed, oversized neck circle. All supported shapes use world-space sizes.
    internal static class RingCaptureGeometry
    {
        static readonly Vector3[] corners = new Vector3[8];

        public static bool Encloses(Collider[] shapes, Vector3 center, Vector3 normal, float innerRadius)
        {
            bool intersects = false;
            foreach (var shape in shapes)
            {
                if (!shape || !shape.enabled || !shape.gameObject.activeInHierarchy || shape.isTrigger) continue;
                float extent;
                if (shape is SphereCollider sphere)
                {
                    Vector3 scale = Abs(sphere.transform.lossyScale);
                    extent = SphereSection(sphere.transform.TransformPoint(sphere.center),
                        sphere.radius * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z)), center, normal);
                }
                else if (shape is CapsuleCollider capsule) extent = CapsuleSection(capsule, center, normal);
                else
                {
                    if (shape is BoxCollider box)
                    {
                        for (int i = 0; i < 8; i++)
                            corners[i] = box.transform.TransformPoint(box.center + Vector3.Scale(box.size * .5f, Signs(i)));
                    }
                    else
                    {
                        Bounds bounds = shape.bounds;
                        for (int i = 0; i < 8; i++) corners[i] = bounds.center + Vector3.Scale(bounds.extents, Signs(i));
                    }
                    extent = BoxSection(center, normal);
                }
                if (extent < 0) continue;
                intersects = true;
                // Half a millimetre absorbs floating-point noise, not visible rim overlap.
                if (extent > innerRadius + .0005f) return false;
            }
            return intersects;
        }

        static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        static Vector3 Signs(int i) => new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1);

        static float SphereSection(Vector3 sphere, float radius, Vector3 center, Vector3 normal)
        {
            float distance = Vector3.Dot(sphere - center, normal);
            if (Mathf.Abs(distance) > radius) return -1;
            float sectionRadius = Mathf.Sqrt(Mathf.Max(0, radius * radius - distance * distance));
            return Vector3.ProjectOnPlane(sphere - center, normal).magnitude + sectionRadius;
        }

        static float CapsuleSection(CapsuleCollider shape, Vector3 center, Vector3 normal)
        {
            Vector3 scale = Abs(shape.transform.lossyScale);
            int direction = shape.direction;
            float along = direction == 0 ? scale.x : direction == 1 ? scale.y : scale.z;
            float across = direction == 0 ? Mathf.Max(scale.y, scale.z) : direction == 1 ? Mathf.Max(scale.x, scale.z) : Mathf.Max(scale.x, scale.y);
            float radius = shape.radius * across;
            float half = Mathf.Max(0, shape.height * along * .5f - radius);
            Vector3 axis = shape.transform.rotation * (direction == 0 ? Vector3.right : direction == 1 ? Vector3.up : Vector3.forward);
            Vector3 middle = shape.transform.TransformPoint(shape.center);
            if (half < .00001f) return SphereSection(middle, radius, center, normal);
            Vector3 start = middle - axis * half, delta = axis * (2 * half);
            float from = Vector3.Dot(start - center, normal), change = Vector3.Dot(delta, normal);
            float low = 0, high = 1;
            if (Mathf.Abs(change) > .000001f)
            {
                float a = (-radius - from) / change, b = (radius - from) / change;
                low = Mathf.Max(0, Mathf.Min(a, b)); high = Mathf.Min(1, Mathf.Max(a, b));
                if (low > high) return -1;
            }
            else if (Mathf.Abs(from) > radius) return -1;

            float Section(float t) => SphereSection(start + delta * t, radius, center, normal);
            const int samples = 24;
            float step = (high - low) / samples;
            float previous = Section(low), current = Section(low + step);
            float maximum = Mathf.Max(previous, current);
            for (int i = 1; i < samples; i++)
            {
                float next = Section(low + (i + 1) * step);
                maximum = Mathf.Max(maximum, next);
                if (current >= previous && current >= next)
                {
                    float left = low + (i - 1) * step, right = low + (i + 1) * step;
                    for (int refine = 0; refine < 14; refine++)
                    {
                        float a = Mathf.Lerp(left, right, 1f / 3), b = Mathf.Lerp(left, right, 2f / 3);
                        if (Section(a) < Section(b)) left = a; else right = b;
                    }
                    maximum = Mathf.Max(maximum, Section((left + right) * .5f));
                }
                previous = current; current = next;
            }
            return maximum;
        }

        static float BoxSection(Vector3 center, Vector3 normal)
        {
            float maximumSquared = -1;
            for (int i = 0; i < 8; i++)
            {
                float from = Vector3.Dot(corners[i] - center, normal);
                if (Mathf.Abs(from) < .000001f) maximumSquared = Mathf.Max(maximumSquared, (corners[i] - center).sqrMagnitude);
                for (int bit = 1; bit <= 4; bit *= 2)
                {
                    int j = i ^ bit;
                    if (j <= i) continue;
                    float to = Vector3.Dot(corners[j] - center, normal);
                    if (from * to >= 0 || Mathf.Abs(from - to) < .000001f) continue;
                    Vector3 point = Vector3.Lerp(corners[i], corners[j], from / (from - to));
                    maximumSquared = Mathf.Max(maximumSquared, (point - center).sqrMagnitude);
                }
            }
            return maximumSquared < 0 ? -1 : Mathf.Sqrt(maximumSquared);
        }
    }
}
