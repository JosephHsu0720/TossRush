using UnityEngine;

namespace TossRush
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Pedestrian : MonoBehaviour
    {
        public StageView stage;
        [Min(0)] public float speed = 1.1f;
        public float direction = 1;
        [Min(1)] public float halfTravel = 12;
        public Transform leftArm, rightArm, leftLeg, rightLeg;
        Rigidbody body;
        Vector3 home;
        Quaternion rotation;
        Quaternion[] limbRest;
        Transform[] limbs;
        float elapsed;

        public void InitializePose()
        {
            if (body) return;
            body = GetComponent<Rigidbody>();
            home = transform.localPosition;
            rotation = transform.localRotation;
            limbs = new[] { leftArm, rightArm, leftLeg, rightLeg };
            limbRest = new Quaternion[limbs.Length];
            for (int i = 0; i < limbs.Length; i++) if (limbs[i]) limbRest[i] = limbs[i].localRotation;
        }

        public void ResetPose()
        {
            InitializePose();
            elapsed = 0;
            transform.SetLocalPositionAndRotation(home, rotation);
            body.position = transform.position;
            body.rotation = transform.rotation;
            for (int i = 0; i < limbs.Length; i++) if (limbs[i]) limbs[i].localRotation = limbRest[i];
        }

        void FixedUpdate()
        {
            if (!stage || !stage.Game || !stage.Game.CanScore) return;
            elapsed += Time.fixedDeltaTime;
            float x = Mathf.Repeat(home.x + elapsed * speed * Mathf.Sign(direction) + halfTravel, halfTravel * 2) - halfTravel;
            Vector3 next = stage.transform.TransformPoint(new Vector3(x, home.y, home.z));
            if (Vector3.Distance(body.position, next) > halfTravel) body.position = next;
            else body.MovePosition(next);
            float swing = Mathf.Sin(elapsed * speed * 7) * 28;
            for (int i = 0; i < limbs.Length; i++) if (limbs[i])
                limbs[i].localRotation = limbRest[i] * Quaternion.Euler(swing * (i == 0 || i == 3 ? 1 : -1), 0, 0);
        }
    }
}
