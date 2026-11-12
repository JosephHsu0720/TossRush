using UnityEngine;

namespace TossRush
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PhysicsProp : MonoBehaviour
    {
        Rigidbody body;
        Vector3 homePosition;
        Quaternion homeRotation;

        public void InitializePose()
        {
            body = GetComponent<Rigidbody>();
            homePosition = transform.localPosition;
            homeRotation = transform.localRotation;
        }

        public void ResetPose()
        {
            transform.SetLocalPositionAndRotation(homePosition, homeRotation);
            body.position = transform.position;
            body.rotation = transform.rotation;
            body.linearVelocity = body.angularVelocity = Vector3.zero;
            body.Sleep();
        }
    }
}
