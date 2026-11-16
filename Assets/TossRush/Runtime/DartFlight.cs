using UnityEngine;

namespace TossRush
{
    // A tip collider follows the flight direction; missed darts stay embedded until pooled.
    [RequireComponent(typeof(Projectile))]
    [DefaultExecutionOrder(-75)]
    public sealed class DartFlight : MonoBehaviour
    {
        Rigidbody body;
        bool stuck;

        void Awake() => body = GetComponent<Rigidbody>();

        void OnEnable()
        {
            if (!body) body = GetComponent<Rigidbody>();
            stuck = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
        }

        void FixedUpdate()
        {
            if (!stuck && body.linearVelocity.sqrMagnitude > .01f)
                body.rotation = Quaternion.LookRotation(body.linearVelocity, Vector3.up);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (stuck) return;
            stuck = true;
            body.linearVelocity = body.angularVelocity = Vector3.zero;
            body.constraints = RigidbodyConstraints.FreezeAll;
        }
    }
}
