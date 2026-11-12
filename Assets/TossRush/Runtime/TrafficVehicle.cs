using UnityEngine;

namespace TossRush
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class TrafficVehicle : MonoBehaviour
    {
        public StageView stage;
        [Min(0)] public float speed = 4;
        public float direction = 1;
        [Min(1)] public float halfTravel = 16;
        Rigidbody body;
        Vector3 home;
        Quaternion homeRotation;
        float elapsed;
        bool initialized;

        public void InitializePose()
        {
            if (initialized) return;
            body = GetComponent<Rigidbody>();
            home = transform.localPosition;
            homeRotation = transform.localRotation;
            initialized = true;
        }

        public void ResetPose()
        {
            InitializePose();
            elapsed = 0;
            transform.SetLocalPositionAndRotation(home, homeRotation);
            body.position = transform.position;
            body.rotation = transform.rotation;
        }

        void FixedUpdate()
        {
            if (!stage || !stage.Game || !stage.Game.IsPlaying) return;
            elapsed += Time.fixedDeltaTime;
            float x = Mathf.Repeat(home.x + elapsed * speed * Mathf.Sign(direction) + halfTravel, halfTravel * 2) - halfTravel;
            Vector3 next = stage.transform.TransformPoint(new Vector3(x, home.y, home.z));
            if (Vector3.Distance(body.position, next) > halfTravel) body.position = next;
            else body.MovePosition(next);
        }
    }
}
