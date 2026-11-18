using UnityEngine;

namespace TossRush
{
    // The ring lies in local X/Z. Its compound colliders leave the centre open.
    [RequireComponent(typeof(Projectile), typeof(Rigidbody))]
    public sealed class RingFlight : MonoBehaviour
    {
        [Min(.01f)] public float innerRadius = .23f;
        [Min(.001f)] public float tubeRadius = .03f;
        [Range(0, 1)] public float minimumUpAlignment = .7f;

        public float WorldInnerRadius
        {
            get
            {
                Vector3 scale = transform.lossyScale;
                return innerRadius * Mathf.Min(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            }
        }

        void OnEnable()
        {
            // Preserve the slightly raised front edge through each pooled flight.
            GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
        }

        public static Vector3 UpNormal(Quaternion rotation)
        {
            Vector3 normal = rotation * Vector3.up;
            return normal.y < 0 ? -normal : normal;
        }

        public static Quaternion LaunchRotation(StageData data, Vector3 velocity)
        {
            Vector3 heading = Vector3.ProjectOnPlane(velocity, Vector3.up);
            return (heading.sqrMagnitude > .001f ? Quaternion.LookRotation(heading, Vector3.up) : Quaternion.identity)
                * Quaternion.Euler(data.projectileEulerAngles);
        }
    }
}
