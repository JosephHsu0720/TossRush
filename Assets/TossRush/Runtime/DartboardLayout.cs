using System;
using UnityEngine;

namespace TossRush
{
    [CreateAssetMenu(menuName = "TossRush/Dartboard Layout")]
    public sealed class DartboardLayout : ScriptableObject
    {
        [Serializable]
        public sealed class Region
        {
            public string label;
            public float innerRadius, outerRadius, startAngle, endAngle;
            public Mesh mask, outline;

            public Vector2 AimPoint
            {
                get
                {
                    if (innerRadius == 0) return Vector2.zero;
                    float angle = (startAngle + endAngle) * .5f * Mathf.Deg2Rad;
                    return new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * ((innerRadius + outerRadius) * .5f);
                }
            }

            public bool Contains(Vector2 point, float tipRadius)
            {
                float distance = point.magnitude;
                if (distance + tipRadius > outerRadius + .0001f) return false;
                if (innerRadius > 0 && distance - tipRadius < innerRadius - .0001f) return false;
                float span = endAngle - startAngle;
                if (span >= 359.99f) return true;
                float angle = Mathf.Atan2(point.x, point.y) * Mathf.Rad2Deg;
                float delta = Mathf.Abs(Mathf.DeltaAngle((startAngle + endAngle) * .5f, angle));
                float remaining = span * .5f - delta;
                return remaining >= 0 && distance * Mathf.Sin(remaining * Mathf.Deg2Rad) >= tipRadius - .0001f;
            }
        }

        public Region[] regions;
    }
}
