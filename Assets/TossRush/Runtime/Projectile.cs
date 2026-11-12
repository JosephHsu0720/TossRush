using UnityEngine;

namespace TossRush
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Projectile : MonoBehaviour
    {
        public Rigidbody Body { get; private set; }
        public bool InFlight { get; private set; }
        public bool Scored { get; private set; }
        public int StageToken { get; private set; }
        public float Radius { get; private set; }
        public Vector3 PreviousPosition { get; private set; }
        public int CollisionCount { get; private set; }
        public int PropCollisionCount { get; private set; }
        ProjectilePhysicsProfile profile;
        Vector3 wind;
        float gravityScale;
        float expiresAt;

        void Awake() => Body = GetComponent<Rigidbody>();

        public void Launch(StageData data, int token, Vector3 position, Vector3 velocity, Vector3 stageWind)
        {
            if (!Body) Body = GetComponent<Rigidbody>();
            transform.SetPositionAndRotation(position, Quaternion.identity);
            PreviousPosition = position;
            StageToken = token;
            Radius = data.projectileRadius;
            Scored = false;
            CollisionCount = PropCollisionCount = 0;
            InFlight = true;
            profile = data.physicsProfile;
            wind = stageWind * data.windInfluence;
            gravityScale = data.gravityScale;
            expiresAt = Time.time + (profile ? profile.flightLifetime : 4f);
            gameObject.SetActive(true);
            Body.position = position;
            Body.mass = profile ? profile.mass : 0.3f;
            Body.useGravity = false;
            Body.linearDamping = 0;
            Body.angularDamping = profile ? profile.angularDamping : 0.1f;
            Body.maxAngularVelocity = Mathf.Max(7, profile ? profile.initialSpin.magnitude : 0);
            GetComponent<Collider>().sharedMaterial = profile ? profile.surfaceMaterial : null;
            Body.linearVelocity = velocity;
            Body.angularVelocity = profile ? profile.initialSpin : Vector3.zero;
            Body.WakeUp();
        }

        // Called after the target has tested the previous physics segment.
        void FixedUpdate()
        {
            if (!InFlight) return;
            PreviousPosition = Body.position;
            // After contact, let friction and gravity roll the bag off the lid, without artificial sideways lift.
            Vector3 spin = CollisionCount == 0 ? Body.angularVelocity : Vector3.zero;
            Body.AddForce(ProjectileFlight.Acceleration(profile, Body.linearVelocity, wind, gravityScale, spin), ForceMode.Acceleration);
            if (Time.time >= expiresAt || Body.position.y < -4f) Retire();
        }

        public void MarkScored() => Scored = true;

        void OnCollisionEnter(Collision collision)
        {
            if (!InFlight || Scored) return;
            CollisionCount++;
            if (!collision.collider.GetComponentInParent<TargetMarker>()) PropCollisionCount++;
        }

        public void Retire()
        {
            InFlight = false;
            if (Body) { Body.linearVelocity = Vector3.zero; Body.angularVelocity = Vector3.zero; }
            gameObject.SetActive(false);
        }
    }
}
