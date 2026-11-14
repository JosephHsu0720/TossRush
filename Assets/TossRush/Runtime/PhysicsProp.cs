using UnityEngine;

namespace TossRush
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PhysicsProp : MonoBehaviour
    {
        [Min(0)] public int knockdownPoints;
        public bool releaseOnImpact;
        public Vector3 releaseVelocity;
        [HideInInspector] public StageView stage;
        public bool KnockedDown { get; private set; }
        public bool ExcludesReboundBonus => knockdownPoints > 0;
        Rigidbody body;
        Vector3 homePosition;
        Quaternion homeRotation;
        int activatedToken = -1;
        bool homeKinematic;

        public void InitializePose()
        {
            body = GetComponent<Rigidbody>();
            homePosition = transform.localPosition;
            homeRotation = transform.localRotation;
            homeKinematic = body.isKinematic;
        }

        public void ResetPose()
        {
            body.isKinematic = false;
            body.linearVelocity = body.angularVelocity = Vector3.zero;
            transform.SetLocalPositionAndRotation(homePosition, homeRotation);
            body.position = transform.position;
            body.rotation = transform.rotation;
            body.isKinematic = homeKinematic;
            KnockedDown = false;
            activatedToken = -1;
            body.Sleep();
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!stage || !stage.Game || !stage.Game.CanScore || stage.Game.ActiveStage != stage) return;
            var shot = collision.collider.GetComponentInParent<Projectile>();
            var other = collision.collider.GetComponentInParent<PhysicsProp>();
            bool playerHit = shot && shot.Owner == stage.Game && shot.InFlight && !shot.Scored && shot.StageToken == stage.Game.StageToken;
            bool chainHit = other && other.stage == stage && other.activatedToken == stage.Game.StageToken;
            if (!playerHit && !chainHit) return;
            activatedToken = stage.Game.StageToken;
            if (releaseOnImpact && body.isKinematic)
            {
                body.isKinematic = false;
                body.useGravity = true;
                body.linearVelocity = transform.TransformDirection(releaseVelocity);
                body.angularVelocity = transform.right * 1.5f;
                body.WakeUp();
            }
        }

        void FixedUpdate()
        {
            if (knockdownPoints <= 0 || KnockedDown || !stage || !stage.Game || !stage.Game.CanScore
                || stage.Game.ActiveStage != stage || activatedToken != stage.Game.StageToken) return;
            Quaternion originalRotation = transform.parent.rotation * homeRotation;
            bool tipped = Vector3.Dot(transform.up, originalRotation * Vector3.up) < .65f;
            bool fallen = transform.position.y < transform.parent.TransformPoint(homePosition).y - .2f;
            if (!tipped && !fallen) return;
            KnockedDown = true;
            stage.Game.RegisterKnockdown(this);
        }
    }
}
