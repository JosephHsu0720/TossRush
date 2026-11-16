using UnityEngine;

namespace TossRush
{
    [RequireComponent(typeof(Rigidbody))]
    [DefaultExecutionOrder(-50)]
    public sealed class Projectile : MonoBehaviour
    {
        public Rigidbody Body { get; private set; }
        public bool InFlight { get; private set; }
        public bool Scored { get; private set; }
        public int StageToken { get; private set; }
        public float Radius { get; private set; }
        public Vector3 PreviousPosition { get; private set; }
        public Vector3 PreviousScoringPosition { get; private set; }
        public Quaternion PreviousScoringRotation { get; private set; }
        internal int LaunchId { get; private set; }
        public int CollisionCount { get; private set; }
        public int PropCollisionCount { get; private set; }
        public int PendingCollisionPoints { get; private set; }
        public int ReboundCount { get; private set; }
        public float LastReboundSpeed { get; private set; }
        internal GameDirector Owner { get; set; }
        ProjectilePhysicsProfile profile;
        Vector3 wind;
        float gravityScale;
        float expiresAt;
        SoftBagDeformation softBag;
        Collider shape;
        SphereCollider sphere;

        void Awake()
        {
            Body = GetComponent<Rigidbody>(); softBag = GetComponent<SoftBagDeformation>();
            shape = GetComponent<Collider>(); sphere = shape as SphereCollider;
        }

        public Vector3 ScoringPosition
        {
            get
            {
                Vector3 center = sphere ? sphere.center : shape is BoxCollider box ? box.center : shape is CapsuleCollider capsule ? capsule.center : Vector3.zero;
                return Body.position + Body.rotation * Vector3.Scale(center, transform.lossyScale);
            }
        }

        public float ScoringRadius
        {
            get
            {
                if (!sphere) return Radius;
                Vector3 scale = transform.lossyScale;
                return sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            }
        }

        public void Launch(StageData data, int token, Vector3 position, Vector3 velocity, Vector3 stageWind)
        {
            if (!Body) Body = GetComponent<Rigidbody>();
            if (softBag) softBag.ResetDeformation();
            Quaternion rotation = GetComponent<DartFlight>() && velocity.sqrMagnitude > .01f
                ? Quaternion.LookRotation(velocity, Vector3.up) : Quaternion.Euler(data.projectileEulerAngles);
            transform.SetPositionAndRotation(position, rotation);
            PreviousPosition = position;
            StageToken = token;
            LaunchId++;
            Radius = data.projectileRadius;
            Scored = false;
            CollisionCount = PropCollisionCount = 0;
            PendingCollisionPoints = 0;
            ReboundCount = 0;
            LastReboundSpeed = 0;
            InFlight = true;
            profile = data.physicsProfile;
            wind = stageWind * data.windInfluence;
            gravityScale = data.gravityScale;
            expiresAt = Time.time + (profile ? profile.flightLifetime : 4f);
            gameObject.SetActive(true);
            Body.position = position;
            Body.rotation = rotation;
            PreviousScoringPosition = ScoringPosition;
            PreviousScoringRotation = Body.rotation;
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
            // Snapshot before soft-body collider changes and the next physics step.
            PreviousScoringPosition = ScoringPosition;
            PreviousScoringRotation = Body.rotation;
            // After contact, let friction and gravity roll the bag off the lid, without artificial sideways lift.
            Vector3 spin = CollisionCount == 0 ? Body.angularVelocity : Vector3.zero;
            Body.AddForce(ProjectileFlight.Acceleration(profile, Body.linearVelocity, wind, gravityScale, spin), ForceMode.Acceleration);
            if (Time.time >= expiresAt || Body.position.y < -4f) Retire();
        }

        public void MarkScored() => Scored = true;

        internal void AccumulateCollisionPoints(int points)
        {
            PendingCollisionPoints += points;
            ReboundCount++;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!InFlight || Scored) return;
            if (Owner && Owner.ActiveStage.TryScoreBeforeInteriorContact(this, collision)) return;
            CollisionCount++;
            if (!collision.collider.GetComponentInParent<TargetMarker>()) PropCollisionCount++;
            if (softBag && collision.contactCount > 0) softBag.AbsorbCollision(collision);
            if (!Owner || collision.contactCount == 0) return;
            var contact = collision.GetContact(0);
            if (Owner.CanScore && Owner.gameAudio) Owner.gameAudio.Impact(collision.relativeVelocity.magnitude);
            var pedestrian = collision.collider.GetComponentInParent<Pedestrian>();
            if (pedestrian)
            {
                Owner.RegisterPedestrianHit(this, pedestrian, contact.point);
                return;
            }
            var prop = collision.collider.GetComponentInParent<PhysicsProp>();
            if (prop && prop.ExcludesReboundBonus) return;
            Vector3 surfaceVelocity = collision.rigidbody ? collision.rigidbody.GetPointVelocity(contact.point) : Vector3.zero;
            // Use the solved outgoing velocity, not impact impulse: a hard stop or a graze is not a rebound.
            LastReboundSpeed = Mathf.Max(0, Vector3.Dot(Body.linearVelocity - surfaceVelocity, contact.normal));
            Owner.RegisterCollision(this, contact.point, LastReboundSpeed);
        }

        public void Retire()
        {
            InFlight = false;
            PendingCollisionPoints = 0;
            if (softBag) softBag.ResetDeformation();
            if (Body) { Body.linearVelocity = Vector3.zero; Body.angularVelocity = Vector3.zero; }
            gameObject.SetActive(false);
        }
    }
}
