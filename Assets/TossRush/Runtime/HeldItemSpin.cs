using UnityEngine;

namespace TossRush
{
    public sealed class HeldItemSpin : MonoBehaviour
    {
        public float degreesPerSecond = 150;
        Quaternion restingRotation;
        float angle;
        void Awake() => restingRotation = transform.localRotation;
        void OnEnable() { angle = 0; }
        void Update()
        {
            angle = (angle + degreesPerSecond * Time.deltaTime) % 360;
            transform.localRotation = restingRotation * Quaternion.AngleAxis(angle, Vector3.up);
        }
    }
}
